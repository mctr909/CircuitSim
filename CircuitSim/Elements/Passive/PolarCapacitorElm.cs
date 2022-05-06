namespace Circuit.Elements.Passive {
    class PolarCapacitorElm : CapacitorElm {
        public double MaxNegativeVoltage;

        public PolarCapacitorElm() : base() {
            MaxNegativeVoltage = 1;
        }

        public PolarCapacitorElm(StringTokenizer st) : base(st) {
            MaxNegativeVoltage = st.nextTokenDouble();
        }

        public override void CirStepFinished() {
            if (VoltageDiff < 0 && VoltageDiff < -MaxNegativeVoltage) {
                mCir.Stop("耐逆電圧" + Utils.VoltageText(MaxNegativeVoltage) + "を超えました", this);
            }
        }
    }
}
