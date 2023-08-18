using Circuit.Elements.Custom;
using Circuit.UI.Custom;

namespace Circuit.Elements.Logic {
    class ElmDeMultiplexer : ElmChip {
        int mSelectBitCount = 2;
        int mOutputCount;
        int mqPin = 6;

        public ElmDeMultiplexer() : base() { }

        public ElmDeMultiplexer(Chip chip, StringTokenizer st) : base(st) {
            st.nextTokenInt(out mSelectBitCount, mSelectBitCount);
        }

        public override int PostCount { get { return mqPin + 1; } }

        public override int AnaVoltageSourceCount { get { return mOutputCount; } }

        public override void SetupPins(Chip chip) {
            mOutputCount = 1 << mSelectBitCount;
            mqPin = mOutputCount + mSelectBitCount;
            chip.sizeX = 1 + mSelectBitCount;
            chip.sizeY = 1 + mOutputCount;
            AllocNodes();
            Pins = new Chip.Pin[PostCount];
            for (var i = 0; i != mOutputCount; i++) {
                Pins[i] = new Chip.Pin(chip, i, Chip.SIDE_E, "Q" + i);
                Pins[i].output = true;
            }
            for (var i = 0; i != mSelectBitCount; i++) {
                var ii = i + mOutputCount;
                Pins[ii] = new Chip.Pin(chip, i, Chip.SIDE_S, "S" + i);
            }
            Pins[mqPin] = new Chip.Pin(chip, 0, Chip.SIDE_W, "Q");
        }

        protected override void execute() {
            int val = 0;
            for (var i = 0; i != mSelectBitCount; i++) {
                if (Pins[i + mOutputCount].value) {
                    val |= 1 << i;
                }
            }
            for (var i = 0; i != mOutputCount; i++) {
                Pins[i].value = false;
            }
            Pins[val].value = Pins[mqPin].value;
        }
    }
}
