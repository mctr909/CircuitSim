using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class FM : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 28;

        public FM(Point pos) : base(pos) {
            Elm = new ElmFM();
        }

        public FM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmFM(st);
            if ((_Flags & FLAG_COS) != 0) {
                _Flags &= ~FLAG_COS;
            }
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.FM; } }

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
            interpPost(ref _NamePos, 1);
            ReferenceName = "FM";
            Post.SetBbox(SIZE);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmFM)Elm;
            drawLeadA();
            drawCircle(Post.B, SIZE / 2);
            drawCenteredText(ReferenceName, _NamePos);
            updateDotCount(-ce.Current, ref _CurCount);
            if (CirSimForm.ConstructElm != this) {
                drawCurrentA(_CurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmFM)Elm;
            arr[0] = "FM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.GetVoltageDiff());
            arr[3] = "cf = " + Utils.FrequencyText(ce.CarrierFreq);
            arr[4] = "sf = " + Utils.FrequencyText(ce.Signalfreq);
            arr[5] = "dev =" + Utils.FrequencyText(ce.Deviation);
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
