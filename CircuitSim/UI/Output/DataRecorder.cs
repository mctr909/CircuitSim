using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class DataRecorder : BaseUI {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        string mColName = "";
        PointF[] mTextPoly;

        public DataRecorder(Point pos) : base(pos) {
            Elm = new ElmDataRecorder();
        }

        public DataRecorder(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            Elm = new ElmDataRecorder(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.DATA_RECORDER; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmDataRecorder)Elm;
            optionList.Add(ce.DataCount);
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
            setTextPos();
        }

        void setTextPos() {
            if (string.IsNullOrWhiteSpace(mColName)) {
                ReferenceName = "CSV";
            } else {
                ReferenceName = "CSV " + mColName;
            }
            var txtW = Context.GetTextSize(ReferenceName).Width;
            var txtH = Context.GetTextSize(ReferenceName).Height;
            var pw = txtW / Post.Len;
            var ph = 0.5 * (txtH - 1);
            setLead1(1);
            Post.SetBbox(Post.A, Post.B, txtH);
            var p1 = new PointF();
            var p2 = new PointF();
            var p3 = new PointF();
            var p4 = new PointF();
            var p5 = new PointF();
            interpPost(ref p1, 1, -ph);
            interpPost(ref p2, 1, ph);
            interpPost(ref p3, 1 + pw, ph);
            interpPost(ref p4, 1 + pw + ph / Post.Len, 0);
            interpPost(ref p5, 1 + pw, -ph);
            mTextPoly = new PointF[] {
                p1, p2, p3, p4, p5, p1
            };
            var abX = Post.B.X - Post.A.X;
            var abY = Post.B.Y - Post.A.Y;
            mTextRot = Math.Atan2(abY, abX);
            var deg = -mTextRot * 180 / Math.PI;
            if (deg < 0.0) {
                deg += 360;
            }
            if (45 * 3 <= deg && deg < 45 * 7) {
                mTextRot += Math.PI;
                interpPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
            } else {
                interpPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
            }
        }

        public override void Draw(CustomGraphics g) {
            drawLeadA();
            drawCenteredText(ReferenceName, mNamePos, mTextRot);
            drawPolyline(mTextPoly);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmDataRecorder)Elm;
            arr[0] = ReferenceName;
            arr[1] = "電位：" + Utils.VoltageText(ce.Volts[0]);
            arr[2] = (ce.DataFull ? ce.DataCount : ce.DataPtr) + "/" + ce.DataCount;
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmDataRecorder)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("サンプル数", ce.DataCount);
            }
            if (r == 1) {
                return new ElementInfo("列名", mColName);
            }
            if (r == 2) {
                return new ElementInfo("ファイルに保存", new System.EventHandler((s, e) => {
                    saveFileDialog.Filter = "CSVファイル(*.csv)|*.csv";
                    saveFileDialog.ShowDialog();
                    var filePath = saveFileDialog.FileName;
                    var fs = new StreamWriter(filePath);
                    fs.WriteLine(mColName + "," + ControlPanel.TimeStep);
                    if (ce.DataFull) {
                        for (int i = 0; i != ce.DataCount; i++) {
                            fs.WriteLine(ce.Data[(i + ce.DataPtr) % ce.DataCount]);
                        }
                    } else {
                        for (int i = 0; i != ce.DataPtr; i++) {
                            fs.WriteLine(ce.Data[i]);
                        }
                    }
                    fs.Close();
                    fs.Dispose();
                }));
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmDataRecorder)Elm;
            if (n == 0 && 0 < ei.Value) {
                ce.setDataCount((int)ei.Value);
            }
            if (n == 1) {
                mColName = ei.Text;
                setTextPos();
            }
            if (n == 2) {
                return;
            }
        }
    }
}
