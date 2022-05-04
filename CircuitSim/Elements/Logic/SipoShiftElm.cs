using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class SipoShiftElm : ChipElm {
        short data = 0;
        bool clockstate = false;

        public SipoShiftElm(Point pos) : base(pos) { }

        public SipoShiftElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.SHIFT_REGISTER_SIPO; } }

        public override int CirPostCount { get { return 10; } }

        public override int CirVoltageSourceCount { get { return 8; } }

        string getChipName() { return "SIPO shift register"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            sizeX = 9;
            sizeY = 3;
            pins = new Pin[CirPostCount];

            pins[0] = new Pin(this, 1, SIDE_W, "D");
            pins[1] = new Pin(this, 2, SIDE_W, "");
            pins[1].clock = true;

            pins[2] = new Pin(this, 1, SIDE_N, "I7");
            pins[2].output = true;
            pins[3] = new Pin(this, 2, SIDE_N, "I6");
            pins[3].output = true;
            pins[4] = new Pin(this, 3, SIDE_N, "I5");
            pins[4].output = true;
            pins[5] = new Pin(this, 4, SIDE_N, "I4");
            pins[5].output = true;
            pins[6] = new Pin(this, 5, SIDE_N, "I3");
            pins[6].output = true;
            pins[7] = new Pin(this, 6, SIDE_N, "I2");
            pins[7].output = true;
            pins[8] = new Pin(this, 7, SIDE_N, "I1");
            pins[8].output = true;
            pins[9] = new Pin(this, 8, SIDE_N, "I0");
            pins[9].output = true;
        }

        protected override void execute() {
            if (pins[1].value && !clockstate) {
                clockstate = true;
                data = (short)(data >> 1);
                if (pins[0].value) {
                    data += 128;
                }
                pins[2].value = (data & 128) > 0;
                pins[3].value = (data & 64) > 0;
                pins[4].value = (data & 32) > 0;
                pins[5].value = (data & 16) > 0;
                pins[6].value = (data & 8) > 0;
                pins[7].value = (data & 4) > 0;
                pins[8].value = (data & 2) > 0;
                pins[9].value = (data & 1) > 0;
            }
            if (!pins[1].value) {
                clockstate = false;
            }
        }
    }
}
