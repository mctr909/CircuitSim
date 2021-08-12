using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.Elements.Logic {
    class MultiplexerElm : ChipElm {
        int selectBitCount;
        int outputCount;

        public MultiplexerElm(Point pos) : base(pos) {
            selectBitCount = 2;
            SetupPins();
        }

        public MultiplexerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            selectBitCount = 2;
            try {
                selectBitCount = int.Parse(st.nextToken());
            } catch { }
            SetupPins();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.MULTIPLEXER; } }

        public override int PostCount { get { return outputCount + selectBitCount + 1; } }

        public override int VoltageSourceCount { get { return 1; } }

        protected override string dump() { return selectBitCount.ToString(); }

        string getChipName() { return "Multiplexer"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            sizeX = selectBitCount + 1;
            outputCount = 1;
            for (var i = 0; i != selectBitCount; i++) {
                outputCount <<= 1;
            }
            sizeY = outputCount + 1;

            pins = new Pin[PostCount];

            for (var i = 0; i != outputCount; i++) {
                pins[i] = new Pin(this, i, SIDE_W, "I" + i);
            }
            int n = outputCount;
            for (var i = 0; i != selectBitCount; i++, n++) {
                pins[n] = new Pin(this, i + 1, SIDE_S, "S" + i);
            }
            pins[n] = new Pin(this, 0, SIDE_E, "Q");
            pins[n].output = true;

            allocNodes();
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                return new ElementInfo("# of Select Bits", selectBitCount, 1, 8).SetDimensionless();
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2 && ei.Value >= 1 && ei.Value <= 6) {
                selectBitCount = (int)ei.Value;
                SetupPins();
                SetPoints();
                return;
            }
            base.SetElementValue(n, ei);
        }

        protected override void execute() {
            int selectedValue = 0;
            for (var i = 0; i != selectBitCount; i++) {
                if (pins[outputCount + i].value) {
                    selectedValue |= 1 << i;
                }
            }
            pins[outputCount + selectBitCount].value = pins[selectedValue].value;
        }
    }
}
