using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class DataRecorderUI : BaseUI {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        string mName = "";

        public DataRecorderUI(Point pos) : base(pos) {
            Elm = new DataRecorderElm();
        }

        public DataRecorderUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            Elm = new DataRecorderElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.DATA_RECORDER; } }

        protected override string dump() {
            var ce = (DataRecorderElm)Elm;
            return ce.DataCount.ToString();
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
            var ce = (DataRecorderElm)Elm;
            arr[0] = "data export";
            arr[1] = "V = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = (ce.DataFull ? ce.DataCount : ce.DataPtr) + "/" + ce.DataCount;
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (DataRecorderElm)Elm;
            if (n == 0) {
                return new ElementInfo("サンプル数", ce.DataCount, -1, -1).SetDimensionless();
            }
            if (n == 1) {
                var ei = new ElementInfo("列名", 0, -1, -1);
                ei.Text = mName;
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() {
                    Text = "ファイルに保存"
                };
                ei.Button.Click += new System.EventHandler((s, e) => {
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
                });
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (DataRecorderElm)Elm;
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
