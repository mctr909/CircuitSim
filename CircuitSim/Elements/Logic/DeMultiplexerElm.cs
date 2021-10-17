using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class DeMultiplexerElm : ChipElm {
        int selectBitCount = 2;
        int outputCount;
        int qPin = 6;

        public DeMultiplexerElm(Point pos) : base(pos) { }

        public DeMultiplexerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            try {
                selectBitCount = int.Parse(st.nextToken());
                SetupPins();
                SetPoints();
            } catch { }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.DEMULTIPLEXER; } }

        public override int PostCount { get { return qPin + 1; } }

        public override int VoltageSourceCount { get { return outputCount; } }

        protected override string dump() { return selectBitCount.ToString(); }

        string getChipName() { return "demultiplexer"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            outputCount = 1 << selectBitCount;
            qPin = outputCount + selectBitCount;
            sizeX = 1 + selectBitCount;
            sizeY = 1 + outputCount;
            allocNodes();
            pins = new Pin[PostCount];
            for (var i = 0; i != outputCount; i++) {
                pins[i] = new Pin(this, i, SIDE_E, "Q" + i);
                pins[i].output = true;
            }
            for (var i = 0; i != selectBitCount; i++) {
                var ii = i + outputCount;
                pins[ii] = new Pin(this, i, SIDE_S, "S" + i);
            }
            pins[qPin] = new Pin(this, 0, SIDE_W, "Q");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n < 2) {
                return base.GetElementInfo(n);
            }
            if (n == 2) {
                return new ElementInfo("# of Select Bits", selectBitCount).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n < 2) {
                base.SetElementValue(n, ei);
                return;
            }
            if (n == 2 && ei.Value >= 1 && ei.Value <= 6) {
                selectBitCount = (int)ei.Value;
                SetupPins();
                SetPoints();
            }
        }

        protected override void execute() {
            int val = 0;
            for (var i = 0; i != selectBitCount; i++) {
                if (pins[i + outputCount].value) {
                    val |= 1 << i;
                }
            }
            for (var i = 0; i != outputCount; i++) {
                pins[i].value = false;
            }
            pins[val].value = pins[qPin].value;
        }
    }
}
