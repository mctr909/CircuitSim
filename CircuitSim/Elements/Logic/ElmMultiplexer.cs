using Circuit.Elements.Custom;
using Circuit.UI.Custom;

namespace Circuit.Elements.Logic {
    class ElmMultiplexer : ElmChip {
        int mSelectBitCount = 2;
        int mOutputCount;

        public ElmMultiplexer() : base() { }

        public ElmMultiplexer(StringTokenizer st) : base(st) {
            mSelectBitCount = st.nextTokenInt(mSelectBitCount);
        }

        public override int TermCount { get { return mOutputCount + mSelectBitCount + 1; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override void SetupPins(Chip chip) {
            chip.sizeX = mSelectBitCount + 1;
            mOutputCount = 1;
            for (var i = 0; i != mSelectBitCount; i++) {
                mOutputCount <<= 1;
            }
            chip.sizeY = mOutputCount + 1;

            Pins = new Chip.Pin[TermCount];
            for (var i = 0; i != mOutputCount; i++) {
                Pins[i] = new Chip.Pin(chip, i, Chip.SIDE_W, "I" + i);
            }
            int n = mOutputCount;
            for (var i = 0; i != mSelectBitCount; i++, n++) {
                Pins[n] = new Chip.Pin(chip, i + 1, Chip.SIDE_S, "S" + i);
            }
            Pins[n] = new Chip.Pin(chip, 0, Chip.SIDE_E, "Q");
            Pins[n].output = true;

            AllocNodes();
        }

        protected override void execute() {
            int selectedValue = 0;
            for (var i = 0; i != mSelectBitCount; i++) {
                if (Pins[mOutputCount + i].value) {
                    selectedValue |= 1 << i;
                }
            }
            Pins[mOutputCount + mSelectBitCount].value = Pins[selectedValue].value;
        }
    }
}
