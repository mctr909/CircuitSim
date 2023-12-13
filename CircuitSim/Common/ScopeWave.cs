namespace Circuit.Common {
    public class ScopeWave {
        public IUI UI;
        public double[] MinValues;
        public double[] MaxValues;
        public int Speed;
        public int Pointer;

        BaseElement mElm;
        int mCounter;
        int mScopePointCount;

        public ScopePlot.E_COLOR Color { get; private set; } = ScopePlot.E_COLOR.INVALID;

        public ScopeWave(IUI ui) {
            UI = ui;
            mElm = ui.Elm;
        }

        public void SetColor(int index) {
            Color = (ScopePlot.E_COLOR)(index % (int)ScopePlot.E_COLOR.INVALID);
        }

        public int StartIndex(int w) {
            return Pointer + mScopePointCount - w;
        }

        public void Reset(int scopePoints, int speed, bool full) {
            var oldSpc = mScopePointCount;
            mScopePointCount = scopePoints;
            if (Speed != speed) {
                oldSpc = 0;
            }
            Speed = speed;
            var oldMin = MinValues;
            var oldMax = MaxValues;
            MinValues = new double[mScopePointCount];
            MaxValues = new double[mScopePointCount];
            if (oldMin != null && !full) {
                for (int i = 0; i != mScopePointCount && i != oldSpc; i++) {
                    int i1 = (-i) & (mScopePointCount - 1);
                    int i2 = (Pointer - i) & (oldSpc - 1);
                    MinValues[i1] = oldMin[i2];
                    MaxValues[i1] = oldMax[i2];
                }
            } else {
                mCounter = 0;
            }
            Pointer = 0;
        }

        public void TimeStep() {
            var v = mElm.VoltageDiff;
            if (v < MinValues[Pointer]) {
                MinValues[Pointer] = v;
            }
            if (v > MaxValues[Pointer]) {
                MaxValues[Pointer] = v;
            }
            mCounter++;
            if (mCounter >= Speed) {
                Pointer = (Pointer + 1) & (mScopePointCount - 1);
                MinValues[Pointer] = MaxValues[Pointer] = v;
                mCounter = 0;
            }
        }
    }
}
