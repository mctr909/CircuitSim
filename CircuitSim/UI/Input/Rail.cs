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
            Link.Load(st);
        }

        protected override int mNumHandles { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL; } }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(BODY_LEN);
            setLead1(1 - BODY_LEN / Post.Len);
        }

        public string getRailText() {
            return null;
        }

        public override void Draw(CustomGraphics g) {
            var elm = (ElmVoltage)Elm;
            var rt = getRailText();
            double w = rt == null ? (BODY_LEN * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > Post.Len * 0.8) {
                w = Post.Len * 0.8;
            }
            if (elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                && (mFlags & FLAG_CLOCK) != 0 || elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                setLead1(1 - (w - 5) / Post.Len);
            } else {
                setLead1(1 - w / Post.Len);
            }
            
            drawLeadA();
            drawRail();
            drawPosts();
            updateDotCount(-Elm.Current, ref mCurCount);
            if (CirSimForm.DragElm != this) {
                drawCurrentA(mCurCount);
            }
        }

        void drawRail() {
            var elm = (ElmVoltage)Elm;
            if (elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", Post.B, true);
            } else if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                var color = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
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
                drawCenteredText(s, Post.B, true);
            } else {
                drawWaveform(Elm.Post[1]);
            }
        }
    }
}
