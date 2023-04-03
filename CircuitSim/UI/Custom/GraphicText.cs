using System.Collections.Generic;
using System.Drawing;

namespace Circuit.UI.Custom {
    class GraphicText : Graphic {
        const int FLAG_BAR = 2;
        const int FLAG_ESCAPE = 4;

        string mText;
        int mSize;

        public GraphicText(Point pos) : base(pos) {
            mText = "Text";
            mSize = 11;
        }

        public GraphicText(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            mSize = int.Parse(st.nextToken());
            mText = st.nextToken();
            mText = Utils.Unescape(mText);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TEXT; } }

        protected override void dump(List<object> optionList) {
            DumpInfo.Flags |= FLAG_ESCAPE;
            optionList.Add(mSize);
            optionList.Add(Utils.Escape(mText));
        }

        public override double Distance(int x, int y) {
            return DumpInfo.BoxDistance(DumpInfo.BoundingBox, x, y);
        }

        public override void Drag(Point p) {
            DumpInfo.SetPosition(p.X, p.Y, p.X + 16, p.Y);
        }

        public override void Draw(CustomGraphics g) {
            var sizeBk = CustomGraphics.TextSize;
            var colorBk = CustomGraphics.TextColor;
            CustomGraphics.TextSize = mSize;
            var size = g.GetTextSize(mText);
            DumpInfo.SetP2(
                (int)(DumpInfo.P1.X + size.Width),
                (int)(DumpInfo.P1.Y + size.Height)
            );
            if (NeedsHighlight) {
                CustomGraphics.TextColor = CustomGraphics.SelectColor;
            }
            g.DrawLeftText(mText, DumpInfo.P1.X, (int)(DumpInfo.P1.Y + size.Height / 2));
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            CustomGraphics.TextSize = sizeBk;
            CustomGraphics.TextColor = colorBk;
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("テキスト", mText, true);
            }
            if (r == 1) {
                return new ElementInfo("サイズ", mSize, true);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                mText = ei.Textf.Text;
            }
            if (n == 1) {
                mSize = (int)ei.Value;
            }
        }
    }
}
