namespace Circuit.Elements {
    class CustomCompositeChipElm : ChipElm {
        public CustomCompositeChipElm(int xx, int yy) : base(xx, yy) {
            setSize(2);
        }

        public override int VoltageSourceCount { get { return 0; } }

        public override int PostCount { get { return null == pins ? 1 : pins.Length; } }

        protected override bool needsBits() { return false; }

        public override void SetupPins() { }

        void setPins(Pin[] p) {
            pins = p;
        }

        public void allocPins(int n) {
            pins = new Pin[n];
        }

        public void setPin(int n, int p, int s, string t) {
            pins[n] = new Pin(this, p, s, t);
            pins[n].fixName();
        }
    }
}
