﻿namespace Circuit.Elements.Passive {
    class PotElmE : BaseElement {
        public const int V_L = 0;
        public const int V_R = 1;
        public const int V_S = 2;

        public double Position;
        public double MaxResistance;
        public double CurCount1;
        public double CurCount2;
        public double CurCount3;

        public double Resistance1 { get; private set; }
        public double Resistance2 { get; private set; }
        public double Current1 { get; private set; }
        public double Current2 { get; private set; }
        public double Current3 { get; private set; }

        public PotElmE() : base() {
            MaxResistance = 1000;
            Position = 0.5;
        }

        public PotElmE(StringTokenizer st) : base() {
            try {
                MaxResistance = st.nextTokenDouble();
                Position = st.nextTokenDouble();
            } catch { }
        }

        public override int CirPostCount { get { return 3; } }

        protected override void cirCalculateCurrent() {
            if (Resistance1 == 0) {
                return; /* avoid NaN */
            }
            Current1 = (CirVolts[V_L] - CirVolts[V_S]) / Resistance1;
            Current2 = (CirVolts[V_R] - CirVolts[V_S]) / Resistance2;
            Current3 = -Current1 - Current2;
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Current1;
            }
            if (n == 1) {
                return -Current2;
            }
            return -Current3;
        }

        public override void CirStamp() {
            Resistance1 = MaxResistance * Position;
            Resistance2 = MaxResistance * (1 - Position);
            mCir.StampResistor(CirNodes[0], CirNodes[2], Resistance1);
            mCir.StampResistor(CirNodes[2], CirNodes[1], Resistance2);
        }

        public override void CirReset() {
            CurCount1 = CurCount2 = CurCount3 = 0;
            base.CirReset();
        }
    }
}