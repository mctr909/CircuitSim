namespace Circuit.Elements {
    class CustomCompositeChipElm : ChipElm {
        public CustomCompositeChipElm(int xx, int yy) : base(xx, yy) {
            setSize(2);
        }

        bool needsBits() { return false; }

        public override void setupPins() { }

        public override int getVoltageSourceCount() { return 0; }

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

        public override int getPostCount() { return pins == null ? 1 : pins.Length; }
    }
}
