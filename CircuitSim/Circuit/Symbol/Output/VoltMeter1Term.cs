using System.Drawing;

namespace Circuit.Symbol.Output {
    class VoltMeter1Term : VoltMeter {
        PointF mTextPos;

        public VoltMeter1Term(Point pos) : base(pos) {}

        public VoltMeter1Term(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {}

        public override DUMP_ID DumpId { get { return DUMP_ID.VOLTMETER1; } }

        public override void SetPoints() {
            base.SetPoints();
            if (Post.Vertical) {
                InterpolationLead(ref mTextPos, 1 - 0.5 * CustomGraphics.Instance.GetTextSize("-9.99mV").Height / Post.Len);
            } else {
                InterpolationLead(ref mTextPos, 1 - 0.5 * CustomGraphics.Instance.GetTextSize("-9.99mV").Width / Post.Len);
            }
            InterpolationPost(ref mCenter, 1 + 11.0 / Post.Len);
        }

        public override void Draw(CustomGraphics g) {
            DrawLeadA();
            if (MustShowVoltage()) {
                DrawCenteredText(DrawValues(), mTextPos);
            }
        }
    }
}
