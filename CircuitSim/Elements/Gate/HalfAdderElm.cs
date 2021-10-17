using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Gate {
    class HalfAdderElm : ChipElm {
        public HalfAdderElm(Point pos) : base(pos) { }

        public HalfAdderElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.HALF_ADDER; } }

        public override int PostCount { get { return 4; } }

        public override int VoltageSourceCount { get { return 2; } }

        string getChipName() { return "Half Adder"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = 2;
            pins = new Pin[PostCount];
            pins[0] = new Pin(this, 0, SIDE_E, "S");
            pins[0].output = true;
            pins[1] = new Pin(this, 1, SIDE_E, "C");
            pins[1].output = true;
            pins[2] = new Pin(this, 0, SIDE_W, "A");
            pins[3] = new Pin(this, 1, SIDE_W, "B");
        }

        protected override void execute() {
            pins[0].value = pins[2].value ^ pins[3].value;
            pins[1].value = pins[2].value && pins[3].value;
        }
    }
}
