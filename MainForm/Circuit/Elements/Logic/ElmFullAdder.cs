using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Logic {
	class ElmFullAdder : ElmChip {
		public ElmFullAdder() : base() { }

		public override int TermCount { get { return 5; } }

		public override int VoltageSourceCount { get { return 2; } }

		public override void SetupPins(Chip chip) {
			chip.sizeX = 2;
			chip.sizeY = 3;
			Pins = new Chip.Pin[TermCount];
			Pins[0] = new Chip.Pin(chip, 2, Chip.SIDE_E, "S") {
				output = true
			};
			Pins[1] = new Chip.Pin(chip, 0, Chip.SIDE_E, "C") {
				output = true
			};
			Pins[2] = new Chip.Pin(chip, 0, Chip.SIDE_W, "A");
			Pins[3] = new Chip.Pin(chip, 1, Chip.SIDE_W, "B");
			Pins[4] = new Chip.Pin(chip, 2, Chip.SIDE_W, "Cin");
		}

		protected override void execute() {
			Pins[0].value = (Pins[2].value ^ Pins[3].value) ^ Pins[4].value;
			Pins[1].value =
				(Pins[2].value && Pins[3].value) ||
				(Pins[2].value && Pins[4].value) ||
				(Pins[3].value && Pins[4].value);
		}
	}
}
