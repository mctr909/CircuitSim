﻿namespace Circuit.Elements.Output {
    class LogicOutputElmE : BaseElement {
        public double mThreshold;
        public string mValue;

        public bool needsPullDown;

        public LogicOutputElmE() : base() {
            mThreshold = 2.5;
        }

        public LogicOutputElmE(StringTokenizer st) : base() {
            try {
                mThreshold = st.nextTokenDouble();
            } catch {
                mThreshold = 2.5;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        public override void Stamp() {
            if (needsPullDown) {
                mCir.StampResistor(Nodes[0], 0, 1e6);
            }
        }
    }
}
