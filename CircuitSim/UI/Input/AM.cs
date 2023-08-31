using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class AM : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 32;

        public AM(Point pos) : base(pos) {
            Elm = new ElmAM();
        }

        public AM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmAM(st);
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AM; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmAM)Elm;
            optionList.Add(ce.CarrierFreq);
            optionList.Add(ce.SignalFreq);
            optionList.Add(ce.MaxVoltage);
            optionList.Add(ce.Phase);
            optionList.Add(ce.Depth);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / Post.Len);
            interpPost(ref mNamePos, 1);
            ReferenceName = "AM";
            Post.SetBbox(SIZE);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmAM)Elm;
            drawLeadA();
            drawCenteredText(ReferenceName, mNamePos);
            drawWaveform(Post.B);
            updateDotCount(-ce.Current, ref mCurCount);
            if (CirSimForm.ConstructElm != this) {
                drawCurrentA(mCurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmAM)Elm;
            arr[0] = "AM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.GetVoltageDiff());
            arr[3] = "cf = " + Utils.UnitText(ce.CarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(ce.SignalFreq, "Hz");
            arr[5] = "Vmax = " + Utils.VoltageText(ce.MaxVoltage);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmAM)Elm;
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
                return new ElementInfo("信号周波数", ce.SignalFreq);
            }
            if (r == 3) {
                return new ElementInfo("変調度(%)", (int)(ce.Depth * 100));
            }
            if (r == 4) {
                return new ElementInfo("位相(deg)", double.Parse((ce.Phase * 180 / Math.PI).ToString("0.00")));
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmAM)Elm;
            if (n == 0) {
                ce.MaxVoltage = ei.Value;
            }
            if (n == 1) {
                ce.CarrierFreq = ei.Value;
            }
            if (n == 2) {
                ce.SignalFreq = ei.Value;
            }
            if (n == 3) {
                ce.Depth = ei.Value * 0.01;
            }
            if (n == 4) {
                ce.Phase = ei.Value * Math.PI / 180;
            }
        }

        void drawWaveform(Point p) {
            drawCircle(p, SIZE / 2);
            Post.AdjustBbox(p.X - SIZE, p.Y - SIZE, p.X + SIZE, p.Y + SIZE);
        }
    }
}
