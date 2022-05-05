namespace Circuit.Elements.Input {
    class RailElm : VoltageElm {
        public RailElm() : base(WAVEFORM.DC) { }

        public RailElm(WAVEFORM wf) : base(wf) { }

        public RailElm(StringTokenizer st): base(st) { }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            }
        }
    }
}
