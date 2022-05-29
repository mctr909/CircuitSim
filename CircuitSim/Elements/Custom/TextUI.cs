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
            mFlags |= FLAG_ESCAPE;
            return mSize + " " + Utils.Escape(mText);
        }

        public override double Distance(double x, double y) {
            return BoundingBox.Contains((int)x, (int)y) ? 0 : Math.Min(
                Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P1.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P1.Y, P2.X, P2.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P2.Y, P1.X, P2.Y, x, y), 
                Utils.DistanceOnLine(P1.X, P2.Y, P1.X, P1.Y, x, y)
            )));
        }

        public override void Drag(Point p) {
            P1 = p;
            P2.X = p.X + 16;
            P2.Y = p.Y;
        }

        public override void Draw(CustomGraphics g) {
            var bkColor = CustomGraphics.TextColor;
            var bkSize = CustomGraphics.TextSize;
            CustomGraphics.TextColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            CustomGraphics.TextSize = mSize;
            var size = g.GetTextSize(mText);
            P2.X = (int)(P1.X + size.Width);
            P2.Y = (int)(P1.Y + size.Height);
            g.DrawLeftText(mText, P1.X, (int)(P1.Y + size.Height / 2));
            setBbox(P1, P2);
            CustomGraphics.TextColor = bkColor;
            CustomGraphics.TextSize = bkSize;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("テキスト", 0, -1, -1);
                ei.TextArea = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    Width = 150,
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
