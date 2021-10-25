using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class TransformerElm : CircuitElm {
        public const int FLAG_REVERSE = 4;

        const int BODY_LEN = 24;

        const int PRI_T = 0;
        const int PRI_B = 2;
        const int SEC_T = 1;
        const int SEC_B = 3;

        double mInductance;
        double mRatio;
        double mCouplingCoef;

        Point[] mPtEnds;
        Point[] mPtCoil;
        Point[] mPtCore;
        double[] mCurrents;
        double[] mCurCounts;

        Point[] mDots;
        int mPolarity;

        double mCurSourceValue1;
        double mCurSourceValue2;

        double mA1;
        double mA2;
        double mA3;
        double mA4;

        string mReferenceName = "T";
        Point mNamePos;

        public TransformerElm(Point pos) : base(pos) {
            mInductance = 4;
            mRatio = mPolarity = 1;
            mNoDiagonal = true;
            mCouplingCoef = .999;
            mCurrents = new double[2];
            mCurCounts = new double[2];
        }

        public TransformerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mCurrents = new double[2];
            mCurCounts = new double[2];
            try {
                mInductance = st.nextTokenDouble();
                mRatio = st.nextTokenDouble();
                mCurrents[0] = st.nextTokenDouble();
                mCurrents[1] = st.nextTokenDouble();
                try {
                    mCouplingCoef = st.nextTokenDouble();
                } catch {
                    mCouplingCoef = 0.99;
                }
                mReferenceName = st.nextToken();
            } catch { }
            mNoDiagonal = true;
            mPolarity = ((mFlags & FLAG_REVERSE) != 0) ? -1 : 1;
        }

        public override int PostCount { get { return 4; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSFORMER; } }

        bool IsTrapezoidal { get { return (mFlags & Inductor.FLAG_BACK_EULER) == 0; } }

        protected override string dump() {
            return mInductance
                + " " + mRatio
                + " " + mCurrents[0]
                + " " + mCurrents[1]
                + " " + mCouplingCoef
                + " " + mReferenceName;
        }

        protected override void calculateCurrent() {
            double voltdiff1 = Volts[PRI_T] - Volts[PRI_B];
            double voltdiff2 = Volts[SEC_T] - Volts[SEC_B];
            mCurrents[0] = voltdiff1 * mA1 + voltdiff2 * mA2 + mCurSourceValue1;
            mCurrents[1] = voltdiff1 * mA3 + voltdiff2 * mA4 + mCurSourceValue2;
        }

        public override Point GetPost(int n) {
            return mPtEnds[n];
        }

        public override bool GetConnection(int n1, int n2) {
            if (comparePair(n1, n2, 0, 2)) {
                return true;
            }
            if (comparePair(n1, n2, 1, 3)) {
                return true;
            }
            return false;
        }

        public override double GetCurrentIntoNode(int n) {
            if (n < 2) {
                return -mCurrents[n];
            }
            return mCurrents[n - 2];
        }

        public override void Stamp() {
            /* equations for transformer:
             *   v1 = L1 di1/dt + M  di2/dt
             *   v2 = M  di1/dt + L2 di2/dt
             * we invert that to get:
             *   di1/dt = a1 v1 + a2 v2
             *   di2/dt = a3 v1 + a4 v2
             * integrate di1/dt using trapezoidal approx and we get:
             *   i1(t2) = i1(t1) + dt/2 (i1(t1) + i1(t2))
             *          = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1) +
             *                     a1 dt/2 v1(t2) + a2 dt/2 v2(t2)
             * the norton equivalent of this for i1 is:
             *  a. current source, I = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1)
             *  b. resistor, G = a1 dt/2
             *  c. current source controlled by voltage v2, G = a2 dt/2
             * and for i2:
             *  a. current source, I = i2(t1) + a3 dt/2 v1(t1) + a4 dt/2 v2(t1)
             *  b. resistor, G = a3 dt/2
             *  c. current source controlled by voltage v2, G = a4 dt/2
             *
             * For backward euler,
             *
             *   i1(t2) = i1(t1) + a1 dt v1(t2) + a2 dt v2(t2)
             *
             * So the current source value is just i1(t1) and we use
             * dt instead of dt/2 for the resistor and VCCS.
             *
             * first winding goes from node 0 to 2, second is from 1 to 3 */
            double l1 = mInductance;
            double l2 = mInductance * mRatio * mRatio;
            double m = mCouplingCoef * Math.Sqrt(l1 * l2);
            /* build inverted matrix */
            double deti = 1 / (l1 * l2 - m * m);
            double ts = IsTrapezoidal ? ControlPanel.TimeStep / 2 : ControlPanel.TimeStep;
            mA1 = l2 * deti * ts; /* we multiply dt/2 into a1..a4 here */
            mA2 = -m * deti * ts;
            mA3 = -m * deti * ts;
            mA4 = l1 * deti * ts;
            mCir.StampConductance(Nodes[0], Nodes[2], mA1);
            mCir.StampVCCurrentSource(Nodes[0], Nodes[2], Nodes[1], Nodes[3], mA2);
            mCir.StampVCCurrentSource(Nodes[1], Nodes[3], Nodes[0], Nodes[2], mA3);
            mCir.StampConductance(Nodes[1], Nodes[3], mA4);
            mCir.StampRightSide(Nodes[0]);
            mCir.StampRightSide(Nodes[1]);
            mCir.StampRightSide(Nodes[2]);
            mCir.StampRightSide(Nodes[3]);
        }

        public override void StartIteration() {
            double voltdiff1 = Volts[PRI_T] - Volts[PRI_B];
            double voltdiff2 = Volts[SEC_T] - Volts[SEC_B];
            if (IsTrapezoidal) {
                mCurSourceValue1 = voltdiff1 * mA1 + voltdiff2 * mA2 + mCurrents[0];
                mCurSourceValue2 = voltdiff1 * mA3 + voltdiff2 * mA4 + mCurrents[1];
            } else {
                mCurSourceValue1 = mCurrents[0];
                mCurSourceValue2 = mCurrents[1];
            }
        }

        public override void DoStep() {
            mCir.StampCurrentSource(Nodes[0], Nodes[2], mCurSourceValue1);
            mCir.StampCurrentSource(Nodes[1], Nodes[3], mCurSourceValue2);
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override void SetPoints() {
            var width = Math.Max(BODY_LEN, Math.Abs(P2.X - P1.X));
            var height = Math.Max(BODY_LEN, Math.Abs(P2.Y - P1.Y));
            if (P2.X == P1.X) {
                P2.Y = P1.Y;
            }
            base.SetPoints();
            mPoint2.Y = mPoint1.Y;
            mPtEnds = new Point[4];
            mPtCoil = new Point[4];
            mPtCore = new Point[4];
            mPtEnds[0] = mPoint1;
            mPtEnds[1] = mPoint2;
            interpPoint(ref mPtEnds[2], 0, -mDsign * height);
            interpPoint(ref mPtEnds[3], 1, -mDsign * height);
            var ce = 0.5 - 10.0 / width;
            var cd = 0.5 - 1.0 / width;
            for (int i = 0; i != 4; i += 2) {
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCoil[i], ce);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCoil[i + 1], 1 - ce);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCore[i], cd);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCore[i + 1], 1 - cd);
            }
            if (-1 == mPolarity) {
                mDots = new Point[2];
                var dotp = Math.Abs(7.0 / height);
                Utils.InterpPoint(mPtCoil[0], mPtCoil[2], ref mDots[0], dotp, -7 * mDsign);
                Utils.InterpPoint(mPtCoil[3], mPtCoil[1], ref mDots[1], dotp, -7 * mDsign);
                var x = mPtEnds[1];
                mPtEnds[1] = mPtEnds[3];
                mPtEnds[3] = x;
                x = mPtCoil[1];
                mPtCoil[1] = mPtCoil[3];
                mPtCoil[3] = x;
            } else {
                mDots = null;
            }
            setNamePos();
        }

        void setNamePos() {
            var wn = Context.GetTextSize(mReferenceName).Width;
            mNamePos = new Point((int)(mPtCore[0].X - wn / 2 + 2), mPtCore[0].Y - 8);
        }

        public override void Reset() {
            /* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
            mCurrents[0] = mCurrents[1] = 0;
            Volts[PRI_T] = Volts[PRI_B] = 0;
            Volts[SEC_T] = Volts[SEC_B] = 0;
            mCurCounts[0] = mCurCounts[1] = 0;
            mCurSourceValue1 = mCurSourceValue2 = 0;
        }

        public override void Draw(CustomGraphics g) {
            drawVoltage(PRI_T, mPtEnds[0], mPtCoil[0]);
            drawVoltage(SEC_T, mPtEnds[1], mPtCoil[1]);
            drawVoltage(PRI_B, mPtEnds[2], mPtCoil[2]);
            drawVoltage(SEC_B, mPtEnds[3], mPtCoil[3]);

            drawCoil(mPtCoil[0], mPtCoil[2], Volts[PRI_T], Volts[PRI_B], 90 * mDsign);
            drawCoil(mPtCoil[1], mPtCoil[3], Volts[SEC_T], Volts[SEC_B], -90 * mDsign * mPolarity);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawLine(mPtCore[0], mPtCore[2]);
            g.DrawLine(mPtCore[1], mPtCore[3]);
            if (mDots != null) {
                g.DrawCircle(mDots[0], 2.5f);
                g.DrawCircle(mDots[1], 2.5f);
            }

            mCurCounts[0] = updateDotCount(mCurrents[0], mCurCounts[0]);
            mCurCounts[1] = updateDotCount(mCurrents[1], mCurCounts[1]);
            for (int i = 0; i != 2; i++) {
                drawDots(mPtEnds[i], mPtCoil[i], mCurCounts[i]);
                drawDots(mPtCoil[i], mPtCoil[i + 2], mCurCounts[i]);
                drawDots(mPtEnds[i + 2], mPtCoil[i + 2], -mCurCounts[i]);
            }

            drawPosts();
            setBbox(mPtEnds[0], mPtEnds[mPolarity == 1 ? 3 : 1], 0);

            if (ControlPanel.ChkShowName.Checked) {
                g.DrawLeftText(mReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "トランス";
            arr[1] = "L = " + Utils.UnitText(mInductance, "H");
            arr[2] = "Ratio = 1:" + mRatio;
            arr[3] = "Vd1 = " + Utils.VoltageText(Volts[PRI_T] - Volts[PRI_B]);
            arr[4] = "Vd2 = " + Utils.VoltageText(Volts[SEC_T] - Volts[SEC_B]);
            arr[5] = "I1 = " + Utils.CurrentText(mCurrents[0]);
            arr[6] = "I2 = " + Utils.CurrentText(mCurrents[1]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("一次側インダクタンス(H)", mInductance, .01, 5);
            }
            if (n == 1) {
                return new ElementInfo("二次側巻数比", mRatio, 1, 10).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("結合係数", mCouplingCoef, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = mReferenceName;
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "台形近似",
                    Checked = IsTrapezoidal
                };
                return ei;
            }
            if (n == 5) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "極性反転",
                    Checked = mPolarity == -1
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                mInductance = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                mRatio = ei.Value;
            }
            if (n == 2 && ei.Value > 0 && ei.Value < 1) {
                mCouplingCoef = ei.Value;
            }
            if (n == 3) {
                mReferenceName = ei.Textf.Text;
                setNamePos();
            }
            if (n == 4) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            if (n == 5) {
                mPolarity = ei.CheckBox.Checked ? -1 : 1;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_REVERSE;
                } else {
                    mFlags &= ~FLAG_REVERSE;
                }
                SetPoints();
            }
        }
    }
}
