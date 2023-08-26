using System.Drawing;

namespace Circuit.UI.Output {
    class VoltMeter1Term : VoltMeter {
        PointF mTextPos;

        public VoltMeter1Term(Point pos) : base(pos) {}

        public VoltMeter1Term(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {}

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER1; } }

        public override void SetPoints() {
            base.SetPoints();
            if (mVertical) {
                interpLead(ref mTextPos, 1 - 0.5 * Context.GetTextSize("-9.99mV").Height / mLen);
            } else {
                interpLead(ref mTextPos, 1 - 0.5 * Context.GetTextSize("-9.99mV").Width / mLen);
            }
            interpPost(ref mCenter, 1 + 11.0 / mLen);
            setBbox(1);
        }

        public override void Draw(CustomGraphics g) {
            drawLeadA();
            if (mustShowVoltage()) {
                drawCenteredText(drawValues(), mTextPos, true);
            }
        }
    }
}
