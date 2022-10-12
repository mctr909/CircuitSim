namespace Circuit.Elements.Passive {
    class ElmPot : BaseElement {
        public const int V_L = 0;
        public const int V_R = 1;
        public const int V_S = 2;

        public double Position = 0.5;
        public double MaxResistance = 1000;
        public double CurCount1 = 0;
        public double CurCount2 = 0;
        public double CurCount3 = 0;

        public double Resistance1 { get; private set; }
        public double Resistance2 { get; private set; }
        public double Current1 { get; private set; }
        public double Current2 { get; private set; }
        public double Current3 { get; private set; }

        public override int PostCount { get { return 3; } }

        public override void Reset() {
            CurCount1 = CurCount2 = CurCount3 = 0;
            base.Reset();
        }

        public override void AnaStamp() {
            Resistance1 = MaxResistance * Position;
            Resistance2 = MaxResistance * (1 - Position);
            Circuit.StampResistor(Nodes[0], Nodes[2], Resistance1);
            Circuit.StampResistor(Nodes[2], Nodes[1], Resistance2);
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            if (Resistance1 == 0) {
                return; /* avoid NaN */
            }
            Current1 = (Volts[V_L] - Volts[V_S]) / Resistance1;
            Current2 = (Volts[V_R] - Volts[V_S]) / Resistance2;
            Current3 = -Current1 - Current2;
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Current1;
            }
            if (n == 1) {
                return -Current2;
            }
            return -Current3;
        }
    }
}
