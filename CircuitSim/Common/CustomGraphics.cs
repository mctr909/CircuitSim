using System;
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

        protected static Font mTextFontL = new Font("Arial", 11.0f);

        static Font mTextFont = new Font("Arial", 9.0f);
        static Font mElementFont = new Font("MS Gothic", 9.0f);
        static Brush mTextBrush = Brushes.Black;
        static Color mTextColor;
        static Pen mPenPost = new Pen(Color.Green, 5.0f);
        static Pen mPenHandle = new Pen(Color.FromArgb(95, 0, 255, 255), 1.0f);

        Pen mPenLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle,
            Width = 0.1f
        };
        Pen mPenFill = new Pen(Color.White, 1.0f);
        protected Bitmap mImage;
        Graphics mG;

        public const float POST_RADIUS = 2.5f;
        public const float HANDLE_RADIUS = 6.5f;

        public static Color SelectColor { get; private set; }
        public static Color WhiteColor { get; private set; }
        public static Color LineColor { get; private set; }
        public static Color PostColor {
            get { return mPenPost.Color; }
            private set { mPenPost.Color = value; }
        }
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
            set { mTextFont = new Font(mTextFont.Name, value); }
        }
        public static float LTextSize {
            get { return mTextFontL.Size; }
            set { mTextFontL = new Font(mTextFontL.Name, value); }
        }
        
        public bool DoPrint { get; set; } = false;

        public virtual Color DrawColor {
            get { return mPenLine.Color; }
            set { mPenLine.Color = value; }
        }
        public virtual Color FillColor {
            get { return mPenFill.Color; }
            set { mPenFill.Color = value; }
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

        public CustomGraphics(Bitmap image) {
            Width = image.Width;
            Height = image.Height;
            mImage = image;
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

        public static void SetColor(bool isPrintable) {
            if (isPrintable) {
                WhiteColor = Color.Gray;
                LineColor = Color.Black;
                TextColor = Color.Black;
                SelectColor = Color.Black;
                PostColor = Color.Black;
                mPenHandle.Color = Color.Black;
            } else {
                WhiteColor = Color.FromArgb(191, 191, 191);
                LineColor = Color.FromArgb(95, 95, 95);
                TextColor = Color.FromArgb(147, 147, 147);
                SelectColor = Color.FromArgb(0, 255, 255);
                PostColor = Color.FromArgb(0, 127, 0);
                mPenHandle.Color = Color.FromArgb(95, 0, 255, 255);
            }
        }

        public static CustomGraphics FromImage(int width, int height) {
            return new CustomGraphics(width, height);
        }

        public void DrawElementText(string s, float x, float y) {
            mG.DrawString(s, mElementFont, mTextBrush, x, y, mAlignLeft);
        }

        public virtual void DrawLeftText(string s, float x, float y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignLeft);
        }

        public virtual void DrawRightText(string s, PointF p) {
            mG.DrawString(s, mTextFont, mTextBrush, p.X, p.Y, mAlignRight);
        }

        public virtual void DrawRightText(string s, int x, int y) {
            mG.DrawString(s, mTextFont, mTextBrush, x, y, mAlignRight);
        }

        public virtual void DrawCenteredText(string s, PointF p, double rotateAngle) {
            mG.TranslateTransform(p.X, p.Y);
            mG.RotateTransform((float)(rotateAngle * 180 / Math.PI));
            mG.DrawString(s, mTextFont, mTextBrush, 0, 0, mAlignCenter);
            mG.RotateTransform(-(float)(rotateAngle * 180 / Math.PI));
            mG.TranslateTransform(-p.X, -p.Y);
        }

        public virtual void DrawCenteredLText(string s, PointF p) {
            mG.DrawString(s, mTextFontL, mTextBrush, p.X, p.Y + 1, mAlignCenter);
        }

        public virtual void DrawPost(PointF p) {
            mG.FillPie(mPenPost.Brush,
                p.X - POST_RADIUS, p.Y - POST_RADIUS,
                POST_RADIUS * 2, POST_RADIUS * 2,
                0, 360
            );
        }

        public virtual void DrawHandle(PointF p) {
            mG.FillPie(mPenHandle.Brush,
                p.X - HANDLE_RADIUS, p.Y - HANDLE_RADIUS,
                HANDLE_RADIUS * 2, HANDLE_RADIUS * 2,
                0, 360
            );
        }

        public virtual void DrawCurrent(float cx, float cy, float radius) {
            mG.FillPie(Brushes.Snow, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void DrawLine(float ax, float ay, float bx, float by) {
            mG.DrawLine(mPenLine, ax, ay, bx, by);
        }

        public virtual void DrawLine(PointF a, PointF b) {
            mG.DrawLine(mPenLine, a, b);
        }

        public virtual void DrawRectangle(Rectangle rect) {
            mG.DrawRectangle(mPenLine, rect);
        }

        public virtual void DrawDashRectangle(float x, float y, float w, float h) {
            mPenLine.DashStyle = DashStyle.Dash;
            mPenLine.DashPattern = new float[] { 2, 3 };
            mG.DrawRectangle(mPenLine, x, y, w, h);
            mPenLine.DashStyle = DashStyle.Solid;
        }

        public virtual void DrawCircle(PointF p, float radius) {
            mG.DrawArc(mPenLine, p.X - radius, p.Y - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void DrawArc(PointF p, float diameter, float start, float sweep) {
            var md = diameter * .98f;
            mG.DrawArc(mPenLine, p.X - md / 2, p.Y - md / 2, md, md, start, sweep);
        }

        public virtual void DrawPolyline(PointF[] p) {
            mG.DrawLines(mPenLine, p);
        }

        public virtual void DrawPolygon(PointF[] p) {
            mG.DrawPolygon(mPenLine, p);
        }

        public virtual void FillRectangle(int x, int y, int width, int height) {
            mG.FillRectangle(mPenFill.Brush, x, y, width, height);
        }

        public virtual void FillCircle(float cx, float cy, float radius) {
            mG.FillPie(mPenFill.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void FillPolygon(Color color, PointF[] p) {
            mPenFill.Color = color;
            mG.FillPolygon(mPenFill.Brush, p);
        }

        public virtual void ScrollBoard(Point p) {
            mG.Transform = new Matrix(1, 0, 0, 1, p.X, p.Y);
        }

        public virtual void SetPlotFloat(int x, int y) {
            mG.Transform = new Matrix(1, 0, 0, 1, x, y);
        }

        public virtual void ClearTransform() {
            mG.Transform = new Matrix(1, 0, 0, 1, 0, 0);
        }

        public SizeF GetTextSize(string s) {
            return mG.MeasureString(s, mTextFont);
        }

        public SizeF GetTextSizeL(string s) {
            return mG.MeasureString(s, mTextFontL);
        }

        public void Clear(Color color) {
            mG.Clear(color);
        }
    }
}
