using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Wire : BaseUI {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public Wire(Point pos) : base(pos) {
            Elm = new ElmWire();
        }

        public Wire(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmWire();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WIRE; } }

        public override void SetPoints() {
            base.SetPoints();
        }

        public override void Draw(CustomGraphics g) {
            drawLead(mPost1X, mPost1Y, mPost2X, mPost2Y);
            doDots();
            setBbox(3);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ワイヤ";
            arr[1] = "I = " + Utils.CurrentAbsText(Elm.Current);
            arr[2] = "V = " + Utils.VoltageText(Elm.Volts[0]);
        }
    }
}
