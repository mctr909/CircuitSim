using System.Drawing;
using System.Drawing.Drawing2D;

namespace Circuit {
    public class CustomGraphics {
        static readonly StringFormat mAlignLeft = new StringFormat() {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat mAlignRight = new StringFormat() {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat mAlignCenter = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat mAlignCenterV = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.DirectionVertical,
        };

        protected static Font mTextFontL = new Font("Arial", 11.0f);

        static Font mTextFont = new Font("Arial", 9.0f);
        static Brush mTextBrush = Brushes.Black;
        static Color mTextColor;

        Pen mPenPost = new Pen(Color.Red, 5.0f);
        Pen mPenColor = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen mPenLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle,
            Width = 0.1f
        };

        Bitmap mImage;
        Graphics mG;

        public static Brush PenHandle { get; set; }
        public static Color SelectColor { get; set; }
        public static Color WhiteColor { get; set; }
        public static Color GrayColor { get; set; }
        public static Color TextColor {
            get { return mTextColor; }
            set {
                var p = new Pen(value, 1.0f);
                mTextBrush = p.Brush;
                mTextColor = value;
            }
        }
        public static float TextSize {
            get { return mTextFont.Size; }
            set {
                mTextFont = new Font(mTextFont.Name, value);
            }
        }
        public static float LTextSize {
            get { return mTextFontL.Size; }
            set {
                mTextFontL = new Font(mTextFontL.Name, value);
            }
        }
        public bool DoPrint { get; set; } = false;
        public Color PostColor {
            get { return mPenPost.Color; }
            set { mPenPost.Color = value; }
        }
        public Color LineColor {
            get { return mPenLine.Color; }
            set { mPenLine.Color = value; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        CustomGraphics() { }

        public CustomGraphics(int width, int height) {
            Width = width;
            Height = height;
            mImage = new Bitmap(Width, Height);
            mG = Graphics.FromImage(mImage);
        }

        public void CopyTo(Graphics g) {
            g.DrawImage(mImage, 0, 0);
        }

        public void Dispose() {
            mG.Dispose();
            mG = null;
            if (null != mImage) {
                mImage.Dispose();
                mImage = null;
            }
        }

        public static CustomGraphics FromImage(int width, int height) {
            return new CustomGraphics(width, height);
        }

        public virtual void DrawLeftText(string s, int x, int y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignLeft);
        }

        public virtual void DrawRightText(string s, int x, int y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignRight);
        }

        public virtual void DrawCenteredText(string s, int x, int y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignCenter);
        }

        public virtual void DrawCenteredLText(string s, int x, int y) {
            mG.DrawString(s, mTextFontL, mTextBrush, x, y + 1, mAlignCenter);
        }

        public virtual void DrawCenteredVText(string s, int x, int y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignCenterV);
        }

        public virtual void DrawPost(PointF p) {
            mG.FillPie(mPenPost.Brush, p.X - mPenPost.Width / 2, p.Y - mPenPost.Width / 2, mPenPost.Width, mPenPost.Width, 0, 360);
        }

        public virtual void DrawHandle(Point p) {
            var radius = 4;
            mG.FillPie(PenHandle, p.X - radius, p.Y - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void DrawLine(float ax, float ay, float bx, float by) {
            mG.DrawLine(mPenLine, ax, ay, bx, by);
        }

        public virtual void DrawLine(PointF a, PointF b) {
            mG.DrawLine(mPenLine, a, b);
        }

        public void DrawRectangle(Rectangle rect) {
            mG.DrawRectangle(mPenLine, rect);
        }

        public void DrawDashRectangle(int x, int y, int w, int h) {
            mPenLine.DashStyle = DashStyle.Dash;
            mPenLine.DashPattern = new float[] { 3, 5 };
            mG.DrawRectangle(mPenLine, new Rectangle(x, y, w, h));
            mPenLine.DashStyle = DashStyle.Solid;
        }

        public virtual void DrawCircle(Point p, float radius) {
            mG.DrawArc(mPenLine, p.X - radius, p.Y - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void DrawArc(Point p, float diameter, float start, float sweep) {
            var md = diameter * .98f;
            mG.DrawArc(mPenLine, p.X - md / 2, p.Y - md / 2, md, md, start, sweep);
        }

        public virtual void DrawPolygon(Point[] p) {
            mG.DrawPolygon(mPenLine, p);
        }

        public void FillRectangle(int x, int y, int width, int height) {
            mG.FillRectangle(mPenLine.Brush, x, y, width, height);
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height) {
            mG.FillRectangle(brush, x, y, width, height);
        }

        public virtual void FillCircle(int cx, int cy, float radius) {
            mG.FillPie(mPenLine.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void FillPolygon(Color color, Point[] p) {
            mPenColor.Color = color;
            mG.FillPolygon(mPenColor.Brush, p);
        }

        public SizeF GetTextSize(string s) {
            return mG.MeasureString(s, mTextFont);
        }

        public SizeF GetTextSizeL(string s) {
            return mG.MeasureString(s, mTextFontL);
        }

        public void SetTransform(Matrix matrix) {
            mG.Transform = matrix;
        }

        public void ClearTransform() {
            mG.Transform = new Matrix(1, 0, 0, 1, 0, 0);
        }

        public void Clear(Color color) {
            mG.Clear(color);
        }
    }
}
