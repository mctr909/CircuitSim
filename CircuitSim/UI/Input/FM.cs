using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class FM : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 32;

        public FM(Point pos) : base(pos) {
            Elm = new ElmFM();
        }

        public FM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmFM(st);
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.FM; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmFM)Elm;
            optionList.Add(ce.CarrierFreq);
            optionList.Add(ce.Signalfreq);
            optionList.Add(ce.MaxVoltage);
            optionList.Add(ce.Deviation);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / Post.Len);
            interpPost(ref mNamePos, 1);
            ReferenceName = "FM";
            Post.SetBbox(SIZE);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmFM)Elm;
            drawLeadA();
            drawCenteredText(ReferenceName, mNamePos);
            drawWaveform(Elm.Post[1]);
            drawPosts();
            updateDotCount(-ce.Current, ref mCurCount);
            if (CirSimForm.DragElm != this) {
                drawCurrentA(mCurCount);
            }
        }

        void drawWaveform(Point p) {
            drawCircle(p, SIZE / 2);
            Post.AdjustBbox(p.X - SIZE, p.Y - SIZE, p.X + SIZE, p.Y + SIZE);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmFM)Elm;
            arr[0] = "FM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.GetVoltageDiff());
            arr[3] = "cf = " + Utils.UnitText(ce.CarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(ce.Signalfreq, "Hz");
            arr[5] = "dev =" + Utils.UnitText(ce.Deviation, "Hz");
            arr[6] = "Vmax = " + Utils.VoltageText(ce.MaxVoltage);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmFM)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("振幅", ce.MaxVoltage);
            }
            if (r == 1) {
                return new ElementInfo("搬送波周波数", ce.CarrierFreq);
            }
            if (r == 2) {
                return new ElementInfo("信号周波数", ce.Signalfreq);
            }
            if (r == 3) {
                return new ElementInfo("周波数偏移(Hz)", ce.Deviation);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmFM)Elm;
            if (n == 0) {
                ce.MaxVoltage = ei.Value;
            }
            if (n == 1) {
                ce.CarrierFreq = ei.Value;
            }
            if (n == 2) {
                ce.Signalfreq = ei.Value;
            }
            if (n == 3) {
                ce.Deviation = ei.Value;
            }
        }
    }
}
