using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.Elements.Custom {
    class CustomCompositeChipElm : ChipElm {
        public CustomCompositeChipElm(Point pos) : base(pos) {
            setSize(2);
        }

        public override int CirVoltageSourceCount { get { return 0; } }

        public override int CirPostCount { get { return null == pins ? 1 : pins.Length; } }

        protected override bool needsBits() { return false; }

        public override void SetupPins() { }

        void setPins(Pin[] p) {
            pins = p;
        }

        public void allocPins(int n) {
            pins = new Pin[n];
            cirAllocNodes();
        }

        public void setPin(int n, int p, int s, string t) {
            pins[n] = new Pin(this, p, s, t);
            pins[n].fixName();
        }
    }
}
