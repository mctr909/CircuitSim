using System.Drawing;

namespace Circuit.Elements {
    class RingCounterElm : ChipElm {
        bool mJustLoaded;

        protected override int bits { get; set; } = 10;

        public RingCounterElm(Point pos) : base(pos) { }

        public RingCounterElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            mJustLoaded = true;
        }

        public override int VoltageSourceCount { get { return bits; } }

        public override int PostCount { get { return bits + 2; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RING_COUNTER; } }

        string getChipName() { return "ring counter"; }

        protected override bool needsBits() { return true; }

        public override void SetupPins() {
            sizeX = bits > 2 ? bits : 2;
            sizeY = 2;
            pins = new Pin[PostCount];
            pins[0] = new Pin(this, 1, SIDE_W, "");
            pins[0].clock = true;
            pins[1] = new Pin(this, sizeX - 1, SIDE_S, "R");
            pins[1].bubble = true;
            int i;
            for (i = 0; i != bits; i++) {
                int ii = i + 2;
                pins[ii] = new Pin(this, i, SIDE_N, "Q" + i);
                pins[ii].output = pins[ii].state = true;
            }
            allocNodes();
        }

        protected override void execute() {
            int i;

            /* if we just loaded then the volts[] array is likely to be all zeroes,
             * which might force us to do a reset, so defer execution until the next iteration */
            if (mJustLoaded) {
                mJustLoaded = false;
                return;
            }

            if (pins[0].value && !lastClock) {
                for (i = 0; i != bits; i++) {
                    if (pins[i + 2].value) {
                        break;
                    }
                }
                if (i < bits) {
                    pins[i++ + 2].value = false;
                }
                i %= bits;
                pins[i + 2].value = true;
            }
            if (!pins[1].value) {
                for (i = 1; i != bits; i++) {
                    pins[i + 2].value = false;
                }
                pins[2].value = true;
            }
            lastClock = pins[0].value;
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
    }
}
