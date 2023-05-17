using System.Linq;

namespace Circuit.Elements.Input {
    class ElmRail : ElmVoltage {
        public ElmRail() : base(WAVEFORM.DC) { }

        public ElmRail(WAVEFORM wf) : base(wf) { }

        public ElmRail(StringTokenizer st): base(st) { }

        public override int PostCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            int n0 = Nodes[0] - 1;
            int vn = Circuit.Nodes.Count + mVoltSource - 1;
            if (n0 < 0 || vn < 0) {
                return;
            }
            Circuit.Matrix[vn, n0] += 1;
            Circuit.Matrix[n0, vn] -= 1;
            if (WaveForm == WAVEFORM.DC) {
                Circuit.RightSide[vn] += GetVoltage();
            } else {
                Circuit.RowInfo[vn].RightChanges = true;
            }
        }
    }
}
