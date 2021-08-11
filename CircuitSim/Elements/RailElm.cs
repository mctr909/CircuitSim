using System;
using System.Drawing;

namespace Circuit.Elements {
    class RailElm : VoltageElm {
        protected const int FLAG_CLOCK = 1;

        public RailElm(Point pos) : base(pos, WAVEFORM.DC) {
            mNumHandles = 1;
        }

        public RailElm(Point pos, WAVEFORM wf) : base(pos, wf) { }

        public RailElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st): base(xa, ya, xb, yb, f, st) {
            mNumHandles = 1;
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL; } }

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
            if (waveform == WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0 || waveform == WAVEFORM.DC) {
                mLead1 = Utils.InterpPoint(mPoint1, mPoint2, 1 - (w - 5) / mLen);
            } else {
                mLead1 = Utils.InterpPoint(mPoint1, mPoint2, 1 - w / mLen);
            }
            setBbox(mPoint1, mPoint2, circleSize);

            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            drawRail(g);
            drawPosts(g);
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (Sim.DragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            if (waveform == WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText(g, "CLK", P2.X, P2.Y, true);
            } else if (waveform == WAVEFORM.DC) {
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
                drawCenteredText(g, s, P2.X, P2.Y, true);
            } else {
                drawWaveform(g, mPoint2);
            }
        }

        public override void Stamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
            }
        }

        public override void DoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
            }
        }

        public override bool HasGroundConnection(int n1) { return true; }
    }
}
