using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Logic {
	class ElmMultiplexer : ElmChip {
		public int SelectBitCount = 2;

		int mOutputCount;

		public override int TermCount { get { return mOutputCount + SelectBitCount + 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		public ElmMultiplexer() : base() {
			//Setup(mElm, st);
			//SelectBitCount = st.nextTokenInt(2);
		}

		public override void SetupPins(Chip chip) {
			chip.sizeX = SelectBitCount + 1;
			mOutputCount = 1;
			for (var i = 0; i != SelectBitCount; i++) {
				mOutputCount <<= 1;
			}
			chip.sizeY = mOutputCount + 1;

			Pins = new Chip.Pin[TermCount];
			for (var i = 0; i != mOutputCount; i++) {
				Pins[i] = new Chip.Pin(chip, i, Chip.SIDE_W, "I" + i);
			}
			int n = mOutputCount;
			for (var i = 0; i != SelectBitCount; i++, n++) {
				Pins[n] = new Chip.Pin(chip, i + 1, Chip.SIDE_S, "S" + i);
			}
			Pins[n] = new Chip.Pin(chip, 0, Chip.SIDE_E, "Q") {
				output = true
			};

			AllocateNodes();
		}

		protected override void execute() {
			int selectedValue = 0;
			for (var i = 0; i != SelectBitCount; i++) {
				if (Pins[mOutputCount + i].value) {
					selectedValue |= 1 << i;
				}
			}
			Pins[mOutputCount + SelectBitCount].value = Pins[selectedValue].value;
		}
	}
}
