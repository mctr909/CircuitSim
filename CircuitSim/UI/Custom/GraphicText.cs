using System.Collections.Generic;
using System.Drawing;

namespace Circuit.UI.Custom {
    class GraphicText : Graphic {
        string mText;
        int mFontSize;
        SizeF mTextSize;

        public GraphicText(Point pos) : base(pos) {
            mText = "Text";
            mFontSize = 11;
        }

        public GraphicText(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            mFontSize = st.nextTokenInt(11);
            st.nextToken(out mText);
            mText = Utils.Unescape(mText);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.TEXT; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(mFontSize);
            optionList.Add(Utils.Escape(mText));
        }

        public override double Distance(Point p) {
            return Post.BoxDistance(Post.GetRect(), p);
        }

        public override void SetPoints() {
            base.SetPoints();
            setTextSize();
        }

        public override void Drag(Point p) {
            p = CirSimForm.SnapGrid(p);
            Post.SetPosition(p.X, p.Y, p.X + 16, p.Y);
            setTextSize();
        }

        public override void Draw(CustomGraphics g) {
            var sizeBk = g.FontSize;
            g.FontSize = mFontSize;
            drawLeftText(mText, Post.A.X, (int)(Post.A.Y + mTextSize.Height / 2));
            g.FontSize = sizeBk;
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("テキスト", mText, true);
            }
            if (r == 1) {
                return new ElementInfo("サイズ", mFontSize, false);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                mText = ei.Text;
            }
            if (n == 1) {
                mFontSize = (int)ei.Value;
            }
            setTextSize();
        }

        void setTextSize() {
            var g = CustomGraphics.Instance;
            var sizeBk = g.FontSize;
            g.FontSize = mFontSize;
            mTextSize = g.GetTextSize(mText);
            Post.B.X = (int)(Post.A.X + mTextSize.Width);
            Post.B.Y = (int)(Post.A.Y + mTextSize.Height);
            g.FontSize = sizeBk;
        }
    }
}
