using System;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Rail : Voltage {
        protected const int FLAG_CLOCK = 1;

        public Rail(Point pos, ElmVoltage.WAVEFORM wf) : base(pos, wf) {
            Elm = new ElmRail(wf);
        }

        public Rail(Point p1, Point p2, int f, StringTokenizer st): base(p1, p2, f) {
            Elm = new ElmRail(st);
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
            var elm = (ElmVoltage)Elm;
            var rt = getRailText();
            double w = rt == null ? (BODY_LEN * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > mLen * 0.8) {
                w = mLen * 0.8;
            }
            if (elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                && (DumpInfo.Flags & FLAG_CLOCK) != 0 || elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                setLead1(1 - (w - 5) / mLen);
            } else {
                setLead1(1 - w / mLen);
            }
            setBbox(BODY_LEN);

            drawLeadA();
            drawRail(g);
            drawPosts();
            CurCount = updateDotCount(-Elm.Current, CurCount);
            if (CirSimForm.DragElm != this) {
                drawDotsA(CurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            var elm = (ElmVoltage)Elm;
            if (elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE && (DumpInfo.Flags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", DumpInfo.P2.X, DumpInfo.P2.Y, true);
            } else if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
                double v = elm.GetVoltage();
                string s;
                if (Math.Abs(v) < 1) {
                    s = v.ToString("0.000") + " V";
                } else {
                    s = Utils.UnitText(v, "V");
                }
                if (elm.GetVoltage() > 0) {
                    s = "+" + s;
                }
                drawCenteredText(s, DumpInfo.P2.X, DumpInfo.P2.Y, true);
            } else {
                drawWaveform(g, Elm.Post[1]);
            }
        }
    }
}
