using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Custom {
    class TextElm : GraphicElm {
        const int FLAG_CENTER = 1;
        const int FLAG_BAR = 2;
        const int FLAG_ESCAPE = 4;

        string mText;
        int mSize;

        public TextElm(Point pos) : base(pos) {
            mText = "hello";
            mSize = 24;
        }

        public TextElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            mSize = int.Parse(st.nextToken());
            mText = st.nextToken();
            mText = CustomLogicModel.unescape(mText);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TEXT; } }

        protected override string dump() {
            mFlags |= FLAG_ESCAPE;
            return mSize + " " + CustomLogicModel.escape(mText);
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
            if (0 < (mFlags & FLAG_CENTER)) {
                var p1 = new Point(
                    (int)(P1.X - size.Width / 2),
                    (int)(P1.Y - size.Height / 2)
                );
                P2.X = (int)(P1.X + size.Width / 2);
                P2.Y = (int)(P1.Y + size.Height / 2);
                g.DrawCenteredText(mText, P1.X, P1.Y);
                setBbox(p1, P2);
            } else {
                P2.X = (int)(P1.X + size.Width);
                P2.Y = (int)(P1.Y + size.Height);
                g.DrawLeftText(mText, P1.X, (int)(P1.Y + size.Height / 2));
                setBbox(P1, P2);
            }
            CustomGraphics.TextColor = bkColor;
            CustomGraphics.TextSize = bkSize;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("テキスト", 0, -1, -1);
                ei.TextArea = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    ScrollBars = ScrollBars.Vertical,
                    Text = mText
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("サイズ", mSize, 5, 100);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "中央",
                    Checked = (mFlags & FLAG_CENTER) != 0
                };
                return ei;
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
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_CENTER;
                } else {
                    mFlags &= ~FLAG_CENTER;
                }
            }
        }
    }
}
