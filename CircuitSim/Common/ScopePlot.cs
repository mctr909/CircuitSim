using System.Drawing;

using Circuit.Elements;

namespace Circuit {
    class ScopePlot {
        static readonly Color[] colors = {
            Color.FromArgb(0xFF, 0x00, 0x00),
            Color.FromArgb(0xFF, 0x80, 0x00),
            Color.FromArgb(0xFF, 0x00, 0xFF),
            Color.FromArgb(0x7F, 0x00, 0xFF),
            Color.FromArgb(0x00, 0x00, 0xFF),
            Color.FromArgb(0x00, 0x80, 0xFF),
            Color.FromArgb(0xFF, 0xFF, 0x00),
            Color.FromArgb(0x00, 0xFF, 0xFF)
        };

        public CircuitElm elm;

        public double[] minValues { get; private set; }
        public double[] maxValues { get; private set; }
        public int ptr { get; private set; }
        public int value { get; private set; }
        public int speed { get; private set; }
        public int units;
        public double lastValue { get; private set; }
        public Color color { get; private set; }

        int scopePointCount;
        int ctr;

        public ScopePlot(CircuitElm e, int u) {
            elm = e;
            units = u;
        }

        public ScopePlot(CircuitElm e, int u, int v) {
            elm = e;
            units = u;
            value = v;
        }

        public int startIndex(int w) {
            return ptr + scopePointCount - w;
        }

        public void reset(int spc, int sp, bool full) {
            int oldSpc = scopePointCount;
            scopePointCount = spc;
            if (speed != sp) {
                oldSpc = 0; /* throw away old data */
            }
            speed = sp;
            var oldMin = minValues;
            var oldMax = maxValues;
            minValues = new double[scopePointCount];
            maxValues = new double[scopePointCount];
            if (oldMin != null && !full) {
                /* preserve old data if possible */
                int i;
                for (i = 0; i != scopePointCount && i != oldSpc; i++) {
                    int i1 = (-i) & (scopePointCount - 1);
                    int i2 = (ptr - i) & (oldSpc - 1);
                    minValues[i1] = oldMin[i2];
                    maxValues[i1] = oldMax[i2];
                }
            } else {
                ctr = 0;
            }
            ptr = 0;
        }

        public void timeStep() {
            if (elm == null) {
                return;
            }
            double v = elm.getScopeValue(value);
            if (v < minValues[ptr]) {
                minValues[ptr] = v;
            }
            if (v > maxValues[ptr]) {
                maxValues[ptr] = v;
            }
            lastValue = v;
            ctr++;
            if (ctr >= speed) {
                ptr = (ptr + 1) & (scopePointCount - 1);
                minValues[ptr] = maxValues[ptr] = v;
                ctr = 0;
            }
        }

        public string getUnitText(double v) {
            switch (units) {
            case Scope.UNITS_V:
                return CircuitElm.getVoltageText(v);
            case Scope.UNITS_A:
                return CircuitElm.getCurrentText(v);
            case Scope.UNITS_OHMS:
                return CircuitElm.getUnitText(v, CirSim.ohmString);
            case Scope.UNITS_W:
                return CircuitElm.getUnitText(v, "W");
            }
            return null;
        }

        public void assignColor(int count) {
            if (count > 0) {
                color = colors[(count - 1) % 8];
                return;
            }
            switch (units) {
            case Scope.UNITS_V:
                color = Color.FromArgb(0x00, 0xFF, 0x00);
                break;
            case Scope.UNITS_A:
                color = Color.FromArgb(0xFF, 0xFF, 0x00);
                break;
            default:
                color = CirSim.theSim.chkPrintableCheckItem.Checked ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255);
                break;
            }
        }
    }
}
