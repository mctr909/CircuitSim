using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
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

        public TransformerElm(int xx, int yy) : base(xx, yy) {
            inductance = 4;
            ratio = polarity = 1;
            width = 32;
            mNoDiagonal = true;
            couplingCoef = .999;
            current = new double[2];
            curcount = new double[2];
        }

        public TransformerElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            width = Math.Max(32, Math.Abs(yb - ya));
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

        protected override string dump() {
            return inductance
                + " " + ratio
                + " " + current[0]
                + " " + current[1]
                + " " + couplingCoef;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.TRANSFORMER; }

        public override void Drag(int xx, int yy) {
            xx = Sim.snapGrid(xx);
            yy = Sim.snapGrid(yy);
            width = Math.Max(32, Math.Abs(yy - Y1));
            if (xx == X1) {
                yy = Y1;
            }
            X2 = xx;
            Y2 = yy;
            SetPoints();
        }

        public override void Draw(Graphics g) {
            drawThickLine(g, getVoltageColor(Volts[PRI_T]), ptEnds[0], ptCoil[0]);
            drawThickLine(g, getVoltageColor(Volts[SEC_T]), ptEnds[1], ptCoil[1]);
            drawThickLine(g, getVoltageColor(Volts[PRI_B]), ptEnds[2], ptCoil[2]);
            drawThickLine(g, getVoltageColor(Volts[SEC_B]), ptEnds[3], ptCoil[3]);

            drawCoil(g,  90,            ptCoil[0], ptCoil[2], Volts[PRI_T], Volts[PRI_B]);
            drawCoil(g, -90 * polarity, ptCoil[1], ptCoil[3], Volts[SEC_T], Volts[SEC_B]);

            PenLine.Color = needsHighlight() ? SelectColor : LightGrayColor;
            PenThickLine.Color = PenLine.Color;
            drawThickLine(g, ptCore[0], ptCore[2]);
            drawThickLine(g, ptCore[1], ptCore[3]);
            if (dots != null) {
                g.DrawArc(PenLine, dots[0].X - 2, dots[0].Y - 2, 5, 5, 0, 360);
                g.DrawArc(PenLine, dots[1].X - 2, dots[1].Y - 2, 5, 5, 0, 360);
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
            ptEnds = newPointArray(4);
            ptCoil = newPointArray(4);
            ptCore = newPointArray(4);
            ptEnds[0] = mPoint1;
            ptEnds[1] = mPoint2;
            interpPoint(mPoint1, mPoint2, ref ptEnds[2], 0, -mDsign * width);
            interpPoint(mPoint1, mPoint2, ref ptEnds[3], 1, -mDsign * width);
            double ce = .5 - 16 / mLen;
            double cd = .5 - 2 / mLen;
            int i;
            for (i = 0; i != 4; i += 2) {
                interpPoint(ptEnds[i], ptEnds[i + 1], ref ptCoil[i], ce);
                interpPoint(ptEnds[i], ptEnds[i + 1], ref ptCoil[i + 1], 1 - ce);
                interpPoint(ptEnds[i], ptEnds[i + 1], ref ptCore[i], cd);
                interpPoint(ptEnds[i], ptEnds[i + 1], ref ptCore[i + 1], 1 - cd);
            }
            if (polarity == -1) {
                dots = new Point[2];
                double dotp = Math.Abs(7.0 / width);
                dots[0] = interpPoint(ptCoil[0], ptCoil[2], dotp, -7 * mDsign);
                dots[1] = interpPoint(ptCoil[3], ptCoil[1], dotp, -7 * mDsign);
                var x = ptEnds[1]; ptEnds[1] = ptEnds[3]; ptEnds[3] = x;
                x = ptCoil[1]; ptCoil[1] = ptCoil[3]; ptCoil[3] = x;
            } else {
                dots = null;
            }
        }

        public override Point GetPost(int n) {
            return ptEnds[n];
        }

        public override void Reset() {
            // need to set current-source values here in case one of the nodes is node 0.  In that case
            // calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
            // startIteration() gets called
            current[0] = current[1] = 0;
            Volts[PRI_T] = Volts[PRI_B] = 0;
            Volts[SEC_T] = Volts[SEC_B] = 0;
            curcount[0] = curcount[1] = 0;
            curSourceValue1 = curSourceValue2 = 0;
        }

        public override void Stamp() {
            // equations for transformer:
            //   v1 = L1 di1/dt + M  di2/dt
            //   v2 = M  di1/dt + L2 di2/dt
            // we invert that to get:
            //   di1/dt = a1 v1 + a2 v2
            //   di2/dt = a3 v1 + a4 v2
            // integrate di1/dt using trapezoidal approx and we get:
            //   i1(t2) = i1(t1) + dt/2 (i1(t1) + i1(t2))
            //          = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1) +
            //                     a1 dt/2 v1(t2) + a2 dt/2 v2(t2)
            // the norton equivalent of this for i1 is:
            //  a. current source, I = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1)
            //  b. resistor, G = a1 dt/2
            //  c. current source controlled by voltage v2, G = a2 dt/2
            // and for i2:
            //  a. current source, I = i2(t1) + a3 dt/2 v1(t1) + a4 dt/2 v2(t1)
            //  b. resistor, G = a3 dt/2
            //  c. current source controlled by voltage v2, G = a4 dt/2
            //
            // For backward euler,
            //
            //   i1(t2) = i1(t1) + a1 dt v1(t2) + a2 dt v2(t2)
            //
            // So the current source value is just i1(t1) and we use
            // dt instead of dt/2 for the resistor and VCCS.
            //
            // first winding goes from node 0 to 2, second is from 1 to 3
            double l1 = inductance;
            double l2 = inductance * ratio * ratio;
            double m = couplingCoef * Math.Sqrt(l1 * l2);
            // build inverted matrix
            double deti = 1 / (l1 * l2 - m * m);
            double ts = IsTrapezoidal ? Sim.timeStep / 2 : Sim.timeStep;
            a1 = l2 * deti * ts; // we multiply dt/2 into a1..a4 here
            a2 = -m * deti * ts;
            a3 = -m * deti * ts;
            a4 = l1 * deti * ts;
            Cir.StampConductance(Nodes[0], Nodes[2], a1);
            Cir.StampVCCurrentSource(Nodes[0], Nodes[2], Nodes[1], Nodes[3], a2);
            Cir.StampVCCurrentSource(Nodes[1], Nodes[3], Nodes[0], Nodes[2], a3);
            Cir.StampConductance(Nodes[1], Nodes[3], a4);
            Cir.StampRightSide(Nodes[0]);
            Cir.StampRightSide(Nodes[1]);
            Cir.StampRightSide(Nodes[2]);
            Cir.StampRightSide(Nodes[3]);
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
            Cir.StampCurrentSource(Nodes[0], Nodes[2], curSourceValue1);
            Cir.StampCurrentSource(Nodes[1], Nodes[3], curSourceValue2);
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
            arr[1] = "L = " + getUnitText(inductance, "H");
            arr[2] = "Ratio = 1:" + ratio;
            arr[3] = "Vd1 = " + getVoltageText(Volts[PRI_T] - Volts[PRI_B]);
            arr[4] = "Vd2 = " + getVoltageText(Volts[SEC_T] - Volts[SEC_B]);
            arr[5] = "I1 = " + getCurrentText(current[0]);
            arr[6] = "I2 = " + getCurrentText(current[1]);
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

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Primary Inductance (H)", inductance, .01, 5);
            }
            if (n == 1) {
                return new EditInfo("Ratio", ratio, 1, 10).SetDimensionless();
            }
            if (n == 2) {
                return new EditInfo("Coupling Coefficient", couplingCoef, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Trapezoidal Approximation",
                    Checked = IsTrapezoidal
                };
                return ei;
            }
            if (n == 4) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Swap Secondary Polarity",
                    Checked = polarity == -1
                };
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
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
