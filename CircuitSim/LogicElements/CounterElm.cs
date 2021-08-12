using System.Drawing;
using System.Windows.Forms;

using Circuit.ActiveElements;

namespace Circuit.LogicElements {
    class CounterElm : ChipElm {
        const int FLAG_UP_DOWN = 4;
        bool invertreset;
        int modulus;

        public CounterElm(Point pos) : base(pos) { }

        public CounterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            invertreset = true;
            try {
                invertreset = bool.Parse(st.nextToken());
                modulus = int.Parse(st.nextToken());
            } catch { }
            pins[1].bubble = invertreset;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.COUNTER; } }

        public override int PostCount { get { return hasUpDown ? bits + 3 : bits + 2; } }

        public override int VoltageSourceCount { get { return bits; } }

        protected override bool needsBits() { return true; }

        protected override string dump() {
            return base.dump() + " " + invertreset + " " + modulus;
        }

        string getChipName() { return "Counter"; }

        bool hasReset { get { return false; } }

        bool hasUpDown { get { return (mFlags & FLAG_UP_DOWN) != 0; } }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = bits > 2 ? bits : 2;
            pins = new Pin[PostCount];
            pins[0] = new Pin(this, 0, SIDE_W, "");
            pins[0].clock = true;
            pins[1] = new Pin(this, sizeY - 1, SIDE_W, "R");
            pins[1].bubble = invertreset;
            for (var i = 0; i != bits; i++) {
                var ii = i + 2;
                pins[ii] = new Pin(this, i, SIDE_E, "Q" + (bits - i - 1));
                pins[ii].output = pins[ii].state = true;
            }
            if (hasUpDown) {
                pins[bits + 2] = new Pin(this, sizeY - 2, SIDE_W, "U/D");
            }
            allocNodes();
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Flip X",
                    Checked = (mFlags & FLAG_FLIP_X) != 0
                };
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Flip Y",
                    Checked = (mFlags & FLAG_FLIP_Y) != 0
                };
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Invert reset pin",
                    Checked = invertreset
                };
                return ei;
            }
            if (n == 3) {
                return new ElementInfo("# of Bits", bits, 1, 1).SetDimensionless();
            }
            if (n == 4) {
                return new ElementInfo("Modulus", modulus, 1, 1).SetDimensionless();
            }
            if (n == 5) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Up/Down Pin",
                    Checked = hasUpDown
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP_X;
                } else {
                    mFlags &= ~FLAG_FLIP_X;
                }
                SetPoints();
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP_Y;
                } else {
                    mFlags &= ~FLAG_FLIP_Y;
                }
                SetPoints();
            }
            if (n == 2) {
                invertreset = ei.CheckBox.Checked;
                SetupPins();
                SetPoints();
            }
            if (n == 3 && ei.Value >= 3) {
                bits = (int)ei.Value;
                SetupPins();
                SetPoints();
            }
            if (n == 4) {
                modulus = (int)ei.Value;
            }
            if (n == 5) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_UP_DOWN);
                SetupPins();
                SetPoints();
            }
        }

        protected override void execute() {
            if (pins[0].value && !lastClock) {
                int value = 0;
                // get direction
                int dir = 1;
                if (hasUpDown && pins[bits + 2].value) {
                    dir = -1;
                }
                // get current value
                int lastBit = 2 + bits - 1;
                for (var i = 0; i != bits; i++) {
                    if (pins[lastBit - i].value) {
                        value |= 1 << i;
                    }
                }
                // update value
                value += dir;
                if (modulus != 0) {
                    value = (value + modulus) % modulus;
                }
                // convert value to binary
                for (var i = 0; i != bits; i++) {
                    pins[lastBit - i].value = (value & (1 << i)) != 0;
                }
            }
            if (!pins[1].value == invertreset) {
                for (var i = 0; i != bits; i++) {
                    pins[i + 2].value = false;
                }
            }
            lastClock = pins[0].value;
        }
    }
}
