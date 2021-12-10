using System.Drawing;

namespace Circuit.Elements.Passive {
    class WireElm : CircuitElm {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public WireElm(Point pos) : base(pos) { }

        public WireElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WIRE; } }

        protected override string dump() { return ""; }

        public override void SetPoints() {
            base.SetPoints();
        }

        public override void Draw(CustomGraphics g) {
            drawLead(mPoint1, mPoint2);
            doDots();
            setBbox(mPoint1, mPoint2, 3);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ワイヤ";
            arr[1] = "I = " + Utils.CurrentAbsText(mCurrent);
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
        }
    }
}
