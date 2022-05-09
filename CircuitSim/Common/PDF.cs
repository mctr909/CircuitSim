using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Circuit;

class PDF {
    const int Width = 842;
    const int Height = 595;

    public class Page : CustomGraphics {
        MemoryStream mMs;
        StreamWriter mSw;

        public Page(int width, int height) : base(width, height) {
            mMs = new MemoryStream();
            mSw = new StreamWriter(mMs);
            mSw.WriteLine("0 w");
        }

        internal void Flush(StreamWriter sw) {
            mSw.Flush();
            mMs.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(mMs);
            while (!sr.EndOfStream) {
                sw.WriteLine(sr.ReadLine());
            }
        }

        public override void DrawLeftTopText(string s, int x, int y) {
            mSw.WriteLine("/F0 {0} Tf", (int)TextSize);
            mSw.WriteLine("1 0 0 1 {0} {1} Tm", x, Height - y);
            mSw.WriteLine("({0}) Tj", s);
        }

        public override void DrawLeftText(string s, int x, int y) {
            DrawLeftTopText(s, x, y);
        }

        public override void DrawRightText(string s, int x, int y) {
            DrawLeftTopText(s, x, y);
        }

        public override void DrawRightVText(string s, int x, int y) {
            DrawLeftTopText(s, x, y);
        }

        public override void DrawCenteredVText(string s, int x, int y) {
            DrawLeftTopText(s, x, y);
        }

        public override void DrawLine(int ax, int ay, int bx, int by) {
            mSw.WriteLine("{0} {1} m", ax, Height - ay);
            mSw.WriteLine("{0} {1} l S", bx, Height - by);
        }

        public override void DrawLine(Point a, Point b) {
            DrawLine(a.X, a.Y, b.X, b.Y);
        }

        public override void DrawPolygon(Point[] poly) {
            var p = poly[0];
            mSw.WriteLine("{0} {1} m", p.X, Height - p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                mSw.WriteLine("{0} {1} l", p.X, Height - p.Y);
            }
            p = poly[0];
            mSw.WriteLine("{0} {1} l S", p.X, Height - p.Y);
        }

        public override void FillPolygon(Color color, Point[] poly) {
            var p = poly[0];
            mSw.WriteLine("{0} {1} m", p.X, Height - p.Y);
            for (int i = 1; i < poly.Length; i++) {
                p = poly[i];
                mSw.WriteLine("{0} {1} l", p.X, Height - p.Y);
            }
            p = poly[0];
            mSw.WriteLine("{0} {1} l b", p.X, Height - p.Y);
        }

        public override void FillCircle(int cx, int cy, float radius) {
            FillCircleF(cx, cy, radius);
        }

        public override void FillCircle(Brush brush, Point pos, float radius) {
            FillCircleF(pos.X, pos.Y, radius);
        }

        public override void DrawPost(PointF p) {
            FillCircleF(p.X, p.Y, 2);
        }

        void FillCircleF(float cx, float cy, float radius) {
            var poly = new PointF[16];
            for (int i = 0; i < poly.Length; i++) {
                var th = 2 * Math.PI * i / poly.Length;
                poly[i] = new PointF(
                    (float)(cx + radius * Math.Cos(th)),
                    (float)(cy + radius * Math.Sin(th))
                );
            }
            var p = poly[0];
            mSw.WriteLine("{0} {1} m", p.X, Height - p.Y);
            for (int i = 1; i < poly.Length - 1; i++) {
                p = poly[i];
                mSw.WriteLine("{0} {1} l", p.X, Height - p.Y);
            }
            p = poly[poly.Length - 1];
            mSw.WriteLine("{0} {1} l b", p.X, Height - p.Y);
        }
    }

    List<Page> mPageList = new List<Page>();

    public void AddPage(Page page) {
        mPageList.Add(page);
    }

    public void Save(string path) {
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
        sw.WriteLine("      /BaseFont /Times-Roman");
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
