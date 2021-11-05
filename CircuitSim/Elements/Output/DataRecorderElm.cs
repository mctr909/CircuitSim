using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class DataRecorderElm : CircuitElm {
        double[] mData;
        int mDataCount;
        int mDataPtr;
        bool mDataFull;

        string mName = "";

        SaveFileDialog saveFileDialog = new SaveFileDialog();

        public DataRecorderElm(Point pos) : base(pos) {
            setDataCount(10000);
        }

        public DataRecorderElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            setDataCount(int.Parse(st.nextToken()));
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.DATA_RECORDER; } }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        protected override string dump() {
            return mDataCount.ToString();
        }

        public override void StepFinished() {
            mData[mDataPtr++] = Volts[0];
            if (mDataPtr >= mDataCount) {
                mDataPtr = 0;
                mDataFull = true;
            }
        }

        public override void Reset() {
            mDataPtr = 0;
            mDataFull = false;
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Draw(CustomGraphics g) {
            var str = "export";

            interpPoint(ref mLead1, 1 - ((int)g.GetTextSize(str).Width / 2) / mLen);
            setBbox(mPoint1, mLead1, 0);

            drawCenteredText(str, P2, true);
            drawLead(mPoint1, mLead1);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "data export";
            arr[1] = "V = " + Utils.VoltageText(Volts[0]);
            arr[2] = (mDataFull ? mDataCount : mDataPtr) + "/" + mDataCount;
        }

        void setDataCount(int ct) {
            mDataCount = ct;
            mDataPtr = 0;
            mDataFull = false;
            mData = new double[mDataCount];
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("サンプル数", mDataCount, -1, -1).SetDimensionless();
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
                    if (mDataFull) {
                        for (int i = 0; i != mDataCount; i++) {
                            fs.WriteLine(mData[(i + mDataPtr) % mDataCount]);
                        }
                    } else {
                        for (int i = 0; i != mDataPtr; i++) {
                            fs.WriteLine(mData[i]);
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
                mName = ei.Textf.Text;
            }
            if (n == 2) {
                return;
            }
        }
    }
}
