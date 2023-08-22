using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using Circuit;

class PDF {
    const string FontName = "Arial";
    
    public class Page : CustomGraphics {
        public const float Width = 841.92f;
        public const float Height = 595.32f;
        readonly float FONT_SCALE;
        readonly float TEXT_SCALE;
        readonly float PIX_SCALE;

        MemoryStream mMs;
        StreamWriter mSw;
        double mOfsX;
        double mOfsY;
        double mBoardOfsX;
        double mBoardOfsY;

        public override Color DrawColor {
            set {
                mSw.WriteLine("{0} {1} {2} RG",
                    (value.R / 255.0).ToString("0.##"),
                    (value.G / 255.0).ToString("0.##"),
                    (value.B / 255.0).ToString("0.##")
                );
            }
        }

        public Page(int width, int height) : base(width, height) {
            mMs = new MemoryStream();
            mSw = new StreamWriter(mMs);
            mOfsX = 0.0;
            mOfsY = 0.0;
            mBoardOfsX = 0.0;
            mBoardOfsY = 0.0;
            FONT_SCALE = 1.2f;
            TEXT_SCALE = FONT_SCALE * 1.2f;
            PIX_SCALE = FONT_SCALE * 0.65f;
        }

        internal void Flush(FileStream fs) {
            mSw.Flush();
            mMs.Seek(0, SeekOrigin.Begin);
            var ms = new MemoryStream();
            var tmp = new StreamWriter(ms);
            tmp.WriteLine("q");
            tmp.WriteLine("0 w");
            tmp.WriteLine("0.5 0 0 -0.5 0 {0} cm", Height);
            tmp.WriteLine("BT");
            var sr = new StreamReader(mMs);
            while (!sr.EndOfStream) {
                tmp.WriteLine(sr.ReadLine());
            }
            tmp.WriteLine("ET");
            tmp.WriteLine("Q");
            tmp.Flush();

            var enc = Deflate.Compress(ms.ToArray());
            var sw = new StreamWriter(fs);
            sw.NewLine = "\n";
            sw.WriteLine("<</Filter /FlateDecode /Length {0}>>stream", enc.Length + 2);
            sw.Flush();
            fs.WriteByte(0x68);
            fs.WriteByte(0xDE);
            fs.Write(enc, 0, enc.Length);
            fs.Flush();
            sw.WriteLine();
            sw.WriteLine("endstream");
            sw.Flush();
        }

        public override void DrawLeftText(string s, int x, int y) {
            writeText(s, x, y);
        }

        public override void DrawRightText(string s, int x, int y) {
            writeText(s, x, y, GetTextSize(s).Width);
        }

        public override void DrawCenteredText(string s, int x, int y) {
            writeText(s, x, y, GetTextSize(s).Width * 0.5f);
        }

        public override void DrawCenteredLText(string s, int x, int y) {
            writeTextL(s, x, y, GetTextSizeL(s).Width * 0.5f);
        }

        public override void DrawCenteredVText(string s, int x, int y) {
            writeTextV(s, x, y, GetTextSize(s).Width * 0.5f);
        }

        public override void DrawPost(PointF p) {
            fillCircleF(p.X, p.Y, 2.5f);
        }

        public override void DrawHandle(Point p) {
            fillCircleF(p.X, p.Y, 4);
        }

        public override void DrawCurrent(int cx, int cy, float radius) { }

        public override void DrawLine(float ax, float ay, float bx, float by) {
            writeM(ax, ay);
            writeLS(bx, by);
        }

        public override void DrawLine(PointF a, PointF b) {
            DrawLine(a.X, a.Y, b.X, b.Y);
        }

        public override void DrawRectangle(Rectangle rect) {
            var x0 = rect.X;
            var x1 = x0 + rect.Width - 1;
            var y0 = rect.Y;
            var y1 = y0 + rect.Height - 1;
            DrawLine(x0, y0, x1, y0);
            DrawLine(x1, y0, x1, y1);
            DrawLine(x1, y1, x0, y1);
            DrawLine(x0, y1, x0, y0);
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

        public override void DrawCircle(PointF c, float radius) {
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

        public override void FillRectangle(int x, int y, int width, int heght) {
            writeM(x, y);
            writeL(x + width, y);
            writeL(x + width, y + heght);
            writeL(x, y + heght);
            writeLF(x, y);
        }

        public override void FillPolygon(Color color, Point[] poly) {
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLF(p);
        }

        public override void FillPolygon(Color color, PointF[] poly) {
            var p = poly[0];
            writeM(p.X, p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                writeL(p);
            }
            p = poly[0];
            writeLF(p);
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
            writeLF(p);
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

        void writeFontSize(float size) {
            mSw.WriteLine("/F0 {0} Tf", (size * FONT_SCALE).ToString("0.##"));
        }

        void writeText(string text) {
            mSw.WriteLine("({0}) Tj", text);
        }

        void writeText(string s, float x, float y, float ofsX = 0.0f) {
            writeFontSize(TextSize);
            var ofsY = TextSize * PIX_SCALE * 0.5f;
            var strs = s.Replace("\r", "").Split('\n');
            foreach (var str in strs) {
                mSw.WriteLine("1 0 0 -1 {0} {1} Tm",
                    (x + mOfsX - ofsX * PIX_SCALE).ToString("0.##"),
                    (y + mOfsY + ofsY).ToString("0.##")
                );
                writeText(str.Replace("\n", ""));
                ofsY += TextSize * (PIX_SCALE + 0.2f);
            }
        }

        void writeTextL(string s, float x, float y, float ofsX = 0.0f) {
            writeFontSize(LTextSize);
            mSw.WriteLine("1 0 0 -1 {0} {1} Tm",
                (x + mOfsX - ofsX * PIX_SCALE).ToString("0.##"),
                (y + mOfsY + LTextSize * PIX_SCALE * 0.5f).ToString("0.##")
            );
            writeText(s);
        }

        void writeTextV(string s, float x, float y, float ofsY = 0.0f) {
            writeFontSize(TextSize);
            mSw.WriteLine("0 -1 -1 0 {0} {1} Tm",
                (x + mOfsX + TextSize * TEXT_SCALE).ToString("0.##"),
                (y + mOfsY + ofsY * PIX_SCALE).ToString("0.##")
            );
            writeText(s);
        }

        void writeM(float x, float y) {
            mSw.WriteLine("{0} {1} m",
                (x + mOfsX).ToString("0.##"),
                (y + mOfsY).ToString("0.##")
            );
        }

        void writeL(float x, float y) {
            mSw.WriteLine("{0} {1} l",
                (x + mOfsX).ToString("0.##"),
                (y + mOfsY).ToString("0.##")
            );
        }

        void writeL(PointF p) {
            writeL(p.X, p.Y);
        }

        void writeLS(float x, float y) {
            mSw.WriteLine("{0} {1} l S",
                (x + mOfsX).ToString("0.##"),
                (y + mOfsY).ToString("0.##")
            );
        }

        void writeLF(float x, float y) {
            mSw.WriteLine("{0} {1} l f",
                (x + mOfsX).ToString("0.##"),
                (y + mOfsY).ToString("0.##")
            );
        }

        void writeLF(PointF p) {
            writeLF(p.X, p.Y);
        }

        byte[] comp(MemoryStream ms) {
            var compMs = new MemoryStream();
            var comp = new DeflateStream(compMs, CompressionLevel.Optimal);
            var arr = ms.ToArray();
            comp.Write(arr, 0, arr.Length);
            comp.Flush();
            comp.Close();
            return compMs.ToArray();
        }

        public override void ScrollBoard(Point p) {
            mBoardOfsX = p.X;
            mBoardOfsY = p.Y;
        }

        public override void SetPlotFloat(int x, int y) {
            mOfsX = x - mBoardOfsX;
            mOfsY = y - mBoardOfsY;
        }

        public override void ClearTransform() {
            mOfsX = 0.0;
            mOfsY = 0.0;
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
        sw.NewLine = "\n";
        sw.WriteLine("%PDF-1.7");
        sw.Flush();
        fs.WriteByte(0xE2);
        fs.WriteByte(0xE3);
        fs.WriteByte(0xCF);
        fs.WriteByte(0xD3);
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
            sw.WriteLine("  /MediaBox [0 0 {0} {1}]", Page.Width, Page.Height);
            sw.WriteLine("  /Contents {0} 0 R", mPageList.Count + pIdx + 4);
            sw.WriteLine(">>");
            sw.WriteLine("endobj");
            sw.WriteLine();
        }
        for (int pIdx = 0; pIdx < mPageList.Count; pIdx++) {
            sw.WriteLine("{0} 0 obj", mPageList.Count + pIdx + 4);
            sw.Flush();
            mPageList[pIdx].Flush(fs);
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
