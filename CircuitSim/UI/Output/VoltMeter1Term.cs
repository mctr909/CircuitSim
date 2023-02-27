using System.Drawing;

namespace Circuit.UI.Output {
    class VoltMeter1Term : VoltMeter {
        public VoltMeter1Term(Point pos) : base(pos) {}

        public VoltMeter1Term(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {}

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER1; } }

        public override void SetPoints() {
            base.SetPoints();
            if (mVertical) {
                setLead1(1 - 0.5 * Context.GetTextSize("1.99mV").Height / mLen);
            } else {
                setLead1(1 - 0.5 * Context.GetTextSize("1.99mV").Width / mLen);
            }
            interpPoint(ref mCenter, 1 + 11.0 / mLen);
            setBbox(Elm.Post1, mCenter, 0);
        }

        public override void Draw(CustomGraphics g) {
            setBbox(1);
            if (NeedsHighlight) {
                g.DrawColor = CustomGraphics.SelectColor;
            } else {
                g.DrawColor = CustomGraphics.LineColor;
            }
            drawLeadA();
            if (mustShowVoltage()) {
                drawCenteredText(drawValues(), DumpInfo.P2X, DumpInfo.P2Y, true);
            }
        }
    }
}
