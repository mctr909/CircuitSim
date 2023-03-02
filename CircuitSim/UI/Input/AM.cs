using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class AM : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 28;

        public AM(Point pos) : base(pos) {
            Elm = new ElmAM();
        }

        public AM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmAM(st);
            if ((DumpInfo.Flags & FLAG_COS) != 0) {
                DumpInfo.Flags &= ~FLAG_COS;
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
            setLead1(1 - 0.5 * SIZE / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmAM)Elm;
            setBbox(SIZE);
            drawLeadA();

            string s = "AM";
            drawCenteredText(s, DumpInfo.P2X, DumpInfo.P2Y, true);
            drawWaveform(g, Elm.Post[1]);
            drawPosts();
            CurCount = updateDotCount(-ce.Current, CurCount);
            if (CirSimForm.DragElm != this) {
                drawDotsA(CurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmAM)Elm;
            arr[0] = "AM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.VoltageDiff);
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
                return new ElementInfo("振幅(V)", ce.MaxVoltage);
            }
            if (r == 1) {
                return new ElementInfo("搬送波周波数(Hz)", ce.CarrierFreq);
            }
            if (r == 2) {
                return new ElementInfo("信号周波数(Hz)", ce.SignalFreq);
            }
            if (r == 3) {
                return new ElementInfo("変調度(%)", (int)(ce.Depth * 100));
            }
            if (r == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((ce.Phase * 180 / Math.PI).ToString("0.00")));
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

        void drawWaveform(CustomGraphics g, Point p) {
            drawWaveform(g, p.X, p.Y);
        }

        void drawWaveform(CustomGraphics g, int x, int y) {
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.DrawCircle(x, y, SIZE / 2);
            DumpInfo.AdjustBbox(x - SIZE, y - SIZE, x + SIZE, y + SIZE);
        }
    }
}
