using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class DataRecorder : BaseUI {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        string mName = "";

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
        }

        public override void Draw(CustomGraphics g) {
            var str = "export";

            interpPoint(ref mLead1, 1 - ((int)g.GetTextSize(str).Width / 2) / mLen);
            setBbox(mPost1, mLead1, 0);

            drawCenteredText(str, DumpInfo.P2, true);
            drawLead(mPost1, mLead1);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmDataRecorder)Elm;
            arr[0] = "data export";
            arr[1] = "V = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = (ce.DataFull ? ce.DataCount : ce.DataPtr) + "/" + ce.DataCount;
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmDataRecorder)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("サンプル数", ce.DataCount).SetDimensionless();
            }
            if (r == 1) {
                return new ElementInfo("列名", mName);
            }
            if (r == 2) {
                return new ElementInfo("ファイルに保存", new System.EventHandler((s, e) => {
                    saveFileDialog.Filter = "CSVファイル(*.csv)|*.csv";
                    saveFileDialog.ShowDialog();
                    var filePath = saveFileDialog.FileName;
                    var fs = new StreamWriter(filePath);
                    fs.WriteLine(mName + "," + ControlPanel.TimeStep);
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
                mName = ei.Textf.Text;
            }
            if (n == 2) {
                return;
            }
        }
    }
}
