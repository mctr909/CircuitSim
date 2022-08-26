using System;
using System.Drawing;

namespace Circuit.Elements.Input {
    class RailUI : VoltageUI {
        protected const int FLAG_CLOCK = 1;

        public RailUI(Point pos) : base(pos, VoltageElm.WAVEFORM.DC) {
            Elm = new RailElm(VoltageElm.WAVEFORM.DC);
        }

        public RailUI(Point pos, VoltageElm.WAVEFORM wf) : base(pos, wf) {
            Elm = new RailElm(wf);
        }

        public RailUI(Point p1, Point p2, int f, StringTokenizer st): base(p1, p2, f) {
            Elm = new RailElm(st);
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
            var elm = (VoltageElm)Elm;
            var rt = getRailText();
            double w = rt == null ? (BODY_LEN * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > mLen * 0.8) {
                w = mLen * 0.8;
            }
            if (elm.waveform == VoltageElm.WAVEFORM.SQUARE
                && (DumpInfo.Flags & FLAG_CLOCK) != 0 || elm.waveform == VoltageElm.WAVEFORM.DC) {
                setLead1(1 - (w - 5) / mLen);
            } else {
                setLead1(1 - w / mLen);
            }
            setBbox(mPost1, mPost2, BODY_LEN);

            drawLead(mPost1, mLead1);
            drawRail(g);
            drawPosts();
            Elm.CurCount = updateDotCount(-Elm.Current, Elm.CurCount);
            if (CirSimForm.Sim.DragElm != this) {
                drawDots(mPost1, mLead1, Elm.CurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            var elm = (VoltageElm)Elm;
            if (elm.waveform == VoltageElm.WAVEFORM.SQUARE && (DumpInfo.Flags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", DumpInfo.P2, true);
            } else if (elm.waveform == VoltageElm.WAVEFORM.DC) {
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
                drawCenteredText(s, DumpInfo.P2, true);
            } else {
                drawWaveform(g, mPost2);
            }
        }
    }
}
