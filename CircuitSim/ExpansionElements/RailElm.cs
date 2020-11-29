using System;
using System.Drawing;

namespace Circuit.Elements {
    class RailElm : VoltageElm {
        protected const int FLAG_CLOCK = 1;

        public RailElm(int xx, int yy) : base(xx, yy, WF_DC) {
            numHandles = 1;
        }

        public RailElm(int xx, int yy, int wf) : base(xx, yy, wf) { }

        public RailElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st): base(xa, ya, xb, yb, f, st) {
            numHandles = 1;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.RAIL; }

        public override int getPostCount() { return 1; }

        public override void setPoints() {
            base.setPoints();
            lead1 = interpPoint(point1, point2, 1 - circleSize / dn);
        }

        public string getRailText() {
            return null;
        }

        public override void draw(Graphics g) {
            var rt = getRailText();
            double w = rt == null ? circleSize : g.MeasureString(rt, FONT_TERM_NAME).Width / 2;
            if (w > dn * .8) {
                w = dn * .8;
            }
            lead1 = interpPoint(point1, point2, 1 - w / dn);
            setBbox(point1, point2, circleSize);
            
            drawThickLine(g, getVoltageColor(volts[0]), point1, lead1);
            drawRail(g);
            drawPosts(g);
            curcount = updateDotCount(-current, curcount);
            if (sim.dragElm != this) {
                drawDots(g, point1, lead1, curcount);
            }
        }

        void drawRail(Graphics g) {
            if (waveform == WF_SQUARE && (flags & FLAG_CLOCK) != 0) {
                drawRailText(g, "CLK");
            } else if (waveform == WF_DC || waveform == WF_VAR) {
                var color = needsHighlight() ? selectColor : whiteColor;
                double v = getVoltage();
                string s;
                if (Math.Abs(v) < 1) {
                    s = v.ToString("0.000") + " V";
                } else {
                    s = getShortUnitText(v, "V");
                }
                if (getVoltage() > 0) {
                    s = "+" + s;
                }
                drawCenteredText(g, s, x2, y2, true);
            } else {
                drawWaveform(g, point2);
            }
        }

        void drawRailText(Graphics g, string s) {
            drawCenteredText(g, s, x2, y2, true);
        }

        public override double getVoltageDiff() { return volts[0]; }

        public override void stamp() {
            if (waveform == WF_DC) {
                cir.stampVoltageSource(0, nodes[0], voltSource, getVoltage());
            } else {
                cir.stampVoltageSource(0, nodes[0], voltSource);
            }
        }

        public override void doStep() {
            if (waveform != WF_DC) {
                cir.updateVoltageSource(0, nodes[0], voltSource, getVoltage());
            }
        }

        public override bool hasGroundConnection(int n1) { return true; }

        public override DUMP_ID getShortcut() { return DUMP_ID.RAIL; }
    }
}
