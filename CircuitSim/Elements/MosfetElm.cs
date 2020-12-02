using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class MosfetElm : CircuitElm {
        const int FLAG_PNP = 1;
        const int FLAG_SHOWVT = 2;
        const int FLAG_DIGITAL = 4;
        const int FLAG_FLIP = 8;
        const int FLAG_HIDE_BULK = 16;
        const int FLAG_BODY_DIODE = 32;
        const int FLAG_BODY_TERMINAL = 64;
        const int FLAGS_GLOBAL = (FLAG_HIDE_BULK | FLAG_DIGITAL);

        const int hs = 16;

        int pnp;
        int bodyTerminal;

        double vt;
        /* beta = 1/(RdsON*(Vgs-Vt)) */
        double beta;
        static int globalFlags;
        Diode diodeB1;
        Diode diodeB2;
        double diodeCurrent1;
        double diodeCurrent2;
        double bodyCurrent;
        double curcount_body1;
        double curcount_body2;
        static double lastBeta;

        int pcircler;

        /* points for source and drain (these are swapped on PNP mosfets) */
        Point[] src;
        Point[] drn;

        /* points for gate, body, and the little circle on PNP mosfets */
        Point[] gate;
        Point[] body;
        Point pcircle;
        Point[] arrowPoly;

        Point ps1;
        Point ps2;

        double lastv0;
        double lastv1;
        double lastv2;
        double ids;
        int mode = 0;
        double gm = 0;

        public MosfetElm(int xx, int yy, bool pnpflag) : base(xx, yy) {
            pnp = pnpflag ? -1 : 1;
            mFlags = pnpflag ? FLAG_PNP : 0;
            mFlags |= FLAG_BODY_DIODE;
            mNoDiagonal = true;
            setupDiodes();
            beta = getDefaultBeta();
            vt = getDefaultThreshold();
        }

        public MosfetElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            pnp = ((f & FLAG_PNP) != 0) ? -1 : 1;
            mNoDiagonal = true;
            setupDiodes();
            vt = getDefaultThreshold();
            beta = getBackwardCompatibilityBeta();
            try {
                vt = st.nextTokenDouble();
                beta = st.nextTokenDouble();
            } catch { }
            globalFlags = mFlags & (FLAGS_GLOBAL);
            allocNodes(); /* make sure volts[] has the right number of elements when hasBodyTerminal() is true */
        }

        protected override string dump() {
            return vt + " " + beta;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.MOSFET; }

        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            diodeB1 = new Diode(Sim, Cir);
            diodeB1.setupForDefaultModel();
            /* diode from node 2 to body terminal */
            diodeB2 = new Diode(Sim, Cir);
            diodeB2.setupForDefaultModel();
        }

        double getDefaultThreshold() { return 1.5; }

        /* default beta for new elements */
        double getDefaultBeta() {
            return lastBeta == 0 ? getBackwardCompatibilityBeta() : lastBeta;
        }

        /* default for elements in old files with no configurable beta.
         * JfetElm overrides this.
         * Not sure where this value came from, but the ZVP3306A has a beta of about .027.
         * Power MOSFETs have much higher betas (like 80 or more) */
        double getBackwardCompatibilityBeta() { return .02; }

        public override bool nonLinear() { return true; }

        bool drawDigital() { return (mFlags & FLAG_DIGITAL) != 0; }

        bool showBulk() { return (mFlags & (FLAG_DIGITAL | FLAG_HIDE_BULK)) == 0; }

        bool hasBodyTerminal() { return (mFlags & FLAG_BODY_TERMINAL) != 0; }

        bool doBodyDiode() { return (mFlags & FLAG_BODY_DIODE) != 0 && showBulk(); }

        public override void reset() {
            lastv1 = lastv2 = Volts[0] = Volts[1] = Volts[2] = mCurCount = 0;
            diodeB1.reset();
            diodeB2.reset();
        }

        public override void draw(Graphics g) {
            /* pick up global flags changes */
            if ((mFlags & FLAGS_GLOBAL) != globalFlags) {
                setPoints();
            }

            setBbox(mPoint1, mPoint2, hs);

            /* draw source/drain terminals */
            drawThickLine(g, getVoltageColor(Volts[1]), src[0], src[1]);
            drawThickLine(g, getVoltageColor(Volts[2]), drn[0], drn[1]);

            /* draw line connecting source and drain */
            int segments = 6;
            int i;
            double segf = 1.0/ segments;
            bool enhancement = vt > 0 && showBulk();
            for (i = 0; i != segments; i++) {
                if ((i == 1 || i == 4) && enhancement) {
                    continue;
                }
                double v = Volts[1] + (Volts[2] - Volts[1]) * i / segments;
                PenThickLine.Color = getVoltageColor(v);
                interpPoint(src[1], drn[1], ref ps1, i * segf);
                interpPoint(src[1], drn[1], ref ps2, (i + 1) * segf);
                drawThickLine(g, ps1, ps2);
            }

            /* draw little extensions of that line */
            drawThickLine(g, getVoltageColor(Volts[1]), src[1], src[2]);
            drawThickLine(g, getVoltageColor(Volts[2]), drn[1], drn[2]);

            /* draw bulk connection */
            if (showBulk()) {
                PenThickLine.Color = getVoltageColor(Volts[bodyTerminal]);
                if (!hasBodyTerminal()) {
                    drawThickLine(g, pnp == -1 ? drn[0] : src[0], body[0]);
                }
                drawThickLine(g, body[0], body[1]);
            }

            /* draw arrow */
            if (!drawDigital()) {
                fillPolygon(g, getVoltageColor(Volts[bodyTerminal]), arrowPoly);
            }

            PenThickLine.Color = getVoltageColor(Volts[0]);

            /* draw gate */
            drawThickLine(g, mPoint1, gate[1]);
            drawThickLine(g, gate[0], gate[2]);
            if (drawDigital() && pnp == -1) {
                drawThickCircle(g, pcircle.X, pcircle.Y, pcircler);
            }

            if ((mFlags & FLAG_SHOWVT) != 0) {
                string s = "" + (vt * pnp);
                drawCenteredText(g, s, X2 + 2, Y2, false);
            }
            mCurCount = updateDotCount(-ids, mCurCount);
            drawDots(g, src[0], src[1], mCurCount);
            drawDots(g, src[1], drn[1], mCurCount);
            drawDots(g, drn[1], drn[0], mCurCount);

            if (showBulk()) {
                curcount_body1 = updateDotCount(diodeCurrent1, curcount_body1);
                curcount_body2 = updateDotCount(diodeCurrent2, curcount_body2);
                drawDots(g, src[0], body[0], -curcount_body1);
                drawDots(g, body[0], drn[0], curcount_body2);
            }

            drawPosts(g);
        }

        /* post 0 = gate,
         * 1 = source for NPN,
         * 2 = drain for NPN,
         * 3 = body (if present)
         * for PNP, 1 is drain, 2 is source */
        public override Point getPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? src[0] : (n == 2) ? drn[0] : body[0];
        }

        public override double getCurrent() { return ids; }

        public override double getPower() {
            return ids * (Volts[2] - Volts[1])
                - diodeCurrent1 * (Volts[1] - Volts[bodyTerminal])
                - diodeCurrent2 * (Volts[2] - Volts[bodyTerminal]);
        }

        public override int getPostCount() { return hasBodyTerminal() ? 4 : 3; }

        public override void setPoints() {
            base.setPoints();

            /* these two flags apply to all mosfets */
            mFlags &= ~FLAGS_GLOBAL;
            mFlags |= globalFlags;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            int hs2 = hs * mDsign;
            if ((mFlags & FLAG_FLIP) != 0) {
                hs2 = -hs2;
            }
            src = newPointArray(3);
            drn = newPointArray(3);
            interpPoint(mPoint1, mPoint2, ref src[0], ref drn[0], 1, -hs2);
            interpPoint(mPoint1, mPoint2, ref src[1], ref drn[1], 1 - 22 / mLen, -hs2);
            interpPoint(mPoint1, mPoint2, ref src[2], ref drn[2], 1 - 22 / mLen, -hs2 * 4 / 3);

            gate = newPointArray(3);
            interpPoint(mPoint1, mPoint2, ref gate[0], ref gate[2], 1 - 28 / mLen, hs2 / 2); /* was 1-20/dn */
            interpPoint(gate[0], gate[2], ref gate[1], .5);

            if (showBulk()) {
                body = newPointArray(2);
                interpPoint(src[0], drn[0], ref body[0], .5);
                interpPoint(src[1], drn[1], ref body[1], .5);
            }

            if (!drawDigital()) {
                if (pnp == 1) {
                    if (!showBulk()) {
                        arrowPoly = calcArrow(src[1], src[0], 10, 4).ToArray();
                    } else {
                        arrowPoly = calcArrow(body[0], body[1], 12, 5).ToArray();
                    }
                } else {
                    if (!showBulk()) {
                        arrowPoly = calcArrow(drn[0], drn[1], 12, 5).ToArray();
                    } else {
                        arrowPoly = calcArrow(body[1], body[0], 12, 5).ToArray();
                    }
                }
            } else if (pnp == -1) {
                interpPoint(mPoint1, mPoint2, ref gate[1], 1 - 36 / mLen);
                int dist = (mDsign < 0) ? 32 : 31;
                pcircle = interpPoint(mPoint1, mPoint2, 1 - dist / mLen);
                pcircler = 3;
            }
        }

        public override void stamp() {
            Cir.StampNonLinear(Nodes[1]);
            Cir.StampNonLinear(Nodes[2]);

            if (hasBodyTerminal()) {
                bodyTerminal = 3;
            } else {
                bodyTerminal = (pnp == -1) ? 2 : 1;
            }

            if (doBodyDiode()) {
                if (pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    diodeB1.stamp(Nodes[1], Nodes[bodyTerminal]);
                    diodeB2.stamp(Nodes[2], Nodes[bodyTerminal]);
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    diodeB1.stamp(Nodes[bodyTerminal], Nodes[1]);
                    diodeB2.stamp(Nodes[bodyTerminal], Nodes[2]);
                }
            }
        }

        bool nonConvergence(double last, double now) {
            double diff = Math.Abs(last - now);

            /* high beta MOSFETs are more sensitive to small differences,
             * so we are more strict about convergence testing */
            if (beta > 1) {
                diff *= 100;
            }

            /* difference of less than 10mV is fine */
            if (diff < .01) {
                return false;
            }
            /* larger differences are fine if value is large */
            if (Cir.SubIterations > 10 && diff < Math.Abs(now) * .001) {
                return false;
            }
            /* if we're having trouble converging, get more lenient */
            if (Cir.SubIterations > 100 && diff < .01 + (Cir.SubIterations - 100) * .0001) {
                return false;
            }
            return true;
        }

        public override void stepFinished() {
            calculate(true);

            /* fix current if body is connected to source or drain */
            if (bodyTerminal == 1) {
                diodeCurrent1 = -diodeCurrent2;
            }
            if (bodyTerminal == 2) {
                diodeCurrent2 = -diodeCurrent1;
            }
        }

        public override void doStep() {
            calculate(false);
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
                vs[0] = Volts[0];
                vs[1] = Volts[1];
                vs[2] = Volts[2];
                if (vs[1] > lastv1 + .5) {
                    vs[1] = lastv1 + .5;
                }
                if (vs[1] < lastv1 - .5) {
                    vs[1] = lastv1 - .5;
                }
                if (vs[2] > lastv2 + .5) {
                    vs[2] = lastv2 + .5;
                }
                if (vs[2] < lastv2 - .5) {
                    vs[2] = lastv2 - .5;
                }
            }

            int source = 1;
            int drain = 2;

            /* if source voltage > drain (for NPN), swap source and drain
             * (opposite for PNP) */
            if (pnp * vs[1] > pnp * vs[2]) {
                source = 2;
                drain = 1;
            }
            int gate = 0;
            double vgs = vs[gate] - vs[source];
            double vds = vs[drain] - vs[source];
            if (!finished && (nonConvergence(lastv1, vs[1]) || nonConvergence(lastv2, vs[2]) || nonConvergence(lastv0, vs[0]))) {
                Cir.Converged = false;
            }
            lastv0 = vs[0];
            lastv1 = vs[1];
            lastv2 = vs[2];
            double realvgs = vgs;
            double realvds = vds;
            vgs *= pnp;
            vds *= pnp;
            ids = 0;
            gm = 0;
            double Gds = 0;
            if (vgs < vt) {
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                ids = vds * Gds;
                mode = 0;
            } else if (vds < vgs - vt) {
                /* linear */
                ids = beta * ((vgs - vt) * vds - vds * vds * .5);
                gm = beta * vds;
                Gds = beta * (vgs - vds - vt);
                mode = 1;
            } else {
                /* saturation; Gds = 0 */
                gm = beta * (vgs - vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                ids = .5 * beta * (vgs - vt) * (vgs - vt) + (vds - (vgs - vt)) * Gds;
                mode = 2;
            }

            if (doBodyDiode()) {
                diodeB1.doStep(pnp * (Volts[bodyTerminal] - Volts[1]));
                diodeCurrent1 = diodeB1.calculateCurrent(pnp * (Volts[bodyTerminal] - Volts[1])) * pnp;
                diodeB2.doStep(pnp * (Volts[bodyTerminal] - Volts[2]));
                diodeCurrent2 = diodeB2.calculateCurrent(pnp * (Volts[bodyTerminal] - Volts[2])) * pnp;
            } else {
                diodeCurrent1 = diodeCurrent2 = 0;
            }

            double ids0 = ids;

            /* flip ids if we swapped source and drain above */
            if (source == 2 && pnp == 1 || source == 1 && pnp == -1) {
                ids = -ids;
            }

            if (finished) {
                return;
            }

            double rs = -pnp * ids0 + Gds * realvds + gm * realvgs;
            Cir.StampMatrix(Nodes[drain], Nodes[drain], Gds);
            Cir.StampMatrix(Nodes[drain], Nodes[source], -Gds - gm);
            Cir.StampMatrix(Nodes[drain], Nodes[gate], gm);

            Cir.StampMatrix(Nodes[source], Nodes[drain], -Gds);
            Cir.StampMatrix(Nodes[source], Nodes[source], Gds + gm);
            Cir.StampMatrix(Nodes[source], Nodes[gate], -gm);

            Cir.StampRightSide(Nodes[drain], rs);
            Cir.StampRightSide(Nodes[source], -rs);
        }

        void getFetInfo(string[] arr, string n) {
            arr[0] = ((pnp == -1) ? "p-" : "n-") + n;
            arr[0] += " (Vt=" + getVoltageText(pnp * vt);
            arr[0] += ", \u03b2=" + beta + ")";
            arr[1] = ((pnp == 1) ? "Ids = " : "Isd = ") + getCurrentText(ids);
            arr[2] = "Vgs = " + getVoltageText(Volts[0] - Volts[pnp == -1 ? 2 : 1]);
            arr[3] = ((pnp == 1) ? "Vds = " : "Vsd = ") + getVoltageText(Volts[2] - Volts[1]);
            arr[4] = (mode == 0) ? "off" : (mode == 1) ? "linear" : "saturation";
            arr[5] = "gm = " + getUnitText(gm, "A/V");
            arr[6] = "P = " + getUnitText(getPower(), "W");
            if (showBulk()) {
                arr[7] = "Ib = " + getUnitText(bodyTerminal == 1 ? -diodeCurrent1 : bodyTerminal == 2 ? diodeCurrent2 : -pnp * (diodeCurrent1 + diodeCurrent2), "A");
            }
        }

        public override void getInfo(string[] arr) {
            getFetInfo(arr, "MOSFET");
        }

        public override string getScopeText(int v) {
            return ((pnp == -1) ? "p-" : "n-") + "MOSFET";
        }

        public override bool canViewInScope() { return true; }

        public override double getVoltageDiff() { return Volts[2] - Volts[1]; }

        public override bool getConnection(int n1, int n2) {
            return !(n1 == 0 || n2 == 0);
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Threshold Voltage", pnp * vt, .01, 5);
            }
            if (n == 1) {
                return new EditInfo(EditInfo.MakeLink("mosfet-beta.html", "Beta"), beta, .01, 5);
            }
            if (n == 2) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Show Bulk",
                    Checked = showBulk()
                };
                return ei;
            }
            if (n == 3) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Swap D/S",
                    Checked = (mFlags & FLAG_FLIP) != 0
                };
                return ei;
            }
            if (n == 4 && !showBulk()) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Digital Symbol",
                    Checked = drawDigital()
                };
                return ei;
            }
            if (n == 4 && showBulk()) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Simulate Body Diode",
                    Checked = (mFlags & FLAG_BODY_DIODE) != 0
                };
                return ei;
            }
            if (n == 5 && doBodyDiode()) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Body Terminal",
                    Checked = (mFlags & FLAG_BODY_TERMINAL) != 0
                };
                return ei;
            }

            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                vt = pnp * ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                beta = lastBeta = ei.Value;
            }
            if (n == 2) {
                globalFlags = (!ei.CheckBox.Checked)
                    ? (globalFlags | FLAG_HIDE_BULK) : (globalFlags & ~(FLAG_HIDE_BULK | FLAG_DIGITAL));
                setPoints();
                ei.NewDialog = true;
            }
            if (n == 3) {
                mFlags = ei.CheckBox.Checked
                    ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
                setPoints();
            }
            if (n == 4 && !showBulk()) {
                globalFlags = ei.CheckBox.Checked
                    ? (globalFlags | FLAG_DIGITAL) : (globalFlags & ~FLAG_DIGITAL);
                setPoints();
            }
            if (n == 4 && showBulk()) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_DIODE);
                ei.NewDialog = true;
            }
            if (n == 5) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_TERMINAL);
                allocNodes();
                setPoints();
            }
        }

        public override double getCurrentIntoNode(int n) {
            if (n == 0) {
                return 0;
            }
            if (n == 3) {
                return -diodeCurrent1 - diodeCurrent2;
            }
            if (n == 1) {
                return ids + diodeCurrent1;
            }
            return -ids + diodeCurrent2;
        }
    }
}
