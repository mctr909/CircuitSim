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
                Bits = st.nextTokenInt();
            }
            int i;
            for (i = 0; i != PostCount; i++) {
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
            for (int i = 0; i != PostCount; i++) {
                Pins[i].value = false;
                Pins[i].curcount = 0;
                Volts[i] = 0;
            }
            lastClock = false;
        }

        public override bool AnaHasGroundConnection(int n1) {
            return Pins[n1].output;
        }

        public override void AnaSetVoltageSource(int j, int vs) {
            for (int i = 0; i != PostCount; i++) {
                var p = Pins[i];
                if (p.output && j-- == 0) {
                    p.voltSource = vs;
                    return;
                }
            }
            Console.WriteLine("setVoltageSource failed for " + this);
        }

        public override void AnaStamp() {
            for (int i = 0; i != PostCount; i++) {
                var p = Pins[i];
                if (p.output) {
                    Circuit.StampVoltageSource(0, Nodes[i], p.voltSource);
                }
            }
        }

        public override void CirDoIteration() {
            int i;
            for (i = 0; i != PostCount; i++) {
                var p = Pins[i];
                if (!p.output) {
                    p.value = Volts[i] > 2.5;
                }
            }
            execute();
            for (i = 0; i != PostCount; i++) {
                var p = Pins[i];
                if (p.output) {
                    Circuit.UpdateVoltageSource(p.voltSource, p.value ? 5 : 0);
                }
            }
        }

        protected virtual void execute() { }

        public override void CirSetCurrent(int x, double c) {
            for (int i = 0; i != PostCount; i++) {
                if (Pins[i].output && Pins[i].voltSource == x) {
                    Pins[i].current = c;
                }
            }
        }

        public override bool AnaGetConnection(int n1, int n2) { return false; }

        public override double CirGetCurrentIntoNode(int n) {
            return Pins[n].current;
        }
    }
}
