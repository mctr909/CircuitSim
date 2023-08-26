using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Wire : BaseUI {
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
            setBbox(3);
        }

        public override void Draw(CustomGraphics g) {
            drawLine(Elm.Post[0], Elm.Post[1]);
            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ワイヤ";
            arr[1] = "I = " + Utils.CurrentAbsText(Elm.Current);
            arr[2] = "V = " + Utils.VoltageText(Elm.Volts[0]);
        }
    }
}
