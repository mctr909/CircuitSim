using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Circuit;

class PDF {
    public const float Width = 841.92f;
    public const float Height = 595.32f;
    const string FontName = "Arial";
    
    public class Page : CustomGraphics {
        public float Scale { get; private set; }
        readonly float FONT_SCALE;
        readonly float TEXT_SCALE_X;
        readonly float TEXT_SCALE_Y;
        
        MemoryStream mMs;
        StreamWriter mSw;

        public Page(int width, int height, float scale = 1.0f) : base(width, height) {
            mMs = new MemoryStream();
            mSw = new StreamWriter(mMs);
            mSw.WriteLine("0 w");
            Scale = scale;
            FONT_SCALE = Scale * 1.25f;
            TEXT_SCALE_X = FONT_SCALE * 0.65f;
            TEXT_SCALE_Y = FONT_SCALE * 1.2f;
        }

        internal void Flush(StreamWriter sw) {
            mSw.Flush();
            mMs.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(mMs);
            while (!sr.EndOfStream) {
                sw.WriteLine(sr.ReadLine());
            }
        }

        public override void DrawLeftText(string s, int x, int y) {
            writeText(s, x, y);
        }

        public override void DrawRightText(string s, int x, int y) {
            writeText(s, x, y, GetTextSize(s).Width * TEXT_SCALE_X);
        }

        public override void DrawCenteredText(string s, int x, int y) {
            writeText(s, x, y, GetTextSize(s).Width * TEXT_SCALE_X * 0.5f);
        }

        public override void DrawCenteredLText(string s, int x, int y) {
            writeTextL(s, x, y, GetTextSizeL(s).Width * TEXT_SCALE_X * 0.5f);
        }

        public override void DrawCenteredVText(string s, int x, int y) {
            writeTextV(s, x, y, GetTextSize(s).Width * TEXT_SCALE_X * 0.5f);
        }

        public override void DrawPost(PointF p) {
            fillCircleF(p.X, p.Y, 2);
        }

        public override void DrawHandle(Point p) {
            fillCircleF(p.X, p.Y, 4);
        }

        public override void DrawLine(float ax, float ay, float bx, float by) {
            writeM(ax, ay);
            writeLS(bx, by);
        }

        public override void DrawLine(PointF a, PointF b) {
            DrawLine(a.X, a.Y, b.X, b.Y);
        }

        public override void DrawPolygon(Point[] poly) {
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLS(p.X, p.Y);
        }

        public override void DrawCircle(Point c, float radius) {
            var poly = polyCircle(c.X, c.Y, radius);
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLS(p.X, p.Y);
        }

        public override void DrawArc(Point c, float diameter, float start, float sweep) {
            var poly = polyCircle(c.X, c.Y, diameter * 0.5f, start, sweep);
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length - 1; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[poly.Length - 1];
            writeLS(p.X, p.Y);
        }

        public override void FillPolygon(Color color, Point[] poly) {
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLB(p);
        }

        public override void FillCircle(int cx, int cy, float radius) {
            fillCircleF(cx, cy, radius);
        }

        void fillCircleF(float cx, float cy, float radius) {
            var poly = polyCircle(cx, cy, radius);
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLB(p);
        }

        PointF[] polyCircle(float cx, float cy, float radius, float start = 0, float sweep = 360) {
            var poly = new PointF[radius < 4 ? 8 : 16];
            var sRad = Math.PI * start / 180;
            var ssweep = sweep / 360.0;
            for (int i = 0; i < poly.Length; i++) {
                var th = 2 * Math.PI * (i + 0.5) * ssweep / poly.Length + sRad;
                poly[i] = new PointF(
                    (float)(cx + radius * Math.Cos(th)),
                    (float)(cy + radius * Math.Sin(th))
                );
            }
            return poly;
        }

        void writeText(string s, float x, float y, float ofsX = 0.0f) {
            writeFontSize(TextSize);
            mSw.WriteLine("1 0 0 1 {0} {1} Tm", x * Scale - ofsX, Height - TextSize * TEXT_SCALE_X * 0.5f - y * Scale);
            writeText(s);
        }

        void writeTextL(string s, float x, float y, float ofsX = 0.0f) {
            writeFontSize(LTextSize);
            mSw.WriteLine("1 0 0 1 {0} {1} Tm", x * Scale - ofsX, Height - LTextSize * TEXT_SCALE_X * 0.5f - y * Scale);
            writeText(s);
        }

        void writeTextV(string s, float x, float y, float ofsY = 0.0f) {
            writeFontSize(TextSize);
            mSw.WriteLine("0 1 -1 0 {0} {1} Tm", x * Scale + TextSize * TEXT_SCALE_Y, Height - ofsY - y * Scale);
            writeText(s);
        }

        void writeFontSize(float size) {
            mSw.WriteLine("/F0 {0} Tf", size * FONT_SCALE);
        }

        void writeText(string text) {
            mSw.WriteLine("({0}) Tj", text);
        }

        void writeM(float x, float y) {
            mSw.WriteLine("{0} {1} m", x * Scale, Height - y * Scale);
        }

        void writeL(PointF p) {
            mSw.WriteLine("{0} {1} l", p.X * Scale, Height - p.Y * Scale);
        }

        void writeLS(float x, float y) {
            mSw.WriteLine("{0} {1} l S", x * Scale, Height - y * Scale);
        }

        void writeLB(PointF p) {
            mSw.WriteLine("{0} {1} l b", p.X * Scale, Height - p.Y * Scale);
        }
    }

    List<Page> mPageList = new List<Page>();

    public void AddPage(Page page) {
        mPageList.Add(page);
    }

    public void Save(string path) {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.GetDirectoryName(path))) {
            return;
        }
        var fs = new FileStream(path, FileMode.Create);
        var sw = new StreamWriter(fs);
        sw.WriteLine("%PDF-1.7");
        sw.WriteLine();
        sw.WriteLine("1 0 obj");
        sw.WriteLine("<<");
        sw.WriteLine("  /Type /Catalog");
        sw.WriteLine("  /Pages 2 0 R");
        sw.WriteLine(">>");
        sw.WriteLine("endobj");
        sw.WriteLine();
        sw.WriteLine("2 0 obj");
        sw.WriteLine("<<");
        sw.WriteLine("  /Type /Pages");
        sw.Write("  /Kids [");
        for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
            sw.Write("{0} 0 R ", pIdx + 4);
        }
        sw.WriteLine("]");
        sw.WriteLine("  /Count {0}", mPageList.Count);
        sw.WriteLine(">>");
        sw.WriteLine("endobj");
        sw.WriteLine();
        sw.WriteLine("3 0 obj");
        sw.WriteLine("<<");
        sw.WriteLine("  /Font <<");
        sw.WriteLine("    /F0 <<");
        sw.WriteLine("      /Type /Font");
        sw.WriteLine("      /BaseFont /{0}", FontName);
        sw.WriteLine("      /Subtype /Type1");
        sw.WriteLine("    >>");
        sw.WriteLine("  >>");
        sw.WriteLine(">>");
        sw.WriteLine("endobj");
        sw.WriteLine();
        for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
            sw.WriteLine("{0} 0 obj", pIdx + 4);
            sw.WriteLine("<<");
            sw.WriteLine("  /Type /Page");
            sw.WriteLine("  /Parent 2 0 R");
            sw.WriteLine("  /Resources 3 0 R");
            sw.WriteLine("  /MediaBox [0 0 {0} {1}]", Width, Height);
            sw.WriteLine("  /Contents {0} 0 R", mPageList.Count + pIdx + 4);
            sw.WriteLine(">>");
            sw.WriteLine("endobj");
            sw.WriteLine();
        }
        for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
            sw.WriteLine("{0} 0 obj", mPageList.Count + pIdx + 4);
            sw.WriteLine("<< >>");
            sw.WriteLine("stream");
            sw.WriteLine("BT");
            mPageList[pIdx].Flush(sw);
            sw.WriteLine("ET");
            sw.WriteLine("endstream");
            sw.WriteLine("endobj");
            sw.WriteLine();
        }
        sw.WriteLine("xref");
        sw.WriteLine("trailer");
        sw.WriteLine("<<");
        sw.WriteLine("  /Size {0}", mPageList.Count * 2 + 4);
        sw.WriteLine("  /Root 1 0 R");
        sw.WriteLine(">>");
        sw.WriteLine("startxref");
        sw.WriteLine("0");
        sw.WriteLine("%%EOF");
        sw.Close();
        sw.Dispose();
    }
}
