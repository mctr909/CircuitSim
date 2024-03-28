using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Logic {
	class ElmDeMultiplexer : ElmChip {
		public int SelectBitCount = 2;

		int mOutputCount;
		int mqPin = 6;

		public override int TermCount { get { return mqPin + 1; } }

		public override int VoltageSourceCount { get { return mOutputCount; } }

		public ElmDeMultiplexer() : base() {
			//Setup(mElm, st);
			//SelectBitCount = st.nextTokenInt(2);
		}

		public override void SetupPins(Chip chip) {
			mOutputCount = 1 << SelectBitCount;
			mqPin = mOutputCount + SelectBitCount;
			chip.sizeX = 1 + SelectBitCount;
			chip.sizeY = 1 + mOutputCount;
			alloc_nodes();
			Pins = new Chip.Pin[TermCount];
			for (var i = 0; i != mOutputCount; i++) {
				Pins[i] = new Chip.Pin(chip, i, Chip.SIDE_E, "Q" + i) {
					output = true
				};
			}
			for (var i = 0; i != SelectBitCount; i++) {
				var ii = i + mOutputCount;
				Pins[ii] = new Chip.Pin(chip, i, Chip.SIDE_S, "S" + i);
			}
			Pins[mqPin] = new Chip.Pin(chip, 0, Chip.SIDE_W, "Q");
		}

		protected override void execute() {
			int val = 0;
			for (var i = 0; i != SelectBitCount; i++) {
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
