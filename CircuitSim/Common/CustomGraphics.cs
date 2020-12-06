using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace Circuit {
    class CustomGraphics {
        public static readonly Font FontText = new Font("Segoe UI", 9.0f);
        static readonly Font mFontLText = new Font("Segoe UI", 14.0f);
        static readonly StringFormat mTextLeft = new StringFormat() {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat mTextLeftTop = new StringFormat() {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Far
        };
        static readonly StringFormat mTextRight = new StringFormat() {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat mTextCenter = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        Bitmap mImage;
        Graphics g;
        Brush mBrushText;

        Pen mPenPost = new Pen(Color.Red, 5.0f);
        Pen mPenColor = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen mPenLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen mPenThickLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };

        public Color TextColor {
            set {
                var p = new Pen(value, 1.0f);
                mBrushText = p.Brush;
            }
        }
        public Color PostColor {
            get { return mPenPost.Color; }
            set { mPenPost.Color = value; }
        }
        public Color LineColor {
            get { return mPenLine.Color; }
            set { mPenLine.Color = value; }
        }
        public Color ThickLineColor {
            get { return mPenThickLine.Color; }
            set { mPenThickLine.Color = value; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        CustomGraphics() { }
        CustomGraphics(Bitmap image) {
            Width = image.Width;
            Height = image.Height;
            mImage = image;
            g = Graphics.FromImage(mImage);
        }
        CustomGraphics(int width, int height) {
            Width = width;
            Height = height;
            mImage = new Bitmap(Width, Height);
            g = Graphics.FromImage(mImage);
        }

        public void CopyTo(Graphics g) {
            g.DrawImage(mImage, 0, 0);
        }

        public void Dispose() {
            g.Dispose();
            g = null;
            if (null != mImage) {
                mImage.Dispose();
                mImage = null;
            }
        }

        public void Print() {
            var antiAlias = g.SmoothingMode == SmoothingMode.AntiAlias;
            if (!antiAlias) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            }

            var p = new PrintDocument();
            p.PrintPage += new PrintPageEventHandler((s, e) => {
                e.PageSettings.Landscape = true;
                e.Graphics.DrawImage(mImage, 0, 0, mImage.Width, mImage.Height);
                e.HasMorePages = false;
            });
            p.Print();

            if (!antiAlias) {
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.None;
            }
        }

        public static CustomGraphics FromImage(Bitmap image) {
            return new CustomGraphics(image);
        }

        public static CustomGraphics FromImage(int width, int height) {
            return new CustomGraphics(width, height);
        }

        public void DrawPost(Point p) {
            g.FillPie(mPenPost.Brush, p.X - mPenPost.Width / 2, p.Y - mPenPost.Width / 2, mPenPost.Width, mPenPost.Width, 0, 360);
        }

        public void DrawPost(float x, float y) {
            g.FillPie(mPenPost.Brush, x - mPenPost.Width / 2, y - mPenPost.Width / 2, mPenPost.Width, mPenPost.Width, 0, 360);
        }

        public void DrawLeftText(string s, float x, float y) {
            g.DrawString(s, FontText, mBrushText, x, y, mTextLeft);
        }

        public void DrawLeftTopText(string s, float x, float y) {
            g.DrawString(s, FontText, mBrushText, x, y, mTextLeftTop);
        }

        public void DrawRightText(string s, float x, float y) {
            g.DrawString(s, FontText, mBrushText, x, y, mTextRight);
        }

        public void DrawCenteredText(string s, float x, float y) {
            g.DrawString(s, FontText, mBrushText, x, y, mTextCenter);
        }

        public void DrawCenteredText(string s, float x, float y, Font font) {
            g.DrawString(s, font, mBrushText, x, y, mTextCenter);
        }

        public void DrawCenteredLText(string s, float x, float y) {
            g.DrawString(s, mFontLText, mBrushText, x, y, mTextCenter);
        }

        public void DrawLine(float ax, float ay, float bx, float by) {
            g.DrawLine(mPenLine, ax, ay, bx, by);
        }

        public void DrawRectangle(float x, float y, float width, float height) {
            g.DrawRectangle(mPenLine, x, y, width, height);
        }

        public void DrawCircle(float centerX, float centerY, float radius) {
            g.DrawArc(mPenLine, centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 360);
        }

        public void DrawPolygon(Point[] p) {
            g.DrawPolygon(mPenLine, p);
        }

        public void DrawThickLine(float ax, float ay, float bx, float by) {
            g.DrawLine(mPenThickLine, ax, ay, bx, by);
        }

        public void DrawThickLine(Point a, Point b) {
            g.DrawLine(mPenThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickLine(Color color, Point a, Point b) {
            mPenThickLine.Color = color;
            g.DrawLine(mPenThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickCircle(float centerX, float centerY, float diameter) {
            var md = diameter * .98f;
            g.DrawArc(mPenThickLine, centerX - md / 2, centerY - md / 2, md, md, 0, 360);
        }

        public void DrawThickArc(float centerX, float centerY, float diameter, float start, float sweep) {
            var md = diameter * .98f;
            g.DrawArc(mPenThickLine, centerX - md / 2, centerY - md / 2, md, md, start, sweep);
        }

        public void DrawThickPolygon(Point[] p) {
            g.DrawPolygon(mPenThickLine, p);
        }

        public void FillRectangle(float x, float y, float width, float height) {
            g.FillRectangle(mPenLine.Brush, x, y, width, height);
        }

        public void FillRectangle(Color color, float x, float y, float width, float height) {
            mPenColor.Color = color;
            g.FillRectangle(mPenColor.Brush, x, y, width, height);
        }

        public void FillCircle(float cx, float cy, float radius) {
            g.FillPie(mPenLine.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public void FillCircle(Color color, float cx, float cy, float radius) {
            mPenColor.Color = color;
            g.FillPie(mPenColor.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public void FillPolygon(Color color, Point[] p) {
            mPenColor.Color = color;
            g.FillPolygon(mPenColor.Brush, p);
        }

        public SizeF GetTextSize(string s) {
            return g.MeasureString(s, FontText);
        }

        public SizeF GetTextSize(string s, Font font) {
            return g.MeasureString(s, font);
        }

        public SizeF GetLTextSize(string s) {
            return g.MeasureString(s, mFontLText);
        }

        public void SetTransform(Matrix matrix) {
            g.Transform = matrix;
        }

        public void ClearTransform() {
            g.Transform = new Matrix(1, 0, 0, 1, 0, 0);
        }

        public void Clear(Color color) {
            g.Clear(color);
        }
    }
}
