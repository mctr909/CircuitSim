using System;

namespace Circuit.Elements.Passive {
    class ElmTransformer : BaseElement {
        public const int PRI_T = 0;
        public const int PRI_B = 2;
        public const int SEC_T = 1;
        public const int SEC_B = 3;

        public double PInductance = 0.01;
        public double Ratio = 1.0;
        public double CouplingCoef = 0.999;
        public int Polarity = 1;
        public double[] Currents = new double[2];
        public double[] CurCounts = new double[2];

        double mCurSourceValue1;
        double mCurSourceValue2;

        double mA1;
        double mA2;
        double mA3;
        double mA4;

        public override int TermCount { get { return 4; } }

        public override void Reset() {
            /* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
            Currents[0] = Currents[1] = 0;
            Volts[PRI_T] = Volts[PRI_B] = 0;
            Volts[SEC_T] = Volts[SEC_B] = 0;
            CurCounts[0] = CurCounts[1] = 0;
            mCurSourceValue1 = mCurSourceValue2 = 0;
        }

        public override bool AnaGetConnection(int n1, int n2) {
            if (ComparePair(n1, n2, 0, 2)) {
                return true;
            }
            if (ComparePair(n1, n2, 1, 3)) {
                return true;
            }
            return false;
        }

        public override void AnaStamp() {
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
            double l1 = PInductance;
            double l2 = PInductance * Ratio * Ratio;
            double m = CouplingCoef * Math.Sqrt(l1 * l2);
            /* build inverted matrix */
            double deti = 1 / (l1 * l2 - m * m);
            double ts = ControlPanel.TimeStep / 2;
            mA1 = l2 * deti * ts; /* we multiply dt/2 into a1..a4 here */
            mA2 = -m * deti * ts;
            mA3 = -m * deti * ts;
            mA4 = l1 * deti * ts;
            var pre_t = Nodes[PRI_T] - 1;
            var pre_b = Nodes[PRI_B] - 1;
            var sec_t = Nodes[SEC_T] - 1;
            var sec_b = Nodes[SEC_B] - 1;

            Circuit.Matrix[pre_t, pre_t] += mA1;
            Circuit.Matrix[pre_b, pre_b] += mA1;
            Circuit.Matrix[pre_t, pre_b] -= mA1;
            Circuit.Matrix[pre_b, pre_t] -= mA1;

            Circuit.Matrix[pre_t, sec_t] += mA2;
            Circuit.Matrix[pre_b, sec_b] += mA2;
            Circuit.Matrix[pre_t, sec_b] -= mA2;
            Circuit.Matrix[pre_b, sec_t] -= mA2;

            Circuit.Matrix[sec_t, pre_t] += mA3;
            Circuit.Matrix[sec_b, pre_b] += mA3;
            Circuit.Matrix[sec_t, pre_b] -= mA3;
            Circuit.Matrix[sec_b, pre_t] -= mA3;

            Circuit.Matrix[sec_t, sec_t] += mA4;
            Circuit.Matrix[sec_b, sec_b] += mA4;
            Circuit.Matrix[sec_t, sec_b] -= mA4;
            Circuit.Matrix[sec_b, sec_t] -= mA4;

            Circuit.RowInfo[pre_t].RightChanges = true;
            Circuit.RowInfo[sec_t].RightChanges = true;
            Circuit.RowInfo[pre_b].RightChanges = true;
            Circuit.RowInfo[sec_b].RightChanges = true;
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n < 2) {
                return -Currents[n];
            }
            return Currents[n - 2];
        }

        public override void CirPrepareIteration() {
            var voltDiffP = Volts[PRI_T] - Volts[PRI_B];
            var voltDiffS = Volts[SEC_T] - Volts[SEC_B];
            mCurSourceValue1 = voltDiffP * mA1 + voltDiffS * mA2 + Currents[0];
            mCurSourceValue2 = voltDiffP * mA3 + voltDiffS * mA4 + Currents[1];
        }

        public override void CirDoIteration() {
            var r = Circuit.RowInfo[Nodes[PRI_T] - 1].MapRow;
            Circuit.RightSide[r] -= mCurSourceValue1;
            r = Circuit.RowInfo[Nodes[PRI_B] - 1].MapRow;
            Circuit.RightSide[r] += mCurSourceValue1;
            r = Circuit.RowInfo[Nodes[SEC_T] - 1].MapRow;
            Circuit.RightSide[r] -= mCurSourceValue2;
            r = Circuit.RowInfo[Nodes[SEC_B] - 1].MapRow;
            Circuit.RightSide[r] += mCurSourceValue2;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            var voltDiffP = Volts[PRI_T] - Volts[PRI_B];
            var voltDiffS = Volts[SEC_T] - Volts[SEC_B];
            Currents[0] = voltDiffP * mA1 + voltDiffS * mA2 + mCurSourceValue1;
            Currents[1] = voltDiffP * mA3 + voltDiffS * mA4 + mCurSourceValue2;
        }
    }
}
