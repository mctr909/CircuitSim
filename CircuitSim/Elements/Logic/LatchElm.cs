using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class LatchElm : ChipElm {
        const int FLAG_STATE = 2;

        int loadPin;
        bool lastLoad = false;

        public LatchElm(Point pos) : base(pos) {
            mFlags = FLAG_STATE;
            SetupPins();
        }

        public LatchElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            // add FLAG_STATE flag to old latches so their state gets saved
            if ((mFlags & FLAG_STATE) == 0) {
                mFlags |= FLAG_STATE;
                SetupPins();
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.LATCH; } }

        public override int PostCount { get { return bits * 2 + 1; } }

        public override int VoltageSourceCount { get { return bits; } }

        string getChipName() { return "Latch"; }

        bool hasReset { get { return false; } }

        protected override bool needsBits() { return true; }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = bits + 1;
            pins = new Pin[PostCount];
            for (var i = 0; i != bits; i++) {
                pins[i] = new Pin(this, bits - 1 - i, SIDE_W, "I" + i);
            }
            for (var i = 0; i != bits; i++) {
                pins[i + bits] = new Pin(this, bits - 1 - i, SIDE_E, "O");
                pins[i + bits].output = true;
                pins[i + bits].state = (mFlags & FLAG_STATE) != 0;
            }
            pins[loadPin = bits * 2] = new Pin(this, bits, SIDE_W, "Ld");
            allocNodes();
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n < 2) {
                return base.GetElementInfo(n);
            }
            if (n == 2) {
                return new ElementInfo("# of Bits", bits, 1, 1).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n < 2) {
                base.SetElementValue(n, ei);
                return;
            }
            if (n == 2 && ei.Value >= 2) {
                bits = (int)ei.Value;
                SetupPins();
                SetPoints();
            }
        }

        protected override void execute() {
            if (pins[loadPin].value && !lastLoad) {
                for (var i = 0; i != bits; i++) {
                    pins[i + bits].value = pins[i].value;
                }
            }
            lastLoad = pins[loadPin].value;
        }
    }
}
