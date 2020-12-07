using System;
using System.Drawing;

namespace Circuit.Elements {
    class RailElm : VoltageElm {
        protected const int FLAG_CLOCK = 1;

        public RailElm(int xx, int yy) : base(xx, yy, WF_DC) {
            mNumHandles = 1;
        }

        public RailElm(int xx, int yy, int wf) : base(xx, yy, wf) { }

        public RailElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st): base(xa, ya, xb, yb, f, st) {
            mNumHandles = 1;
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        protected override DUMP_ID getDumpType() { return DUMP_ID.RAIL; }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, 1 - circleSize / mLen);
        }

        public string getRailText() {
            return null;
        }

        public override void Draw(CustomGraphics g) {
            var rt = getRailText();
            double w = rt == null ? (circleSize * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > mLen * .8) {
                w = mLen * .8;
            }
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, 1 - w / mLen);
            setBbox(mPoint1, mPoint2, circleSize);

            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            drawRail(g);
            drawPosts(g);
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            if (waveform == WF_SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText(g, "CLK", X2, Y2, true);
            } else if (waveform == WF_DC || waveform == WF_VAR) {
                var color = NeedsHighlight ? SelectColor : WhiteColor;
                double v = getVoltage();
                string s;
                if (Math.Abs(v) < 1) {
                    s = v.ToString("0.000") + " V";
                } else {
                    s = Utils.ShortUnitText(v, "V");
                }
                if (getVoltage() > 0) {
                    s = "+" + s;
                }
                drawCenteredText(g, s, X2, Y2, true);
            } else {
                drawWaveform(g, mPoint2);
            }
        }

        public override void Stamp() {
            if (waveform == WF_DC) {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
            }
        }

        public override void DoStep() {
            if (waveform != WF_DC) {
                mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            }
        }

        public override bool HasGroundConnection(int n1) { return true; }
    }
}
