using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class MosfetElm : CircuitElm {
        const int FLAG_PNP = 1;
        const int FLAG_SHOWVT = 2;
        const int FLAG_DIGITAL = 4;
        const int FLAG_FLIP = 8;
        const int FLAG_HIDE_BULK = 16;
        const int FLAG_BODY_DIODE = 32;
        const int FLAGS_GLOBAL = (FLAG_HIDE_BULK | FLAG_DIGITAL);

        const int V_G = 0;
        const int V_S = 1;
        const int V_D = 2;

        const int HS = 16;

        const int SEGMENTS = 6;
        const double SEG_F = 1.0 / SEGMENTS;

        static int mGlobalFlags;
        static double mLastHfe;

        int mPnp;
        int mBodyTerminal;
        double mVt;
        double mHfe; /* hfe = 1/(RdsON*(Vgs-Vt)) */
        Diode mDiodeB1;
        Diode mDiodeB2;
        double mDiodeCurrent1;
        double mDiodeCurrent2;
        double mCurcountBody1;
        double mCurcountBody2;

        int mPcircler;

        Point[] mSrc;
        Point[] mDrn;
        Point[] mGate;
        Point[] mBody;
        Point[] mArrowPoly;
        Point mPcircle;

        Point[] mPs1;
        Point[] mPs2;

        double mLastV0;
        double mLastV1;
        double mLastV2;
        double mIds;
        int mMode = 0;
        double mGm = 0;

        public MosfetElm(Point pos, bool pnpflag) : base(pos) {
            mPnp = pnpflag ? -1 : 1;
            mFlags = pnpflag ? FLAG_PNP : 0;
            mFlags |= FLAG_BODY_DIODE;
            mNoDiagonal = true;
            setupDiodes();
            mHfe = DefaultHfe;
            mVt = DefaultThreshold;
        }

        public MosfetElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mPnp = ((f & FLAG_PNP) != 0) ? -1 : 1;
            mNoDiagonal = true;
            setupDiodes();
            mVt = DefaultThreshold;
            mHfe = BackwardCompatibilityHfe;
            try {
                mVt = st.nextTokenDouble();
                mHfe = st.nextTokenDouble();
            } catch { }
            mGlobalFlags = mFlags & (FLAGS_GLOBAL);
            allocNodes(); /* make sure volts[] has the right number of elements when hasBodyTerminal() is true */
        }

        public override double Current { get { return mIds; } }

        public override double VoltageDiff { get { return Volts[V_D] - Volts[V_S]; } }

        public override double Power {
            get {
                return mIds * (Volts[V_D] - Volts[V_S])
                    - mDiodeCurrent1 * (Volts[V_S] - Volts[mBodyTerminal])
                    - mDiodeCurrent2 * (Volts[V_D] - Volts[mBodyTerminal]);
            }
        }

        public override bool CanViewInScope { get { return true; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.MOSFET; } }

        protected override string dump() {
            return mVt + " " + mHfe;
        }

        double DefaultThreshold { get { return 1.5; } }

        double DefaultHfe {
            get { return mLastHfe == 0 ? BackwardCompatibilityHfe : mLastHfe; }
        }

        double BackwardCompatibilityHfe { get { return .02; } }

        bool DrawDigital { get { return (mFlags & FLAG_DIGITAL) != 0; } }

        bool ShowBulk { get { return (mFlags & (FLAG_DIGITAL | FLAG_HIDE_BULK)) == 0; } }

        bool DoBodyDiode { get { return (mFlags & FLAG_BODY_DIODE) != 0 && ShowBulk; } }

        /* post 0 = gate,
         * 1 = source for NPN,
         * 2 = drain for NPN,
         * 3 = body (if present)
         * for PNP, 1 is drain, 2 is source */
        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mSrc[0] : (n == 2) ? mDrn[0] : mBody[0];
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return 0;
            }
            if (n == 3) {
                return -mDiodeCurrent1 - mDiodeCurrent2;
            }
            if (n == 1) {
                return mIds + mDiodeCurrent1;
            }
            return -mIds + mDiodeCurrent2;
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[1]);
            mCir.StampNonLinear(Nodes[2]);

            mBodyTerminal = (mPnp == -1) ? 2 : 1;

            if (DoBodyDiode) {
                if (mPnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    mDiodeB1.stamp(Nodes[1], Nodes[mBodyTerminal]);
                    mDiodeB2.stamp(Nodes[2], Nodes[mBodyTerminal]);
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    mDiodeB1.stamp(Nodes[mBodyTerminal], Nodes[1]);
                    mDiodeB2.stamp(Nodes[mBodyTerminal], Nodes[2]);
                }
            }
        }

        public override void Reset() {
            mLastV1 = mLastV2 = 0;
            Volts[V_G] = Volts[V_S] = Volts[V_D] = 0;
            mCurCount = 0;
            mDiodeB1.reset();
            mDiodeB2.reset();
        }

        public override void SetPoints() {
            base.SetPoints();

            /* these two flags apply to all mosfets */
            mFlags &= ~FLAGS_GLOBAL;
            mFlags |= mGlobalFlags;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            int hs2 = HS * mDsign;
            if ((mFlags & FLAG_FLIP) != 0) {
                hs2 = -hs2;
            }
            mSrc = new Point[3];
            mDrn = new Point[3];
            interpPointAB(ref mSrc[0], ref mDrn[0], 1, -hs2);
            interpPointAB(ref mSrc[1], ref mDrn[1], 1 - 18 / mLen, -hs2);
            interpPointAB(ref mSrc[2], ref mDrn[2], 1 - 18 / mLen, -hs2 * 4 / 3);

            mGate = new Point[3];
            interpPointAB(ref mGate[0], ref mGate[2], 1 - 24 / mLen, hs2 / 2);
            Utils.InterpPoint(mGate[0], mGate[2], ref mGate[1], .5);

            if (ShowBulk) {
                mBody = new Point[2];
                Utils.InterpPoint(mSrc[0], mDrn[0], ref mBody[0], .5);
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mBody[1], .5);
            }

            if (!DrawDigital) {
                if (mPnp == 1) {
                    if (ShowBulk) {
                        Utils.CreateArrow(mBody[0], mBody[1], out mArrowPoly, 10, 4);
                    } else {
                        Utils.CreateArrow(mSrc[1], mSrc[0], out mArrowPoly, 10, 4);
                    }
                } else {
                    if (ShowBulk) {
                        Utils.CreateArrow(mBody[1], mBody[0], out mArrowPoly, 10, 4);
                    } else {
                        Utils.CreateArrow(mDrn[0], mDrn[1], out mArrowPoly, 10, 4);
                    }
                }
            } else if (mPnp == -1) {
                interpPoint(ref mGate[1], 1 - 36 / mLen);
                int dist = (mDsign < 0) ? 32 : 31;
                interpPoint(ref mPcircle, 1 - dist / mLen);
                mPcircler = 3;
            }

            mPs1 = new Point[SEGMENTS];
            mPs2 = new Point[SEGMENTS];
            for (int i = 0; i != SEGMENTS; i++) {
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mPs1[i], i * SEG_F);
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mPs2[i], (i + 1) * SEG_F);
            }
        }

        public override void DoStep() {
            calculate(false);
        }

        public override void StepFinished() {
            calculate(true);

            /* fix current if body is connected to source or drain */
            if (mBodyTerminal == 1) {
                mDiodeCurrent1 = -mDiodeCurrent2;
            }
            if (mBodyTerminal == 2) {
                mDiodeCurrent2 = -mDiodeCurrent1;
            }
        }

        public override void Draw(CustomGraphics g) {
            /* pick up global flags changes */
            if ((mFlags & FLAGS_GLOBAL) != mGlobalFlags) {
                SetPoints();
            }

            setBbox(mPoint1, mPoint2, HS);

            /* draw source/drain terminals */
            drawVoltage(g, V_S, mSrc[0], mSrc[1]);
            drawVoltage(g, V_D, mDrn[0], mDrn[1]);

            /* draw line connecting source and drain */
            bool enhancement = mVt > 0 && ShowBulk;
            for (int i = 0; i != SEGMENTS; i++) {
                if ((i == 1 || i == 4) && enhancement) {
                    continue;
                }
                double v = Volts[V_S] + (Volts[V_D] - Volts[V_S]) * i / SEGMENTS;
                g.DrawThickLine(getVoltageColor(v), mPs1[i], mPs2[i]);
            }

            /* draw little extensions of that line */
            drawVoltage(g, V_S, mSrc[1], mSrc[2]);
            drawVoltage(g, V_D, mDrn[1], mDrn[2]);

            /* draw bulk connection */
            if (ShowBulk) {
                g.ThickLineColor = getVoltageColor(Volts[mBodyTerminal]);
                g.DrawThickLine(mPnp == -1 ? mDrn[0] : mSrc[0], mBody[0]);
                g.DrawThickLine(mBody[0], mBody[1]);
            }

            /* draw arrow */
            if (!DrawDigital) {
                drawVoltage(g, mBodyTerminal, mArrowPoly);
            }

            /* draw gate */
            drawVoltage(g, V_G, mPoint1, mGate[1]);
            g.DrawThickLine(mGate[0], mGate[2]);
            if (DrawDigital && mPnp == -1) {
                g.DrawThickCircle(mPcircle, mPcircler);
            }

            if ((mFlags & FLAG_SHOWVT) != 0) {
                string s = "" + (mVt * mPnp);
                drawCenteredLText(g, s, P2.X + 2, P2.Y, false);
            }
            mCurCount = updateDotCount(-mIds, mCurCount);
            drawDots(g, mSrc[0], mSrc[1], mCurCount);
            drawDots(g, mDrn[1], mDrn[0], mCurCount);
            drawDots(g, mSrc[1], mDrn[1], mCurCount);

            if (ShowBulk) {
                mCurcountBody1 = updateDotCount(mDiodeCurrent1, mCurcountBody1);
                mCurcountBody2 = updateDotCount(mDiodeCurrent2, mCurcountBody2);
                drawDots(g, mSrc[0], mBody[0], -mCurcountBody1);
                drawDots(g, mBody[0], mDrn[0], mCurcountBody2);
            }

            drawPosts(g);
        }

        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            mDiodeB1 = new Diode(mCir);
            mDiodeB1.setupForDefaultModel();
            /* diode from node 2 to body terminal */
            mDiodeB2 = new Diode(mCir);
            mDiodeB2.setupForDefaultModel();
        }

        /* this is called in doStep to stamp the matrix,
         * and also called in stepFinished() to calculate the current */
        void calculate(bool finished) {
            double[] vs;
            if (finished) {
                vs = Volts;
            } else {
                /* limit voltage changes to .5V */
                vs = new double[3];
                vs[0] = Volts[V_G];
                vs[1] = Volts[V_S];
                vs[2] = Volts[V_D];
                if (vs[1] > mLastV1 + .5) {
                    vs[1] = mLastV1 + .5;
                }
                if (vs[1] < mLastV1 - .5) {
                    vs[1] = mLastV1 - .5;
                }
                if (vs[2] > mLastV2 + .5) {
                    vs[2] = mLastV2 + .5;
                }
                if (vs[2] < mLastV2 - .5) {
                    vs[2] = mLastV2 - .5;
                }
            }

            int source = 1;
            int drain = 2;

            /* if source voltage > drain (for NPN), swap source and drain
             * (opposite for PNP) */
            if (mPnp * vs[1] > mPnp * vs[2]) {
                source = 2;
                drain = 1;
            }
            int gate = 0;
            double vgs = vs[gate] - vs[source];
            double vds = vs[drain] - vs[source];
            if (!finished && (nonConvergence(mLastV1, vs[1]) || nonConvergence(mLastV2, vs[2]) || nonConvergence(mLastV0, vs[0]))) {
                mCir.Converged = false;
            }
            mLastV0 = vs[0];
            mLastV1 = vs[1];
            mLastV2 = vs[2];
            double realvgs = vgs;
            double realvds = vds;
            vgs *= mPnp;
            vds *= mPnp;
            mIds = 0;
            mGm = 0;
            double Gds = 0;
            if (vgs < mVt) {
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                mIds = vds * Gds;
                mMode = 0;
            } else if (vds < vgs - mVt) {
                /* linear */
                mIds = mHfe * ((vgs - mVt) * vds - vds * vds * .5);
                mGm = mHfe * vds;
                Gds = mHfe * (vgs - vds - mVt);
                mMode = 1;
            } else {
                /* saturation; Gds = 0 */
                mGm = mHfe * (vgs - mVt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                mIds = .5 * mHfe * (vgs - mVt) * (vgs - mVt) + (vds - (vgs - mVt)) * Gds;
                mMode = 2;
            }

            if (DoBodyDiode) {
                mDiodeB1.doStep(mPnp * (Volts[mBodyTerminal] - Volts[V_S]));
                mDiodeCurrent1 = mDiodeB1.calculateCurrent(mPnp * (Volts[mBodyTerminal] - Volts[V_S])) * mPnp;
                mDiodeB2.doStep(mPnp * (Volts[mBodyTerminal] - Volts[V_D]));
                mDiodeCurrent2 = mDiodeB2.calculateCurrent(mPnp * (Volts[mBodyTerminal] - Volts[V_D])) * mPnp;
            } else {
                mDiodeCurrent1 = mDiodeCurrent2 = 0;
            }

            double ids0 = mIds;

            /* flip ids if we swapped source and drain above */
            if (source == 2 && mPnp == 1 || source == 1 && mPnp == -1) {
                mIds = -mIds;
            }

            if (finished) {
                return;
            }

            double rs = -mPnp * ids0 + Gds * realvds + mGm * realvgs;
            mCir.StampMatrix(Nodes[drain], Nodes[drain], Gds);
            mCir.StampMatrix(Nodes[drain], Nodes[source], -Gds - mGm);
            mCir.StampMatrix(Nodes[drain], Nodes[gate], mGm);

            mCir.StampMatrix(Nodes[source], Nodes[drain], -Gds);
            mCir.StampMatrix(Nodes[source], Nodes[source], Gds + mGm);
            mCir.StampMatrix(Nodes[source], Nodes[gate], -mGm);

            mCir.StampRightSide(Nodes[drain], rs);
            mCir.StampRightSide(Nodes[source], -rs);
        }

        bool nonConvergence(double last, double now) {
            double diff = Math.Abs(last - now);

            /* high beta MOSFETs are more sensitive to small differences,
             * so we are more strict about convergence testing */
            if (mHfe > 1) {
                diff *= 100;
            }

            /* difference of less than 10mV is fine */
            if (diff < .01) {
                return false;
            }
            /* larger differences are fine if value is large */
            if (mCir.SubIterations > 10 && diff < Math.Abs(now) * .001) {
                return false;
            }
            /* if we're having trouble converging, get more lenient */
            if (mCir.SubIterations > 100 && diff < .01 + (mCir.SubIterations - 100) * .0001) {
                return false;
            }
            return true;
        }

        void getFetInfo(string[] arr, string n) {
            arr[0] = ((mPnp == -1) ? "p-" : "n-") + n;
            arr[0] += " (Vt=" + Utils.VoltageText(mPnp * mVt);
            arr[0] += ", \u03b2=" + mHfe + ")";
            arr[1] = ((mPnp == 1) ? "Ids = " : "Isd = ") + Utils.CurrentText(mIds);
            arr[2] = "Vgs = " + Utils.VoltageText(Volts[V_G] - Volts[mPnp == -1 ? V_D : V_S]);
            arr[3] = ((mPnp == 1) ? "Vds = " : "Vsd = ") + Utils.VoltageText(Volts[V_D] - Volts[V_S]);
            arr[4] = (mMode == 0) ? "off" : (mMode == 1) ? "linear" : "saturation";
            arr[5] = "gm = " + Utils.UnitText(mGm, "A/V");
            arr[6] = "P = " + Utils.UnitText(Power, "W");
            if (ShowBulk) {
                arr[7] = "Ib = " + Utils.UnitText(mBodyTerminal == 1 ? -mDiodeCurrent1 : mBodyTerminal == 2 ? mDiodeCurrent2 : -mPnp * (mDiodeCurrent1 + mDiodeCurrent2), "A");
            }
        }

        public override void GetInfo(string[] arr) {
            getFetInfo(arr, "MOSFET");
        }

        public override string GetScopeText(Scope.VAL v) {
            return ((mPnp == -1) ? "p-" : "n-") + "MOSFET";
        }

        public override bool GetConnection(int n1, int n2) {
            return !(n1 == 0 || n2 == 0);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("閾値電圧", mPnp * mVt, .01, 5);
            }
            if (n == 1) {
                return new ElementInfo("hfe", mHfe, .01, 5);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "バルク表示",
                    Checked = ShowBulk
                };
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "ドレイン/ソース 入れ替え",
                    Checked = (mFlags & FLAG_FLIP) != 0
                };
                return ei;
            }
            if (n == 4 && !ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "デジタル",
                    Checked = DrawDigital
                };
                return ei;
            }
            if (n == 4 && ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "還流ダイオード",
                    Checked = (mFlags & FLAG_BODY_DIODE) != 0
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mVt = mPnp * ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                mHfe = mLastHfe = ei.Value;
            }
            if (n == 2) {
                mGlobalFlags = (!ei.CheckBox.Checked)
                    ? (mGlobalFlags | FLAG_HIDE_BULK) : (mGlobalFlags & ~(FLAG_HIDE_BULK | FLAG_DIGITAL));
                SetPoints();
                ei.NewDialog = true;
            }
            if (n == 3) {
                mFlags = ei.CheckBox.Checked
                    ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
                SetPoints();
            }
            if (n == 4 && !ShowBulk) {
                mGlobalFlags = ei.CheckBox.Checked
                    ? (mGlobalFlags | FLAG_DIGITAL) : (mGlobalFlags & ~FLAG_DIGITAL);
                SetPoints();
            }
            if (n == 4 && ShowBulk) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_DIODE);
            }
        }
    }
}
