using System.Drawing;

namespace Circuit.Elements.Input {
    class FMUI : BaseUI {
        const int FLAG_COS = 2;
        const int SIZE = 28;

        public FMUI(Point pos) : base(pos) {
            CirElm = new FMElm();
        }

        public FMUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new FMElm(st);
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.FM; } }

        protected override string dump() {
            var ce = (FMElm)CirElm;
            return ce.CarrierFreq + " " + ce.Signalfreq + " " + ce.MaxVoltage + " " + ce.Deviation;
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (FMElm)CirElm;
            setBbox(mPoint1, mPoint2, SIZE);
            drawLead(mPoint1, mLead1);

            string s = "FM";
            drawCenteredText(s, P2, true);
            drawWaveform(g, mPoint2);
            drawPosts();
            ce.CurCount = updateDotCount(-ce.Current, ce.CurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, ce.CurCount);
            }
        }

        void drawWaveform(CustomGraphics g, Point center) {
            int xc = center.X;
            int yc = center.Y;
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawCircle(center, SIZE / 2);
            adjustBbox(xc - SIZE, yc - SIZE, xc + SIZE, yc + SIZE);
        }

        public override void GetInfo(string[] arr) {
            var ce = (FMElm)CirElm;
            arr[0] = "FM Source";
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.VoltageDiff);
            arr[3] = "cf = " + Utils.UnitText(ce.CarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(ce.Signalfreq, "Hz");
            arr[5] = "dev =" + Utils.UnitText(ce.Deviation, "Hz");
            arr[6] = "Vmax = " + Utils.VoltageText(ce.MaxVoltage);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (FMElm)CirElm;
            if (n == 0) {
                return new ElementInfo("振幅(V)", ce.MaxVoltage, -20, 20);
            }
            if (n == 1) {
                return new ElementInfo("搬送波周波数(Hz)", ce.CarrierFreq, 4, 500);
            }
            if (n == 2) {
                return new ElementInfo("信号周波数(Hz)", ce.Signalfreq, 4, 500);
            }
            if (n == 3) {
                return new ElementInfo("周波数偏移(Hz)", ce.Deviation, 4, 500);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (FMElm)CirElm;
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
