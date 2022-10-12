using System.Drawing;

using Circuit.Elements;
using Circuit.UI;

namespace Circuit {
    public class ScopePlot {
        public static readonly Color RED = Color.FromArgb(0xBF, 0x00, 0x00);
        public static readonly Color YELLOW = Color.FromArgb(0xCF, 0xCF, 0x00);
        public static readonly Color GREEN = Color.FromArgb(0x00, 0xBF, 0x00);
        public static readonly Color BLUE = Color.FromArgb(0x2F, 0x2F, 0xCF);
        public static readonly Color PURPLE = Color.FromArgb(0xBF, 0x00, 0xBF);
        public static readonly Color GRAY = Color.FromArgb(0xA0, 0xA0, 0xA0);

        static readonly Color[] COLORS = {
            GREEN,
            YELLOW,
            RED,
            BLUE,
            PURPLE
        };
        public enum E_COLOR : int {
            GREEN,
            YELLOW,
            RED,
            BLUE,
            PURPLE,
            INVALID
        }

        public BaseUI Elm;

        public double[] MinValues { get; private set; }
        public double[] MaxValues { get; private set; }
        public int Pointer { get; private set; }
        public int Speed { get; private set; }
        public double LastValue { get; private set; }
        public Color Color { get; private set; }
        public E_COLOR ColorIndex { get; private set; } = E_COLOR.INVALID;

        int mScopePointCount;
        int mCounter;

        public ScopePlot(BaseUI e) {
            Elm = e;
        }

        public int StartIndex(int w) {
            return Pointer + mScopePointCount - w;
        }

        public void Reset(int spc, int sp, bool full) {
            int oldSpc = mScopePointCount;
            mScopePointCount = spc;
            if (Speed != sp) {
                oldSpc = 0; /* throw away old data */
            }
            Speed = sp;
            var oldMin = MinValues;
            var oldMax = MaxValues;
            MinValues = new double[mScopePointCount];
            MaxValues = new double[mScopePointCount];
            if (oldMin != null && !full) {
                /* preserve old data if possible */
                int i;
                for (i = 0; i != mScopePointCount && i != oldSpc; i++) {
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
            if (Elm == null) {
                return;
            }
            double v = Elm.Elm.VoltageDiff;
            if (v < MinValues[Pointer]) {
                MinValues[Pointer] = v;
            }
            if (v > MaxValues[Pointer]) {
                MaxValues[Pointer] = v;
            }
            LastValue = v;
            mCounter++;
            if (mCounter >= Speed) {
                Pointer = (Pointer + 1) & (mScopePointCount - 1);
                MinValues[Pointer] = MaxValues[Pointer] = v;
                mCounter = 0;
            }
        }

        public string GetUnitText(double v) {
            return Utils.VoltageText(v);
        }

        public void SetColor(int index) {
            if (0 <= index) {
                ColorIndex = (E_COLOR)(index % COLORS.Length);
            } else {
                ColorIndex = E_COLOR.GREEN;
            }
            Color = COLORS[(int)ColorIndex];
        }
    }
}
