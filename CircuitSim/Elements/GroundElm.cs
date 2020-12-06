using System.Drawing;

namespace Circuit.Elements {
    class GroundElm : CircuitElm {
        Point ps1;
        Point ps2;

        public GroundElm(int xx, int yy) : base(xx, yy) { }

        public GroundElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.GROUND; } }

        public override double VoltageDiff { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        protected override string dump() { return ""; }

        protected override DUMP_ID getDumpType() { return DUMP_ID.GROUND; }

        public override void Draw(CustomGraphics g) {
            g.ThickLineColor = getVoltageColor(0);
            g.DrawThickLine(mPoint1, mPoint2);
            for (int i = 0; i != 3; i++) {
                int a = 10 - i * 4;
                int b = i * 5; /* -10; */
                interpPoint(mPoint1, mPoint2, ref ps1, ref ps2, 1 + b / mLen, a);
                g.DrawThickLine(ps1, ps2);
            }
            doDots(g);
            interpPoint(mPoint1, mPoint2, ref ps2, 1 + 11.0 / mLen);
            setBbox(mPoint1, ps2, 11);
            drawPosts(g);
        }

        public override void SetCurrent(int x, double c) { mCurrent = -c; }

        public override void Stamp() {
            Cir.StampVoltageSource(0, Nodes[0], mVoltSource, 0);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ground";
            arr[1] = "I = " + getCurrentText(mCurrent);
        }

        public override bool HasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) { return -mCurrent; }
    }
}
