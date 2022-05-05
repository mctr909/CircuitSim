using System;
using System.Drawing;

namespace Circuit.Elements.Input {
    class RailElm : VoltageElm {
        protected const int FLAG_CLOCK = 1;

        public RailElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.DC) {
            CirElm = new RailElmE(VoltageElmE.WAVEFORM.DC);
        }

        public RailElm(Point pos, VoltageElmE.WAVEFORM wf) : base(pos, wf) {
            CirElm = new RailElmE(wf);
        }

        public RailElm(Point p1, Point p2, int f, StringTokenizer st): base(p1, p2, f) {
            CirElm = new RailElmE(st);
        }

        protected override int NumHandles { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL; } }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - BODY_LEN / mLen);
        }

        public string getRailText() {
            return null;
        }

        public override void Draw(CustomGraphics g) {
            var elm = (VoltageElmE)CirElm;
            var rt = getRailText();
            double w = rt == null ? (BODY_LEN * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > mLen * 0.8) {
                w = mLen * 0.8;
            }
            if (elm.waveform == VoltageElmE.WAVEFORM.SQUARE
                && (mFlags & FLAG_CLOCK) != 0 || elm.waveform == VoltageElmE.WAVEFORM.DC) {
                setLead1(1 - (w - 5) / mLen);
            } else {
                setLead1(1 - w / mLen);
            }
            setBbox(mPoint1, mPoint2, BODY_LEN);

            drawLead(mPoint1, mLead1);
            drawRail(g);
            drawPosts();
            CirElm.CurCount = CirElm.cirUpdateDotCount(-CirElm.mCurrent, CirElm.CurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, CirElm.CurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            var elm = (VoltageElmE)CirElm;
            if (elm.waveform == VoltageElmE.WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", P2, true);
            } else if (elm.waveform == VoltageElmE.WAVEFORM.DC) {
                var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
                double v = elm.getVoltage();
                string s;
                if (Math.Abs(v) < 1) {
                    s = v.ToString("0.000") + " V";
                } else {
                    s = Utils.UnitText(v, "V");
                }
                if (elm.getVoltage() > 0) {
                    s = "+" + s;
                }
                drawCenteredText(s, P2, true);
            } else {
                drawWaveform(g, mPoint2);
            }
        }
    }
}
