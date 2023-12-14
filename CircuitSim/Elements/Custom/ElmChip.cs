using System;

using Circuit.UI.Custom;

namespace Circuit.Elements.Custom {
	abstract class ElmChip : BaseElement {
		protected bool lastClock;

		public Chip.Pin[] Pins { get; protected set; }

		public virtual int Bits { get; protected set; } = 4;

		public ElmChip() : base() { }

		public ElmChip(StringTokenizer st) : base() {
			if (NeedsBits()) {
				var v = st.nextTokenInt(Bits);
				Bits = v;
			}
			int i;
			for (i = 0; i != TermCount; i++) {
				if (Pins == null) {
					Volts[i] = st.nextTokenDouble();
				} else if (Pins[i].state) {
					Volts[i] = st.nextTokenDouble();
					Pins[i].value = Volts[i] > 2.5;
				}
			}
		}

		public virtual bool NeedsBits() { return false; }

		public virtual void SetupPins(Chip ui) { }

		public override void Reset() {
			for (int i = 0; i != TermCount; i++) {
				Pins[i].value = false;
				Pins[i].curcount = 0;
				Volts[i] = 0;
			}
			lastClock = false;
		}

		public override bool HasGroundConnection(int n1) {
			return Pins[n1].output;
		}

		public override void SetVoltageSource(int j, int vs) {
			for (int i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output && j-- == 0) {
					p.voltSource = vs;
					return;
				}
			}
			Console.WriteLine("setVoltageSource failed for " + this);
		}

		public override void Stamp() {
			for (int i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output) {
					Circuit.StampVoltageSource(0, Nodes[i], p.voltSource);
				}
			}
		}

		public override void DoIteration() {
			int i;
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (!p.output) {
					p.value = Volts[i] > 2.5;
				}
			}
			execute();
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output) {
					Circuit.UpdateVoltageSource(p.voltSource, p.value ? 5 : 0);
				}
			}
		}

		protected virtual void execute() { }

		public override void SetCurrent(int x, double c) {
			for (int i = 0; i != TermCount; i++) {
				if (Pins[i].output && Pins[i].voltSource == x) {
					Pins[i].current = c;
				}
			}
		}

		public override bool GetConnection(int n1, int n2) { return false; }

		public override double GetCurrentIntoNode(int n) {
			return Pins[n].current;
		}
	}
}
