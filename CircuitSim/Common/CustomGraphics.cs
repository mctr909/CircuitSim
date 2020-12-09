using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace Circuit {
    class CustomGraphics {
        public static readonly Font FontText = new Font("Segoe UI", 8.5f);

        public Color TextColor {
            set {
                var p = new Pen(value, 1.0f);
                brushText = p.Brush;
            }
        }
        public Color PostColor {
            get { return penPost.Color; }
            set { penPost.Color = value; }
        }
        public Color LineColor {
            get { return penLine.Color; }
            set { penLine.Color = value; }
        }
        public Color ThickLineColor {
            get { return penThickLine.Color; }
            set { penThickLine.Color = value; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        static readonly Font fontLText = new Font("Segoe UI", 14.0f);
        static readonly StringFormat textLeft = new StringFormat() {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat textLeftTop = new StringFormat() {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Far
        };
        static readonly StringFormat textRight = new StringFormat() {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat textCenter = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        Bitmap image;
        Graphics g;
        Brush brushText;

        Pen penPost = new Pen(Color.Red, 5.0f);
        Pen penColor = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen penLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen penThickLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };

        CustomGraphics() { }
        CustomGraphics(Bitmap image) {
            Width = image.Width;
            Height = image.Height;
            this.image = image;
            g = Graphics.FromImage(this.image);
        }
        CustomGraphics(int width, int height) {
            Width = width;
            Height = height;
            image = new Bitmap(Width, Height);
            g = Graphics.FromImage(image);
        }

        public void CopyTo(Graphics g) {
            g.DrawImage(image, 0, 0);
        }

        public void Dispose() {
            g.Dispose();
            g = null;
            if (null != image) {
                image.Dispose();
                image = null;
            }
        }

        public void Print() {
            var p = new PrintDocument();
            p.DefaultPageSettings.Landscape = true;
            p.PrintPage += new PrintPageEventHandler((s, e) => {
                e.Graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                e.HasMorePages = false;
            });
            p.Print();
        }

        public static CustomGraphics FromImage(Bitmap image) {
            return new CustomGraphics(image);
        }

        public static CustomGraphics FromImage(int width, int height) {
            return new CustomGraphics(width, height);
        }

        public void DrawPost(Point p) {
            g.FillPie(penPost.Brush, p.X - penPost.Width / 2, p.Y - penPost.Width / 2, penPost.Width, penPost.Width, 0, 360);
        }

        public void DrawPost(float x, float y) {
            g.FillPie(penPost.Brush, x - penPost.Width / 2, y - penPost.Width / 2, penPost.Width, penPost.Width, 0, 360);
        }

        public void DrawLeftText(string s, float x, float y) {
            g.DrawString(s, FontText, brushText, x, y, textLeft);
        }

        public void DrawLeftTopText(string s, float x, float y) {
            g.DrawString(s, FontText, brushText, x, y, textLeftTop);
        }

        public void DrawRightText(string s, float x, float y) {
            g.DrawString(s, FontText, brushText, x, y, textRight);
        }

        public void DrawCenteredText(string s, float x, float y) {
            g.DrawString(s, FontText, brushText, x, y, textCenter);
        }

        public void DrawCenteredText(string s, float x, float y, Font font) {
            g.DrawString(s, font, brushText, x, y, textCenter);
        }

        public void DrawCenteredLText(string s, float x, float y) {
            g.DrawString(s, fontLText, brushText, x, y, textCenter);
        }

        public void DrawLine(float ax, float ay, float bx, float by) {
            g.DrawLine(penLine, ax, ay, bx, by);
        }

        public void DrawRectangle(float x, float y, float width, float height) {
            g.DrawRectangle(penLine, x, y, width, height);
        }

        public void DrawCircle(float centerX, float centerY, float radius) {
            g.DrawArc(penLine, centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 360);
        }

        public void DrawPolygon(Point[] p) {
            g.DrawPolygon(penLine, p);
        }

        public void DrawThickLine(float ax, float ay, float bx, float by) {
            g.DrawLine(penThickLine, ax, ay, bx, by);
        }

        public void DrawThickLine(Point a, Point b) {
            g.DrawLine(penThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickLine(PointF a, PointF b) {
            g.DrawLine(penThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickLine(Color color, Point a, Point b) {
            penThickLine.Color = color;
            g.DrawLine(penThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickLine(Color color, PointF a, PointF b) {
            penThickLine.Color = color;
            g.DrawLine(penThickLine, a.X, a.Y, b.X, b.Y);
        }

        public void DrawThickCircle(float centerX, float centerY, float diameter) {
            var md = diameter * .98f;
            g.DrawArc(penThickLine, centerX - md / 2, centerY - md / 2, md, md, 0, 360);
        }

        public void DrawThickArc(float centerX, float centerY, float diameter, float start, float sweep) {
            var md = diameter * .98f;
            g.DrawArc(penThickLine, centerX - md / 2, centerY - md / 2, md, md, start, sweep);
        }

        public void DrawThickPolygon(Point[] p) {
            g.DrawPolygon(penThickLine, p);
        }

        public void FillRectangle(float x, float y, float width, float height) {
            g.FillRectangle(penLine.Brush, x, y, width, height);
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height) {
            g.FillRectangle(brush, x, y, width, height);
        }

        public void FillCircle(float cx, float cy, float radius) {
            g.FillPie(penLine.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public void FillCircle(Brush brush, float cx, float cy, float radius) {
            g.FillPie(brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public void FillPolygon(Color color, Point[] p) {
            penColor.Color = color;
            g.FillPolygon(penColor.Brush, p);
        }

        public SizeF GetTextSize(string s) {
            return g.MeasureString(s, FontText);
        }

        public SizeF GetTextSize(string s, Font font) {
            return g.MeasureString(s, font);
        }

        public SizeF GetLTextSize(string s) {
            return g.MeasureString(s, fontLText);
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
