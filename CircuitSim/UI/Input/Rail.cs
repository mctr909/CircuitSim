using System;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Rail : Voltage {
        protected const int FLAG_CLOCK = 1;

        PointF mC;
        PointF mLa;
        PointF mLb;

        public Rail(Point pos, ElmVoltage.WAVEFORM wf) : base(pos, wf) {
            Elm = new ElmRail(wf);
        }

        public Rail(Point p1, Point p2, int f, StringTokenizer st): base(p1, p2, f) {
            Elm = new ElmRail(st);
            Link.Load(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL; } }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(BODY_LEN);
            interpPost(ref mNamePos, 1 + 0.35 * BODY_LEN / Post.Len);
            interpPost(ref mC, 1);
            interpPost(ref mLa, 1, -5);
            interpPost(ref mLb, 1, 5);
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
                setLead1(1);
            } else {
                setLead1(1 - w / Post.Len);
            }
            
            drawLeadA();
            drawRail();
            updateDotCount(-Elm.Current, ref mCurCount);
            if (CirSimForm.ConstructElm != this) {
                drawCurrentA(mCurCount);
            }
        }

        void drawRail() {
            var elm = (ElmVoltage)Elm;
            if (elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", mNamePos);
            } else if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                drawLine(mLa, mLb);
                drawCircle(mC, 3);
                var v = elm.GetVoltage();
                var s = Utils.VoltageText(v);
                drawCenteredText(s, mNamePos);
            } else {
                drawWaveform(Post.B);
            }
        }
    }
}
