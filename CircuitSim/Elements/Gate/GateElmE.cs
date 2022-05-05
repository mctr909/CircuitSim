namespace Circuit.Elements.Gate {
    class GateElmE : BaseElement {
        public static double LastHighVoltage = 5;

        public int InputCount = 2;
        public bool[] InputStates;
        public double HighVoltage;
        public bool HasSchmittInputs;
        public bool IsInverting;

        int mOscillationCount;

        public bool LastOutput { get; private set; }

        public GateElmE() : base() {
            InputCount = 2;
            /* copy defaults from last gate edited */
            HighVoltage = LastHighVoltage;
        }

        public GateElmE(StringTokenizer st) : base() {
            InputCount = st.nextTokenInt();
            double lastOutputVoltage = st.nextTokenDouble();
            try {
                HighVoltage = st.nextTokenDouble();
            } catch {
                HighVoltage = 5;
            }
            LastOutput = HighVoltage * 0.5 < lastOutputVoltage;
        }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return InputCount + 1; } }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == InputCount) {
                return mCirCurrent;
            }
            return 0;
        }

        public override bool CirHasGroundConnection(int n1) {
            return (n1 == InputCount);
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[InputCount], mCirVoltSource);
        }

        public override void CirDoStep() {
            bool f = calcFunction();
            if (IsInverting) {
                f = !f;
            }

            /* detect oscillation (using same strategy as Atanua) */
            if (LastOutput == !f) {
                if (mOscillationCount++ > 50) {
                    /* output is oscillating too much, randomly leave output the same */
                    mOscillationCount = 0;
                    if (CirSim.Random.Next(10) > 5) {
                        f = LastOutput;
                    }
                }
            } else {
                mOscillationCount = 0;
            }
            LastOutput = f;
            double res = f ? HighVoltage : 0;
            mCir.UpdateVoltageSource(0, CirNodes[InputCount], mCirVoltSource, res);
        }

        protected bool getInput(int x) {
            if (!HasSchmittInputs) {
                return CirVolts[x] > HighVoltage * 0.5;
            }
            bool res = CirVolts[x] > HighVoltage * (InputStates[x] ? 0.35 : 0.55);
            InputStates[x] = res;
            return res;
        }

        protected virtual bool calcFunction() { return false; }
    }
}
