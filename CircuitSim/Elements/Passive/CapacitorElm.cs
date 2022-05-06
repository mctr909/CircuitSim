namespace Circuit.Elements.Passive {
    class CapacitorElm : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Capacitance { get; set; }

        public double VoltDiff { get; private set; }

        public CapacitorElm() {
            Capacitance = 1e-5;
        }

        public CapacitorElm(StringTokenizer st) {
            Capacitance = st.nextTokenDouble();
            VoltDiff = st.nextTokenDouble();
        }

        public override int PostCount { get { return 2; } }

        public override void Reset() {
            base.Reset();
            mCurrent = CurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            VoltDiff = 1e-3;
        }

        public override void AnaShorted() {
            base.Reset();
            VoltDiff = mCurrent = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            if (CirSim.Sim.DcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                mCir.StampResistor(Nodes[0], Nodes[1], 1e8);
                mCurSourceValue = 0;
                return;
            }

            mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);

            mCir.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            mCir.StampRightSide(Nodes[0]);
            mCir.StampRightSide(Nodes[1]);
        }

        public override void CirDoStep() {
            if (CirSim.Sim.DcAnalysisFlag) {
                return;
            }
            mCir.StampCurrentSource(Nodes[0], Nodes[1], mCurSourceValue);
        }

        public override void CirStartIteration() {
            mCurSourceValue = -VoltDiff / mCompResistance - mCurrent;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            VoltDiff = Volts[0] - Volts[1];

            if (CirSim.Sim.DcAnalysisFlag) {
                mCurrent = VoltDiff / 1e8;
                return;
            }

            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (0 < mCompResistance) {
                mCurrent = VoltDiff / mCompResistance + mCurSourceValue;
            }
        }
    }
}
