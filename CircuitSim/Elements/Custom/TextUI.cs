using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Custom {
    class TextUI : GraphicUI {
        const int FLAG_BAR = 2;
        const int FLAG_ESCAPE = 4;

        string mText;
        int mSize;

        public TextUI(Point pos) : base(pos) {
            mText = "Text";
            mSize = 11;
        }

        public TextUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            mSize = int.Parse(st.nextToken());
            mText = st.nextToken();
            mText = Utils.Unescape(mText);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TEXT; } }

        protected override string dump() {
            DumpInfo.Flags |= FLAG_ESCAPE;
            return mSize + " " + Utils.Escape(mText);
        }

        public override double Distance(int x, int y) {
            return DumpInfo.BoxDistance(DumpInfo.BoundingBox, x, y);
        }

        public override void Drag(Point p) {
            DumpInfo.SetPosition(p.X, p.Y, p.X + 16, p.Y);
        }

        public override void Draw(CustomGraphics g) {
            var bkColor = CustomGraphics.TextColor;
            var bkSize = CustomGraphics.TextSize;
            CustomGraphics.TextColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            CustomGraphics.TextSize = mSize;
            var size = g.GetTextSize(mText);
            DumpInfo.SetP2(
                (int)(DumpInfo.P1.X + size.Width),
                (int)(DumpInfo.P1.Y + size.Height)
            );
            g.DrawLeftText(mText, DumpInfo.P1.X, (int)(DumpInfo.P1.Y + size.Height / 2));
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            CustomGraphics.TextColor = bkColor;
            CustomGraphics.TextSize = bkSize;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("テキスト", 0, -1, -1);
                ei.TextArea = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    Width = 250,
                    ScrollBars = ScrollBars.Vertical,
                    Text = mText
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("サイズ", mSize, 5, 100);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mText = ei.TextArea.Text;
            }
            if (n == 1) {
                mSize = (int)ei.Value;
            }
        }
    }
}
