using System.Drawing;

namespace Circuit.Elements {
    class GroundElm : CircuitElm {
        public GroundElm(int xx, int yy) : base(xx, yy) { }

        public GroundElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) { }

        public override DUMP_ID getDumpType() { return DUMP_ID.GROUND; }

        public override int getPostCount() { return 1; }

        public override void draw(Graphics g) {
            PEN_THICK_LINE.Color = getVoltageColor(0);
            drawThickLine(g, point1, point2);
            for (int i = 0; i != 3; i++) {
                int a = 10 - i * 4;
                int b = i * 5; /* -10; */
                interpPoint(point1, point2, ref ps1, ref ps2, 1 + b / dn, a);
                drawThickLine(g, ps1, ps2);
            }
            doDots(g);
            interpPoint(point1, point2, ref ps2, 1 + 11.0 / dn);
            setBbox(point1, ps2, 11);
            drawPosts(g);
        }

        public override void setCurrent(int x, double c) { current = -c; }

        public override void stamp() {
            cir.stampVoltageSource(0, nodes[0], voltSource, 0);
        }

        public override double getVoltageDiff() { return 0; }

        public override int getVoltageSourceCount() { return 1; }

        public override void getInfo(string[] arr) {
            arr[0] = "ground";
            arr[1] = "I = " + getCurrentText(getCurrent());
        }

        public override bool hasGroundConnection(int n1) { return true; }

        public override DUMP_ID getShortcut() { return DUMP_ID.GROUND; }

        public override double getCurrentIntoNode(int n) { return -current; }
    }
}
