﻿namespace Circuit.Elements.Active {
    class AnalogSwitchElmE : BaseElement {
        public double Ron;
        public double Roff;
        public bool Invert;

        public bool IsOpen { get; private set; }

        double mResistance;

        public AnalogSwitchElmE() : base() {
            Ron = 20;
            Roff = 1e10;
        }

        public AnalogSwitchElmE(StringTokenizer st) : base() {
            Ron = 20;
            Roff = 1e10;
            try {
                Ron = double.Parse(st.nextToken());
                Roff = double.Parse(st.nextToken());
            } catch { }
        }

        protected override void calcCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }

        // we need this to be able to change the matrix for each step
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == 2) {
                return 0;
            }
            return mCurrent;
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[0]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void DoStep() {
            IsOpen = Volts[2] < 2.5;
            if (Invert) {
                IsOpen = !IsOpen;
            }
            mResistance = IsOpen ? Roff : Ron;
            mCir.StampResistor(Nodes[0], Nodes[1], mResistance);
        }
    }
}
