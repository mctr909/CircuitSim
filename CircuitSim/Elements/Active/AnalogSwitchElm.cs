﻿namespace Circuit.Elements.Active {
    class AnalogSwitchElm : BaseElement {
        public double Ron;
        public double Roff;
        public bool Invert;

        public bool IsOpen { get; private set; }

        double mResistance;

        public AnalogSwitchElm() : base() {
            Ron = 20;
            Roff = 1e10;
        }

        public AnalogSwitchElm(StringTokenizer st) : base() {
            Ron = 20;
            Roff = 1e10;
            try {
                Ron = double.Parse(st.nextToken());
                Roff = double.Parse(st.nextToken());
            } catch { }
        }

        // we need this to be able to change the matrix for each step
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        // we have to just assume current will flow either way, even though that
        // might cause singular matrix errors
        public override bool GetConnection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == 2) {
                return 0;
            }
            return mCurrent;
        }

        public override void AnaStamp() {
            mCir.StampNonLinear(Nodes[0]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void CirDoStep() {
            IsOpen = Volts[2] < 2.5;
            if (Invert) {
                IsOpen = !IsOpen;
            }
            mResistance = IsOpen ? Roff : Ron;
            mCir.StampResistor(Nodes[0], Nodes[1], mResistance);
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}