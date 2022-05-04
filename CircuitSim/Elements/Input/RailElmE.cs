namespace Circuit.Elements.Input {
    class RailElmE : VoltageElmE {
        public RailElmE() : base(WAVEFORM.DC) { }

        public RailElmE(WAVEFORM wf) : base(wf) { }

        public RailElmE(StringTokenizer st): base(st) { }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirPostCount { get { return 1; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override void CirStamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
            }
        }
    }
}
