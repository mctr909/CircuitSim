using System.Drawing;

namespace Circuit.Elements.Passive {
    class GroundElm : CircuitElm {
        const int BODY_LEN = 10;

        Point mP1;
        Point mP2;

        public GroundElm(Point pos) : base(pos) {
            CirElm = new GroundElmE();
        }

        public GroundElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new GroundElmE();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.GROUND; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.GROUND; } }

        protected override string dump() { return ""; }

        public override void Draw(CustomGraphics g) {
            drawLead(mPoint1, mPoint2);
            for (int i = 0; i != 3; i++) {
                var a = BODY_LEN - i * 4;
                var b = i * BODY_LEN * 0.5;
                interpPointAB(ref mP1, ref mP2, 1 + b / mLen, a);
                drawLead(mP1, mP2);
            }
            doDots();
            setBbox(mPoint1, mP1, 11);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ground";
            arr[1] = "I = " + Utils.CurrentText(CirElm.Current);
        }
    }
}
