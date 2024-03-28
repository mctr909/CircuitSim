using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Logic {
	class ElmLatch : ElmChip {
		int mLoadPin;
		bool mLastLoad = false;

		public override int TermCount { get { return Bits * 2 + 1; } }

		public override int VoltageSourceCount { get { return Bits; } }

		public override bool NeedsBits { get { return true; } }

		public ElmLatch() : base() {
			//Bits = st.nextTokenInt(Bits);
			//Setup(mElm, st);
		}

		public override void SetupPins(Chip chip) {
			chip.sizeX = 2;
			chip.sizeY = Bits + 1;
			Pins = new Chip.Pin[TermCount];
			for (var i = 0; i != Bits; i++) {
				Pins[i] = new Chip.Pin(chip, Bits - 1 - i, Chip.SIDE_W, "I" + i);
			}
			for (var i = 0; i != Bits; i++) {
				Pins[i + Bits] = new Chip.Pin(chip, Bits - 1 - i, Chip.SIDE_E, "O") {
					output = true,
					state = true
				};
			}
			Pins[mLoadPin = Bits * 2] = new Chip.Pin(chip, Bits, Chip.SIDE_W, "Ld");
			alloc_nodes();
		}

		protected override void execute() {
			if (Pins[mLoadPin].value && !mLastLoad) {
				for (var i = 0; i != Bits; i++) {
					Pins[i + Bits].value = Pins[i].value;
				}
			}
			mLastLoad = Pins[mLoadPin].value;
		}
	}
}
