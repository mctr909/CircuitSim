using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class DataRecorderElm : CircuitElm {
        int dataCount;
        int dataPtr;
        double[] data;
        bool dataFull;

        SaveFileDialog saveFileDialog = new SaveFileDialog();

        public DataRecorderElm(Point pos) : base(pos) {
            setDataCount(10240);
        }

        public DataRecorderElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            setDataCount(int.Parse(st.nextToken()));
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.DATA_RECORDER; } }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        protected override string dump() {
            return dataCount.ToString();
        }

        public override void Reset() {
            dataPtr = 0;
            dataFull = false;
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Draw(CustomGraphics g) {
            var str = "export";

            interpPoint(ref mLead1, 1 - ((int)g.GetLTextSize(str).Width / 2 + 8) / mLen);
            setBbox(mPoint1, mLead1, 0);

            drawCenteredText(g, str, P2.X, P2.Y, true);
            drawVoltage(g, 0, mPoint1, mLead1);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "data export";
            arr[1] = "V = " + Utils.VoltageText(Volts[0]);
            arr[2] = (dataFull ? dataCount : dataPtr) + "/" + dataCount;
        }

        public override void StepFinished() {
            data[dataPtr++] = Volts[0];
            if (dataPtr >= dataCount) {
                dataPtr = 0;
                dataFull = true;
            }
        }

        void setDataCount(int ct) {
            dataCount = ct;
            data = new double[dataCount];
            dataPtr = 0;
            dataFull = false;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("# of Data Points", dataCount, -1, -1).SetDimensionless();
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() {
                    Text = "Save file"
                };
                ei.Button.Click += new System.EventHandler((s, e) => {
                    saveFileDialog.Filter = "CSVファイル(*.csv)|*.csv";
                    saveFileDialog.ShowDialog();
                    var filePath = saveFileDialog.FileName;
                    var fs = new StreamWriter(filePath);
                    fs.WriteLine("# time step = {0} sec", ControlPanel.TimeStep);
                    if (dataFull) {
                        for (int i = 0; i != dataCount; i++) {
                            fs.WriteLine(data[(i + dataPtr) % dataCount]);
                        }
                    } else {
                        for (int i = 0; i != dataPtr; i++) {
                            fs.WriteLine(data[i]);
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
            if (n == 0 && 0 < ei.Value) {
                setDataCount((int)ei.Value);
            }
            if (n == 1) {
                return;
            }
        }
    }
}
