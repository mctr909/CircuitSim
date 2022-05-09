using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace Circuit {
    class CustomGraphics {
        public static Font FontText = new Font("Segoe UI", 9.0f);
        static Brush brushText = Brushes.Black;
        static Color colorText;

        public static Brush PenHandle { get; set; }
        public static Color SelectColor { get; set; }
        public static Color WhiteColor { get; set; }
        public static Color GrayColor { get; set; }
        public static Color TextColor {
            get { return colorText; }
            set {
                var p = new Pen(value, 1.0f);
                brushText = p.Brush;
                colorText = value;
            }
        }
        public static float TextSize {
            get { return FontText.Size; }
            set {
                FontText = new Font(FontText.Name, value);
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
        static readonly StringFormat textRightV = new StringFormat() {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.DirectionVertical,
        };
        static readonly StringFormat textCenter = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        static readonly StringFormat textCenterV = new StringFormat() {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.DirectionVertical,
        };

        Bitmap image;
        Graphics g;

        Pen penPost = new Pen(Color.Red, 5.0f);
        Pen penColor = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        Pen penLine = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle,
            Width = 0.1f
        };

        public CustomGraphics() { }
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

        public void DrawPost(PointF p) {
            g.FillPie(penPost.Brush, p.X - penPost.Width / 2, p.Y - penPost.Width / 2, penPost.Width, penPost.Width, 0, 360);
        }

        public void DrawLeftText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textLeft);
        }

        public virtual void DrawLeftTopText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textLeftTop);
        }

        public void DrawRightText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textRight);
        }

        public void DrawRightVText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textRightV);
        }

        public void DrawCenteredText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textCenter);
        }

        public void DrawCenteredText(string s, int x, int y, Font font) {
            g.DrawString(s, font, brushText, x, y, textCenter);
        }

        public void DrawCenteredLText(string s, int x, int y) {
            g.DrawString(s, fontLText, brushText, x, y + 1, textCenter);
        }

        public void DrawCenteredVText(string s, int x, int y) {
            g.DrawString(s, FontText, brushText, x, y, textCenterV);
        }

        public virtual void DrawLine(int ax, int ay, int bx, int by) {
            g.DrawLine(penLine, ax, ay, bx, by);
        }

        public virtual void DrawLine(Point a, Point b) {
            g.DrawLine(penLine, a, b);
        }

        public void DrawRectangle(Rectangle rect) {
            g.DrawRectangle(penLine, rect);
        }

        public void DrawDashRectangle(int x, int y, int w, int h) {
            penLine.DashStyle = DashStyle.Dash;
            g.DrawRectangle(penLine, new Rectangle(x, y, w, h));
            penLine.DashStyle = DashStyle.Solid;
        }

        public void DrawCircle(Point p, float radius) {
            g.DrawArc(penLine, p.X - radius, p.Y - radius, radius * 2, radius * 2, 0, 360);
        }

        public void DrawArc(Point p, float diameter, float start, float sweep) {
            var md = diameter * .98f;
            g.DrawArc(penLine, p.X - md / 2, p.Y - md / 2, md, md, start, sweep);
        }

        public virtual void DrawPolygon(Point[] p) {
            g.DrawPolygon(penLine, p);
        }

        public void FillRectangle(int x, int y, int width, int height) {
            g.FillRectangle(penLine.Brush, x, y, width, height);
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height) {
            g.FillRectangle(brush, x, y, width, height);
        }

        public void FillCircle(int cx, int cy, float radius) {
            g.FillPie(penLine.Brush, cx - radius, cy - radius, radius * 2, radius * 2, 0, 360);
        }

        public void FillCircle(Brush brush, Point pos, float radius) {
            g.FillPie(brush, pos.X - radius, pos.Y - radius, radius * 2, radius * 2, 0, 360);
        }

        public virtual void FillPolygon(Color color, Point[] p) {
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
