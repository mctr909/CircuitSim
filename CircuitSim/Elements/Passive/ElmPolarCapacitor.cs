namespace Circuit.Elements.Passive {
    class ElmPolarCapacitor : ElmCapacitor {
        public double MaxNegativeVoltage;

        public ElmPolarCapacitor() : base() {
            MaxNegativeVoltage = 1;
        }

        public ElmPolarCapacitor(StringTokenizer st) : base(st) {
            MaxNegativeVoltage = st.nextTokenDouble();
        }

        public override void CirIterationFinished() {
            if (VoltageDiff < 0 && VoltageDiff < -MaxNegativeVoltage) {
                Circuit.Stop("耐逆電圧" + Utils.VoltageText(MaxNegativeVoltage) + "を超えました", this);
            }
        }
    }
}
