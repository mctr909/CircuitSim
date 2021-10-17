using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Logic {
    class JKFlipFlopElm : ChipElm {
        const int FLAG_RESET = 2;
        const int FLAG_POSITIVE_EDGE = 4;

        public JKFlipFlopElm(Point pos) : base(pos) { }

        public JKFlipFlopElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            pins[4].value = !pins[3].value;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.FLIP_FLOP_JK; } }

        public override int PostCount { get { return 5 + (hasReset ? 1 : 0); } }

        public override int VoltageSourceCount { get { return 2; } }

        string getChipName() { return "JK flip-flop"; }

        bool hasReset { get { return (mFlags & FLAG_RESET) != 0; } }

        bool positiveEdgeTriggered { get { return (mFlags & FLAG_POSITIVE_EDGE) != 0; } }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = 3;
            pins = new Pin[PostCount];
            pins[0] = new Pin(this, 0, SIDE_W, "J");
            pins[1] = new Pin(this, 1, SIDE_W, "");
            pins[1].clock = true;
            pins[1].bubble = !positiveEdgeTriggered;
            pins[2] = new Pin(this, 2, SIDE_W, "K");
            pins[3] = new Pin(this, 0, SIDE_E, "Q");
            pins[3].output = pins[3].state = true;
            pins[4] = new Pin(this, 2, SIDE_E, "Q");
            pins[4].output = true;
            pins[4].lineOver = true;
            if (hasReset) {
                pins[5] = new Pin(this, 1, SIDE_E, "R");
            }
        }

        public override void Reset() {
            base.Reset();
            Volts[2] = 5;
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
                    Text = "Positive Edge Triggered",
                    Checked = positiveEdgeTriggered
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
                    mFlags &= ~FLAG_RESET;
                }
                SetupPins();
                allocNodes();
                SetPoints();
            }
            if (n == 3) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_POSITIVE_EDGE);
                pins[1].bubble = !positiveEdgeTriggered;
            }
            base.SetElementValue(n, ei);
        }

        protected override void execute() {
            bool transition;
            if (positiveEdgeTriggered) {
                transition = pins[1].value && !lastClock;
            } else {
                transition = !pins[1].value && lastClock;
            }
            if (transition) {
                var q = pins[3].value;
                if (pins[0].value) {
                    if (pins[2].value) {
                        q = !q;
                    } else {
                        q = true;
                    }
                } else if (pins[2].value) {
                    q = false;
                }
                pins[3].value = q;
                pins[4].value = !q;
            }
            lastClock = pins[1].value;
            if (hasReset) {
                if (pins[5].value) {
                    pins[3].value = false;
                    pins[4].value = true;
                }
            }
        }
    }
}
