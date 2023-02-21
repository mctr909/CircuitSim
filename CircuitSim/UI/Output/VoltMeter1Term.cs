using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class VoltMeter1Term : VoltMeter {
        public VoltMeter1Term(Point pos) : base(pos) { }

        public VoltMeter1Term(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER1; } }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mCenter, (mLen + 10) / mLen);
        }

        public override void Draw(CustomGraphics g) {
            setBbox(1);

            if (NeedsHighlight) {
                g.DrawColor = CustomGraphics.SelectColor;
            } else {
                g.DrawColor = CustomGraphics.LineColor;
            }
            drawLead(mPost1, mPost2);

            drawValues();
            Context.DrawPost(mPost1);
        }
    }
}
