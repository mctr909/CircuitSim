using System;

namespace Circuit.Elements.Passive {
    class TransformerElmE : BaseElement {
        public const int PRI_T = 0;
        public const int PRI_B = 2;
        public const int SEC_T = 1;
        public const int SEC_B = 3;

        public double PInductance;
        public double Ratio;
        public double CouplingCoef;
        public int Polarity;
        public bool IsTrapezoidal;
        public double[] CurCounts;

        public double[] Currents { get; private set; }
        
        double mCurSourceValue1;
        double mCurSourceValue2;

        double mA1;
        double mA2;
        double mA3;
        double mA4;

        public TransformerElmE() : base() {
            PInductance = 4;
            Ratio = Polarity = 1;
            CouplingCoef = .999;
            Currents = new double[2];
            CurCounts = new double[2];
        }

        public TransformerElmE(StringTokenizer st, bool reverse = false) : base() {
            Currents = new double[2];
            CurCounts = new double[2];
            try {
                PInductance = st.nextTokenDouble();
                Ratio = st.nextTokenDouble();
                Currents[0] = st.nextTokenDouble();
                Currents[1] = st.nextTokenDouble();
                try {
                    CouplingCoef = st.nextTokenDouble();
                } catch {
                    CouplingCoef = 0.99;
                }
            } catch { }
            Polarity = reverse ? -1 : 1;
        }

        public override int CirPostCount { get { return 4; } }

        protected override void cirCalculateCurrent() {
            double voltdiff1 = CirVolts[PRI_T] - CirVolts[PRI_B];
            double voltdiff2 = CirVolts[SEC_T] - CirVolts[SEC_B];
            Currents[0] = voltdiff1 * mA1 + voltdiff2 * mA2 + mCurSourceValue1;
            Currents[1] = voltdiff1 * mA3 + voltdiff2 * mA4 + mCurSourceValue2;
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n < 2) {
                return -Currents[n];
            }
            return Currents[n - 2];
        }

        public override void CirStamp() {
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
            double ts = IsTrapezoidal ? ControlPanel.TimeStep / 2 : ControlPanel.TimeStep;
            mA1 = l2 * deti * ts; /* we multiply dt/2 into a1..a4 here */
            mA2 = -m * deti * ts;
            mA3 = -m * deti * ts;
            mA4 = l1 * deti * ts;
            mCir.StampConductance(CirNodes[0], CirNodes[2], mA1);
            mCir.StampVCCurrentSource(CirNodes[0], CirNodes[2], CirNodes[1], CirNodes[3], mA2);
            mCir.StampVCCurrentSource(CirNodes[1], CirNodes[3], CirNodes[0], CirNodes[2], mA3);
            mCir.StampConductance(CirNodes[1], CirNodes[3], mA4);
            mCir.StampRightSide(CirNodes[0]);
            mCir.StampRightSide(CirNodes[1]);
            mCir.StampRightSide(CirNodes[2]);
            mCir.StampRightSide(CirNodes[3]);
        }

        public override void CirStartIteration() {
            double voltdiff1 = CirVolts[PRI_T] - CirVolts[PRI_B];
            double voltdiff2 = CirVolts[SEC_T] - CirVolts[SEC_B];
            if (IsTrapezoidal) {
                mCurSourceValue1 = voltdiff1 * mA1 + voltdiff2 * mA2 + Currents[0];
                mCurSourceValue2 = voltdiff1 * mA3 + voltdiff2 * mA4 + Currents[1];
            } else {
                mCurSourceValue1 = Currents[0];
                mCurSourceValue2 = Currents[1];
            }
        }

        public override void CirDoStep() {
            mCir.StampCurrentSource(CirNodes[0], CirNodes[2], mCurSourceValue1);
            mCir.StampCurrentSource(CirNodes[1], CirNodes[3], mCurSourceValue2);
        }

        public override void CirReset() {
            /* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
            Currents[0] = Currents[1] = 0;
            CirVolts[PRI_T] = CirVolts[PRI_B] = 0;
            CirVolts[SEC_T] = CirVolts[SEC_B] = 0;
            CurCounts[0] = CurCounts[1] = 0;
            mCurSourceValue1 = mCurSourceValue2 = 0;
        }
    }
}
