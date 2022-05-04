using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class TFlipFlopElm : ChipElm {
        const int FLAG_RESET = 2;
        const int FLAG_SET = 4;

        public TFlipFlopElm(Point pos) : base(pos) { }

        public TFlipFlopElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            pins[2].value = !pins[1].value;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.FLIP_FLOP_T; } }

        public override int CirPostCount { get { return 4 + (hasReset ? 1 : 0) + (hasSet ? 1 : 0); } }

        public override int CirVoltageSourceCount { get { return 2; } }

        string getChipName() { return "T flip-flop"; }

        bool hasReset { get { return (mFlags & FLAG_RESET) != 0 || hasSet; } }

        bool hasSet { get { return (mFlags & FLAG_SET) != 0; } }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = 3;
            pins = new Pin[CirPostCount];
            pins[0] = new Pin(this, 0, SIDE_W, "T");
            pins[1] = new Pin(this, 0, SIDE_E, "Q");
            pins[1].output = pins[1].state = true;
            pins[2] = new Pin(this, hasSet ? 1 : 2, SIDE_E, "Q");
            pins[2].output = true;
            pins[2].lineOver = true;
            pins[3] = new Pin(this, 1, SIDE_W, "");
            pins[3].clock = true;
            if (!hasSet) {
                if (hasReset) {
                    pins[4] = new Pin(this, 2, SIDE_W, "R");
                }
            } else {
                pins[4] = new Pin(this, 2, SIDE_E, "R");
                pins[5] = new Pin(this, 2, SIDE_W, "S");
            }
        }

        public override void CirReset() {
            base.CirReset();
            CirVolts[2] = 5;
            pins[2].value = true;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Reset Pin",
                    Checked = hasReset
                };
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Set Pin",
                    Checked = hasSet
                };
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_RESET;
                } else {
                    mFlags &= ~FLAG_RESET | FLAG_SET;
                }
                SetupPins();
                cirAllocNodes();
                SetPoints();
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SET;
                } else {
                    mFlags &= ~FLAG_SET;
                }
                SetupPins();
                cirAllocNodes();
                SetPoints();
            }
            base.SetElementValue(n, ei);
        }

        protected override void execute() {
            if (pins[3].value && !lastClock) {
                if (pins[0].value) { //if T = 1
                    pins[1].value = !pins[1].value;
                }
                //else no change
            }
            if (hasSet && pins[5].value) {
                pins[1].value = true;
            }
            if (hasReset && pins[4].value) {
                pins[1].value = false;
            }
            pins[2].value = !pins[1].value;
            lastClock = pins[3].value;
        }
    }
}
