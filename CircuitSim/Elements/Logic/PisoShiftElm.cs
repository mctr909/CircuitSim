using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class PisoShiftElm : ChipElm {
        short data = 0;
        bool clockstate = false;
        bool modestate = false;

        public PisoShiftElm(Point pos) : base(pos) { }

        public PisoShiftElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.SHIFT_REGISTER_PISO; } }

        public override int PostCount { get { return 11; } }

        public override int VoltageSourceCount { get { return 1; } }

        string getChipName() { return "PISO shift register"; }

        bool hasReset { get { return false; } }

        public override void SetupPins() {
            sizeX = 10;
            sizeY = 3;
            pins = new Pin[PostCount];

            pins[0] = new Pin(this, 1, SIDE_W, "L");
            pins[1] = new Pin(this, 2, SIDE_W, "");
            pins[1].clock = true;

            pins[2] = new Pin(this, 1, SIDE_N, "I7");
            pins[3] = new Pin(this, 2, SIDE_N, "I6");
            pins[4] = new Pin(this, 3, SIDE_N, "I5");
            pins[5] = new Pin(this, 4, SIDE_N, "I4");
            pins[6] = new Pin(this, 5, SIDE_N, "I3");
            pins[7] = new Pin(this, 6, SIDE_N, "I2");
            pins[8] = new Pin(this, 7, SIDE_N, "I1");
            pins[9] = new Pin(this, 8, SIDE_N, "I0");

            pins[10] = new Pin(this, 1, SIDE_E, "Q");
            pins[10].output = true;
        }

        protected override void execute() {
            if (pins[0].value && !modestate) {
                modestate = true;
                data = 0;
                if (pins[2].value) data += 128;
                if (pins[3].value) data += 64;
                if (pins[4].value) data += 32;
                if (pins[5].value) data += 16;
                if (pins[6].value) data += 8;
                if (pins[7].value) data += 4;
                if (pins[8].value) data += 2;
                if (pins[9].value) data += 1;
            } else if (pins[1].value && !clockstate) {
                clockstate = true;
                if ((data & 1) == 0) {
                    pins[10].value = false;
                } else {
                    pins[10].value = true;
                }
                data = (byte)(data >> 1);
            }
            if (!pins[0].value) modestate = false;
            if (!pins[1].value) clockstate = false;
        }
    }
}
