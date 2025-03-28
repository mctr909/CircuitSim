﻿using Circuit.Symbol.Custom;

namespace Circuit.Elements.Custom {
	abstract class ElmChip : BaseElement {
		protected bool lastClock;

		public Chip.Pin[] Pins { get; protected set; }

		public virtual int Bits { get; protected set; } = 4;

		public virtual bool NeedsBits { get { return false; } }

		protected ElmChip() : base() { }

		public virtual void SetupPins(Chip ui) { }

		public override bool HasGroundConnection(int nodeIndex) { return Pins[nodeIndex].output; }

		public override void Reset() {
			for (int i = 0; i != TermCount; i++) {
				Pins[i].value = false;
				Pins[i].curcount = 0;
				NodeVolts[i] = 0;
			}
			lastClock = false;
		}

		public override void SetVoltageSource(int n, int v) {
			for (int i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output && n-- == 0) {
					p.voltSource = v;
					return;
				}
			}
			Console.WriteLine("setVoltageSource failed for " + this);
		}

		public override bool HasConnection(int n1, int n2) { return false; }

		public override void Stamp() {
			for (int i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output) {
					StampVoltageSource(0, NodeId[i], p.voltSource);
				}
			}
		}

		#region [method(Circuit)]
		public override void DoIteration() {
			int i;
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (!p.output) {
					p.value = NodeVolts[i] > 2.5;
				}
			}
			execute();
			for (i = 0; i != TermCount; i++) {
				var p = Pins[i];
				if (p.output) {
					UpdateVoltage(p.voltSource, p.value ? 5 : 0);
				}
			}
		}

		public override double GetCurrent(int n) {
			return Pins[n].current;
		}

		public override void SetCurrent(int x, double c) {
			for (int i = 0; i != TermCount; i++) {
				if (Pins[i].output && Pins[i].voltSource == x) {
					Pins[i].current = c;
				}
			}
		}
		#endregion

		protected virtual void execute() { }
	}
}
