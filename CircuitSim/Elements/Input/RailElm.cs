using System;
using System.Drawing;

namespace Circuit.Elements.Input {
    class RailElm : VoltageElm {
        protected const int FLAG_CLOCK = 1;

        public RailElm(Point pos) : base(pos, WAVEFORM.DC) { }

        public RailElm(Point pos, WAVEFORM wf) : base(pos, wf) { }

        public RailElm(Point p1, Point p2, int f, StringTokenizer st): base(p1, p2, f, st) { }

        protected override int NumHandles { get { return 1; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirPostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - BODY_LEN / mLen);
        }

        public string getRailText() {
            return null;
        }

        public override void CirStamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
            }
        }

        public override void Draw(CustomGraphics g) {
            var rt = getRailText();
            double w = rt == null ? (BODY_LEN * 0.5) : g.GetTextSize(rt).Width / 2;
            if (w > mLen * .8) {
                w = mLen * .8;
            }
            if (waveform == WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0 || waveform == WAVEFORM.DC) {
                setLead1(1 - (w - 5) / mLen);
            } else {
                setLead1(1 - w / mLen);
            }
            setBbox(mPoint1, mPoint2, BODY_LEN);

            drawLead(mPoint1, mLead1);
            drawRail(g);
            drawPosts();
            mCirCurCount = cirUpdateDotCount(-mCirCurrent, mCirCurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCirCurCount);
            }
        }

        void drawRail(CustomGraphics g) {
            if (waveform == WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
                drawCenteredText("CLK", P2, true);
            } else if (waveform == WAVEFORM.DC) {
                var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
                double v = getVoltage();
                string s;
                if (Math.Abs(v) < 1) {
                    s = v.ToString("0.000") + " V";
                } else {
                    s = Utils.UnitText(v, "V");
                }
                if (getVoltage() > 0) {
                    s = "+" + s;
                }
                drawCenteredText(s, P2, true);
            } else {
                drawWaveform(g, mPoint2);
            }
        }
    }
}
