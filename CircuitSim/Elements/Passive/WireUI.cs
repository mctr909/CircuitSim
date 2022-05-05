using System.Drawing;

namespace Circuit.Elements.Passive {
    class WireUI : BaseUI {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public WireUI(Point pos) : base(pos) {
            CirElm = new WireElm();
        }

        public WireUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new WireElm();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

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
            arr[1] = "I = " + Utils.CurrentAbsText(CirElm.Current);
            arr[2] = "V = " + Utils.VoltageText(CirElm.Volts[0]);
        }
    }
}
