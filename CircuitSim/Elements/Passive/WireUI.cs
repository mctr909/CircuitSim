using System.Drawing;

namespace Circuit.Elements.Passive {
    class WireUI : BaseUI {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public WireUI(Point pos) : base(pos) {
            Elm = new WireElm();
        }

        public WireUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new WireElm();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WIRE; } }

        protected override string dump() { return ""; }

        public override void SetPoints() {
            base.SetPoints();
        }

        public override void Draw(CustomGraphics g) {
            drawLead(mPost1, mPost2);
            doDots();
            setBbox(mPost1, mPost2, 3);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ワイヤ";
            arr[1] = "I = " + Utils.CurrentAbsText(Elm.Current);
            arr[2] = "V = " + Utils.VoltageText(Elm.Volts[0]);
        }
    }
}
