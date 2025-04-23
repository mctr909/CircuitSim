using Circuit.Symbol.Custom;

namespace Circuit.Elements.Custom {
	abstract class ElmChip : BaseElement {
		public bool lastClock;

		public Chip.Pin[] Pins { get; protected set; }

		public virtual int Bits { get; protected set; } = 4;

		public virtual bool NeedsBits { get { return false; } }

		public virtual void SetupPins(Chip ui) { }

		protected override void DoIteration() {
			int i;
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (!p.output) {
					p.value = V[i] > 2.5;
				}
			}
			execute();
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output) {
					UpdateVoltageSource(p.voltSource, p.value ? 5 : 0);
				}
			}
		}

		protected override double GetCurrent(int n) { return Pins[n].current; }

		protected override void SetCurrent(int n, double i) {
			for (int idxP = 0; idxP != TermCount; idxP++) {
				if (Pins[idxP].output && Pins[idxP].voltSource == n) {
					Pins[idxP].current = i;
				}
			}
		}

		public override void SetVoltageSource(int n, int vs) {
			for (int i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output && n-- == 0) {
					p.voltSource = vs;
					return;
				}
			}
			Console.WriteLine("setVoltageSource failed for " + this);
		}

		protected virtual void execute() { }
	}
}
