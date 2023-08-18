using System.Drawing;

namespace Circuit.Elements.Gate {
    class ElmGate : BaseElement {
        public static double LastHighVoltage = 5;

        public int InputCount = 2;
        public bool[] InputStates;
        public double HighVoltage;
        public bool HasSchmittInputs;
        public bool IsInverting;
        public Point[] InPosts;

        int mOscillationCount;

        public bool LastOutput { get; private set; }

        public override Point GetPost(int n) {
            if (n == InputCount) {
                return Post[1];
            }
            return InPosts[n];
        }

        public ElmGate() : base() {
            InputCount = 2;
            /* copy defaults from last gate edited */
            HighVoltage = LastHighVoltage;
        }

        public ElmGate(StringTokenizer st) : base() {
            st.nextTokenInt(out InputCount, InputCount);
            double lastOutputVoltage = st.nextTokenDouble();
            try {
                HighVoltage = st.nextTokenDouble();
            } catch {
                HighVoltage = 5;
            }
            LastOutput = HighVoltage * 0.5 < lastOutputVoltage;
        }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return InputCount + 1; } }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == InputCount) {
                return Current;
            }
            return 0;
        }

        /* there is no current path through the gate inputs,
        * but there is an indirect path through the output to ground. */
        public override bool AnaGetConnection(int n1, int n2) { return false; }

        public override bool AnaHasGroundConnection(int n1) {
            return (n1 == InputCount);
        }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[InputCount], mVoltSource);
        }

        public override void CirDoIteration() {
            bool f = calcFunction();
            if (IsInverting) {
                f = !f;
            }

            /* detect oscillation (using same strategy as Atanua) */
            if (LastOutput == !f) {
                if (mOscillationCount++ > 50) {
                    /* output is oscillating too much, randomly leave output the same */
                    mOscillationCount = 0;
                    if (CirSimForm.Random.Next(10) > 5) {
                        f = LastOutput;
                    }
                }
            } else {
                mOscillationCount = 0;
            }
            LastOutput = f;
            double res = f ? HighVoltage : 0;
            Circuit.UpdateVoltageSource(mVoltSource, res);
        }

        protected bool getInput(int x) {
            if (!HasSchmittInputs) {
                return Volts[x] > HighVoltage * 0.5;
            }
            bool res = Volts[x] > HighVoltage * (InputStates[x] ? 0.35 : 0.55);
            InputStates[x] = res;
            return res;
        }

        protected virtual bool calcFunction() { return false; }
    }
}
