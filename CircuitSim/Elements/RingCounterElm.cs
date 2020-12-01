﻿namespace Circuit.Elements {
    class RingCounterElm : ChipElm {
        bool mJustLoaded;

        public RingCounterElm(int xx, int yy) : base(xx, yy) { }

        public RingCounterElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            mJustLoaded = true;
        }

        string getChipName() { return "ring counter"; }

        bool needsBits() { return true; }

        public override void setupPins() {
            sizeX = bits > 2 ? bits : 2;
            sizeY = 2;
            pins = new Pin[getPostCount()];
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

        public override int getPostCount() { return bits + 2; }

        public override int getVoltageSourceCount() { return bits; }

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

        public override EditInfo getEditInfo(int n) {
            if (n < 2) {
                return base.getEditInfo(n);
            }
            if (n == 2) {
                return new EditInfo("# of Bits", bits, 1, 1).SetDimensionless();
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n < 2) {
                base.setEditValue(n, ei);
                return;
            }
            if (n == 2 && ei.Value >= 2) {
                bits = (int)ei.Value;
                setupPins();
                setPoints();
            }
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.RING_COUNTER; }
    }
}
