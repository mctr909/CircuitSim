namespace Circuit.Elements.Input {
    class ElmRail : ElmVoltage {
        public ElmRail() : base(WAVEFORM.DC) { }

        public ElmRail(WAVEFORM wf) : base(wf) { }

        public ElmRail(StringTokenizer st): base(st) { }

        public override int PostCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            if (WaveForm == WAVEFORM.DC) {
                Circuit.StampVoltageSource(0, Nodes[0], mVoltSource, GetVoltage());
            } else {
                Circuit.StampVoltageSource(0, Nodes[0], mVoltSource);
            }
        }
    }
}
