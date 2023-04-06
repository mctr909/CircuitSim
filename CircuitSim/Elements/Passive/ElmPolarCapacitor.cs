namespace Circuit.Elements.Passive {
    class ElmPolarCapacitor : ElmCapacitor {
        public double MaxNegativeVoltage = 1.0;

        public override void CirIterationFinished() {
            if (GetVoltageDiff() < 0 && GetVoltageDiff() < -MaxNegativeVoltage) {
                Circuit.Stop("耐逆電圧" + Utils.VoltageText(MaxNegativeVoltage) + "を超えました", this);
            }
        }
    }
}
