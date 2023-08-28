using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    abstract class Gate : BaseUI {
        const int FLAG_SMALL = 1;
        const int FLAG_SCHMITT = 2;

        const int G_WIDTH = 6;
        const int G_WIDTH2 = 12;
        const int G_HEIGHT = 8;
        const int CIRCLE_SIZE = 3;

        static bool mLastSchmitt = false;

        protected int mHs2;
        protected int mWw;

        protected PointF[] mGatePolyEuro;
        protected PointF[] mGatePolyAnsi;

        protected PointF mCirclePos;
        protected PointF[] mLinePoints;

        PointF[] mSchmittPoly;
        PointF[] mInGates;

        protected virtual string gateText { get { return null; } }

        protected virtual string gateName { get { return ""; } }

        public Gate(Point pos) : base(pos) {
            mNoDiagonal = true;
            if (mLastSchmitt) {
                mFlags |= FLAG_SCHMITT;
            }
            mFlags |= FLAG_SMALL;
        }

        public Gate(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = true;
            mFlags |= FLAG_SMALL;
        }

        public static bool UseAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

        protected override void dump(List<object> optionList) {
            var ce = (ElmGate)Elm;
            optionList.Add(ce.InputCount);
            optionList.Add(ce.Volts[ce.InputCount]);
            optionList.Add(ce.HighVoltage);
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmGate)Elm;
            ce.InputStates = new bool[ce.InputCount];
            int hs = G_HEIGHT;
            int i;
            mWw = G_WIDTH2;
            if (mWw > Post.Len / 2) {
                mWw = (int)(Post.Len / 2);
            }
            if (ce.IsInverting && mWw + 8 > Post.Len / 2) {
                mWw = (int)(Post.Len / 2 - 8);
            }
            calcLeads(mWw * 2);
            ce.InPosts = new Point[ce.InputCount];
            mInGates = new PointF[ce.InputCount];
            ce.AllocNodes();
            int i0 = -ce.InputCount / 2;
            for (i = 0; i != ce.InputCount; i++, i0++) {
                if (i0 == 0 && (ce.InputCount & 1) == 0) {
                    i0++;
                }
                interpPost(ref ce.InPosts[i], 0, hs * i0);
                interpLead(ref mInGates[i], 0, hs * i0);
                ce.Volts[i] = (ce.LastOutput ^ ce.IsInverting) ? 5 : 0;
            }
            mHs2 = G_WIDTH * (ce.InputCount / 2 + 1);
            Post.SetBbox(mHs2);
            if (ce.HasSchmittInputs) {
                Utils.CreateSchmitt(mLead1, mLead2, out mSchmittPoly, 1, .47f);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmGate)Elm;
            for (int i = 0; i != ce.InputCount; i++) {
                drawLine(ce.InPosts[i], mInGates[i]);
            }
            drawLeadB();
            if (UseAnsiGates()) {
                drawPolygon(mGatePolyAnsi);
            } else {
                drawPolygon(mGatePolyEuro);
                var center = new PointF();
                interpPost(ref center, 0.5);
                drawCenteredLText(gateText, center, true);
            }
            if (ce.HasSchmittInputs) {
                drawPolygon(mSchmittPoly);
            }
            if (mLinePoints != null && UseAnsiGates()) {
                for (int i = 0; i != mLinePoints.Length - 1; i++) {
                    drawLine(mLinePoints[i], mLinePoints[i + 1]);
                }
            }
            if (ce.IsInverting) {
                drawCircle(mCirclePos, CIRCLE_SIZE);
            }
            updateDotCount(ce.Current, ref mCurCount);
            drawCurrentB(mCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmGate)Elm;
            arr[0] = gateName;
            arr[1] = "Vout：" + Utils.VoltageText(ce.Volts[ce.InputCount]);
            arr[2] = "Iout：" + Utils.CurrentText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmGate)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("入力数", ce.InputCount);
            }
            if (r == 1) {
                return new ElementInfo("High電圧", ce.HighVoltage);
            }
            if (r == 2) {
                return new ElementInfo("シュミットトリガー", ce.HasSchmittInputs);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmGate)Elm;
            if (n == 0 && ei.Value >= 1) {
                ce.InputCount = (int)ei.Value;
                SetPoints();
            }
            if (n == 1) {
                ce.HighVoltage = ElmGate.LastHighVoltage = ei.Value;
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SCHMITT;
                } else {
                    mFlags &= ~FLAG_SCHMITT;
                }
                mLastSchmitt = ce.HasSchmittInputs = 0 != (mFlags & FLAG_SCHMITT);
                SetPoints();
            }
        }

        protected void createEuroGatePolygon() {
            mGatePolyEuro = new PointF[4];
            interpLeadAB(ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, mHs2);
            interpLeadAB(ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, mHs2);
        }
    }
}
