using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Input {
    class AMUI : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 28;

        public AMUI(Point pos) : base(pos) {
            Elm = new AMElm();
        }

        public AMUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new AMElm(st);
            if ((DumpInfo.Flags & FLAG_COS) != 0) {
                DumpInfo.Flags &= ~FLAG_COS;
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AM; } }

        protected override void dump(List<object> optionList) {
            var ce = (AMElm)Elm;
            optionList.Add(ce.CarrierFreq);
            optionList.Add(ce.SignalFreq);
            optionList.Add(ce.MaxVoltage);
            optionList.Add(ce.Phase);
            optionList.Add(ce.Depth);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (AMElm)Elm;
            setBbox(mPost1, mPost2, SIZE);
            drawLead(mPost1, mLead1);

            string s = "AM";
            drawCenteredText(s, DumpInfo.P2, true);
            drawWaveform(g, mPost2);
            drawPosts();
            ce.CurCount = updateDotCount(-ce.Current, ce.CurCount);
            if (CirSimForm.Sim.DragElm != this) {
                drawDots(mPost1, mLead1, ce.CurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (AMElm)Elm;
            arr[0] = "AM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.VoltageDiff);
            arr[3] = "cf = " + Utils.UnitText(ce.CarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(ce.SignalFreq, "Hz");
            arr[5] = "Vmax = " + Utils.VoltageText(ce.MaxVoltage);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (AMElm)Elm;
            if (n == 0) {
                return new ElementInfo("振幅(V)", ce.MaxVoltage, -20, 20);
            }
            if (n == 1) {
                return new ElementInfo("搬送波周波数(Hz)", ce.CarrierFreq, 4, 500);
            }
            if (n == 2) {
                return new ElementInfo("信号周波数(Hz)", ce.SignalFreq, 4, 500);
            }
            if (n == 3) {
                return new ElementInfo("変調度(%)", (int)(ce.Depth * 100), 0, 100);
            }
            if (n == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((ce.Phase * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (AMElm)Elm;
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

        void drawWaveform(CustomGraphics g, Point center) {
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            int xc = center.X;
            int yc = center.Y;
            g.DrawCircle(center, SIZE / 2);
            DumpInfo.AdjustBbox(xc - SIZE, yc - SIZE, xc + SIZE, yc + SIZE);
        }
    }
}
