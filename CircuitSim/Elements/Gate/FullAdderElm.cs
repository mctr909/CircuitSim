using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Gate {
    class FullAdderElm : ChipElm {
        public FullAdderElm(Point pos) : base(pos) { }

        public FullAdderElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.FULL_ADDER; } }

        public override int PostCount { get { return 5; } }

        public override int VoltageSourceCount { get { return 2; } }

        string getChipName() { return "Full Adder"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = 3;
            pins = new Pin[PostCount];
            pins[0] = new Pin(this, 2, SIDE_E, "S");
            pins[0].output = true;
            pins[1] = new Pin(this, 0, SIDE_E, "C");
            pins[1].output = true;
            pins[2] = new Pin(this, 0, SIDE_W, "A");
            pins[3] = new Pin(this, 1, SIDE_W, "B");
            pins[4] = new Pin(this, 2, SIDE_W, "Cin");
        }

        protected override void execute() {
            pins[0].value = (pins[2].value ^ pins[3].value) ^ pins[4].value;
            pins[1].value =
                (pins[2].value && pins[3].value) ||
                (pins[2].value && pins[4].value) ||
                (pins[3].value && pins[4].value);
        }
    }
}
