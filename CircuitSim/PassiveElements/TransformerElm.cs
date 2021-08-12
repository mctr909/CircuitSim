using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.PassiveElements {
    class TransformerElm : CircuitElm {
        public const int FLAG_REVERSE = 4;

        const int PRI_T = 0;
        const int PRI_B = 2;
        const int SEC_T = 1;
        const int SEC_B = 3;

        double inductance;
        double ratio;
        double couplingCoef;
        Point[] ptEnds;
        Point[] ptCoil;
        Point[] ptCore;
        double[] current;
        double[] curcount;

        Point[] dots;
        int width;
        int polarity;

        double curSourceValue1;
        double curSourceValue2;

        double a1;
        double a2;
        double a3;
        double a4;

        bool IsTrapezoidal { get { return (mFlags & Inductor.FLAG_BACK_EULER) == 0; } }

        public TransformerElm(Point pos) : base(pos) {
            inductance = 4;
            ratio = polarity = 1;
            width = 32;
            mNoDiagonal = true;
            couplingCoef = .999;
            current = new double[2];
            curcount = new double[2];
        }

        public TransformerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            width = Math.Max(32, Math.Abs(p2.Y - p1.Y));
            inductance = st.nextTokenDouble();
            ratio =  st.nextTokenDouble();
            current = new double[2];
            curcount = new double[2];
            current[0] = st.nextTokenDouble();
            current[1] = st.nextTokenDouble();
            try {
                couplingCoef = st.nextTokenDouble();
            } catch {
                couplingCoef = .999;
            }
            mNoDiagonal = true;
            polarity = ((mFlags & FLAG_REVERSE) != 0) ? -1 : 1;
        }

        public override int PostCount { get { return 4; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSFORMER; } }

        protected override string dump() {
            return inductance
                + " " + ratio
                + " " + current[0]
                + " " + current[1]
                + " " + couplingCoef;
        }

        public override void Drag(Point pos) {
            pos = Sim.SnapGrid(pos);
            width = Math.Max(32, Math.Abs(pos.Y - P1.Y));
            if (pos.X == P1.X) {
                pos.Y = P1.Y;
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override void Draw(CustomGraphics g) {
            g.DrawThickLine(getVoltageColor(Volts[PRI_T]), ptEnds[0], ptCoil[0]);
            g.DrawThickLine(getVoltageColor(Volts[SEC_T]), ptEnds[1], ptCoil[1]);
            g.DrawThickLine(getVoltageColor(Volts[PRI_B]), ptEnds[2], ptCoil[2]);
            g.DrawThickLine(getVoltageColor(Volts[SEC_B]), ptEnds[3], ptCoil[3]);

            drawCoil(g, ptCoil[0], ptCoil[2], Volts[PRI_T], Volts[PRI_B], 90 * mDsign);
            drawCoil(g, ptCoil[1], ptCoil[3], Volts[SEC_T], Volts[SEC_B], -90 * mDsign * polarity);

            var c = NeedsHighlight ? SelectColor : GrayColor;
            g.LineColor = c;
            g.ThickLineColor = c;
            g.DrawThickLine(ptCore[0], ptCore[2]);
            g.DrawThickLine(ptCore[1], ptCore[3]);
            if (dots != null) {
                g.DrawCircle(dots[0], 2.5f);
                g.DrawCircle(dots[1], 2.5f);
            }

            curcount[0] = updateDotCount(current[0], curcount[0]);
            curcount[1] = updateDotCount(current[1], curcount[1]);
            for (int i = 0; i != 2; i++) {
                drawDots(g, ptEnds[i], ptCoil[i], curcount[i]);
                drawDots(g, ptCoil[i], ptCoil[i + 2], curcount[i]);
                drawDots(g, ptEnds[i + 2], ptCoil[i + 2], -curcount[i]);
            }

            drawPosts(g);
            setBbox(ptEnds[0], ptEnds[polarity == 1 ? 3 : 1], 0);
        }

        public override void SetPoints() {
            base.SetPoints();
            mPoint2.Y = mPoint1.Y;
            ptEnds = new Point[4];
            ptCoil = new Point[4];
            ptCore = new Point[4];
            ptEnds[0] = mPoint1;
            ptEnds[1] = mPoint2;
            Utils.InterpPoint(mPoint1, mPoint2, ref ptEnds[2], 0, -mDsign * width);
            Utils.InterpPoint(mPoint1, mPoint2, ref ptEnds[3], 1, -mDsign * width);
            double ce = .5 - 16 / mLen;
            double cd = .5 - 2 / mLen;
            int i;
            for (i = 0; i != 4; i += 2) {
                Utils.InterpPoint(ptEnds[i], ptEnds[i + 1], ref ptCoil[i], ce);
                Utils.InterpPoint(ptEnds[i], ptEnds[i + 1], ref ptCoil[i + 1], 1 - ce);
                Utils.InterpPoint(ptEnds[i], ptEnds[i + 1], ref ptCore[i], cd);
                Utils.InterpPoint(ptEnds[i], ptEnds[i + 1], ref ptCore[i + 1], 1 - cd);
            }
            if (polarity == -1) {
                dots = new Point[2];
                double dotp = Math.Abs(7.0 / width);
                Utils.InterpPoint(ptCoil[0], ptCoil[2], ref dots[0], dotp, -7 * mDsign);
                Utils.InterpPoint(ptCoil[3], ptCoil[1], ref dots[1], dotp, -7 * mDsign);
                var x = ptEnds[1];
                ptEnds[1] = ptEnds[3];
                ptEnds[3] = x;
                x = ptCoil[1];
                ptCoil[1] = ptCoil[3];
                ptCoil[3] = x;
            } else {
                dots = null;
            }
        }

        public override Point GetPost(int n) {
            return ptEnds[n];
        }

        public override void Reset() {
            /* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
            current[0] = current[1] = 0;
            Volts[PRI_T] = Volts[PRI_B] = 0;
            Volts[SEC_T] = Volts[SEC_B] = 0;
            curcount[0] = curcount[1] = 0;
            curSourceValue1 = curSourceValue2 = 0;
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
            double l1 = inductance;
            double l2 = inductance * ratio * ratio;
            double m = couplingCoef * Math.Sqrt(l1 * l2);
            /* build inverted matrix */
            double deti = 1 / (l1 * l2 - m * m);
            double ts = IsTrapezoidal ? ControlPanel.TimeStep / 2 : ControlPanel.TimeStep;
            a1 = l2 * deti * ts; /* we multiply dt/2 into a1..a4 here */
            a2 = -m * deti * ts;
            a3 = -m * deti * ts;
            a4 = l1 * deti * ts;
            mCir.StampConductance(Nodes[0], Nodes[2], a1);
            mCir.StampVCCurrentSource(Nodes[0], Nodes[2], Nodes[1], Nodes[3], a2);
            mCir.StampVCCurrentSource(Nodes[1], Nodes[3], Nodes[0], Nodes[2], a3);
            mCir.StampConductance(Nodes[1], Nodes[3], a4);
            mCir.StampRightSide(Nodes[0]);
            mCir.StampRightSide(Nodes[1]);
            mCir.StampRightSide(Nodes[2]);
            mCir.StampRightSide(Nodes[3]);
        }

        public override void StartIteration() {
            double voltdiff1 = Volts[PRI_T] - Volts[PRI_B];
            double voltdiff2 = Volts[SEC_T] - Volts[SEC_B];
            if (IsTrapezoidal) {
                curSourceValue1 = voltdiff1 * a1 + voltdiff2 * a2 + current[0];
                curSourceValue2 = voltdiff1 * a3 + voltdiff2 * a4 + current[1];
            } else {
                curSourceValue1 = current[0];
                curSourceValue2 = current[1];
            }
        }

        public override void DoStep() {
            mCir.StampCurrentSource(Nodes[0], Nodes[2], curSourceValue1);
            mCir.StampCurrentSource(Nodes[1], Nodes[3], curSourceValue2);
        }

        protected override void calculateCurrent() {
            double voltdiff1 = Volts[PRI_T] - Volts[PRI_B];
            double voltdiff2 = Volts[SEC_T] - Volts[SEC_B];
            current[0] = voltdiff1 * a1 + voltdiff2 * a2 + curSourceValue1;
            current[1] = voltdiff1 * a3 + voltdiff2 * a4 + curSourceValue2;
        }

        public override double GetCurrentIntoNode(int n) {
            if (n < 2) {
                return -current[n];
            }
            return current[n - 2];
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "transformer";
            arr[1] = "L = " + Utils.UnitText(inductance, "H");
            arr[2] = "Ratio = 1:" + ratio;
            arr[3] = "Vd1 = " + Utils.VoltageText(Volts[PRI_T] - Volts[PRI_B]);
            arr[4] = "Vd2 = " + Utils.VoltageText(Volts[SEC_T] - Volts[SEC_B]);
            arr[5] = "I1 = " + Utils.CurrentText(current[0]);
            arr[6] = "I2 = " + Utils.CurrentText(current[1]);
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

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Primary Inductance (H)", inductance, .01, 5);
            }
            if (n == 1) {
                return new ElementInfo("Ratio", ratio, 1, 10).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("Coupling Coefficient", couplingCoef, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Trapezoidal Approximation",
                    Checked = IsTrapezoidal
                };
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Swap Secondary Polarity",
                    Checked = polarity == -1
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                inductance = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ratio = ei.Value;
            }
            if (n == 2 && ei.Value > 0 && ei.Value < 1) {
                couplingCoef = ei.Value;
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            if (n == 4) {
                polarity = ei.CheckBox.Checked ? -1 : 1;
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
