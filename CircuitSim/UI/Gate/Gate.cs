using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    abstract class Gate : BaseUI {
        const int FLAG_SMALL = 1;
        const int FLAG_SCHMITT = 2;

        const int G_WIDTH = 7;
        const int G_WIDTH2 = 14;
        const int G_HEIGHT = 8;
        const int CIRCLE_SIZE = 3;

        static bool mLastSchmitt = false;

        protected int mHs2;
        protected int mWw;

        protected Point[] mGatePolyEuro;
        protected Point[] mGatePolyAnsi;

        protected Point mCirclePos;
        protected Point[] mLinePoints;

        Point[] mSchmittPoly;
        Point[] mInPosts;
        Point[] mInGates;

        protected virtual string gateText { get { return null; } }

        protected virtual string gateName { get { return ""; } }

        public Gate(Point pos) : base(pos) {
            mNoDiagonal = true;
            if (mLastSchmitt) {
                DumpInfo.Flags |= FLAG_SCHMITT;
            }
            DumpInfo.Flags |= FLAG_SMALL;
        }

        public Gate(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = true;
            DumpInfo.Flags |= FLAG_SMALL;
        }

        public static bool UseAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

        protected override void dump(List<object> optionList) {
            var ce = (ElmGate)Elm;
            optionList.Add(ce.InputCount);
            optionList.Add(ce.Volts[ce.InputCount]);
            optionList.Add(ce.HighVoltage);
        }

        public override Point GetPost(int n) {
            var ce = (ElmGate)Elm;
            if (n == ce.InputCount) {
                return mPost2;
            }
            return mInPosts[n];
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmGate)Elm;
            ce.InputStates = new bool[ce.InputCount];
            int hs = G_HEIGHT;
            int i;
            mWw = G_WIDTH2;
            if (mWw > mLen / 2) {
                mWw = (int)(mLen / 2);
            }
            if (ce.IsInverting && mWw + 8 > mLen / 2) {
                mWw = (int)(mLen / 2 - 8);
            }
            calcLeads(mWw * 2);
            mInPosts = new Point[ce.InputCount];
            mInGates = new Point[ce.InputCount];
            ce.AllocNodes();
            int i0 = -ce.InputCount / 2;
            for (i = 0; i != ce.InputCount; i++, i0++) {
                if (i0 == 0 && (ce.InputCount & 1) == 0) {
                    i0++;
                }
                interpPoint(ref mInPosts[i], 0, hs * i0);
                interpLead(ref mInGates[i], 0, hs * i0);
                ce.Volts[i] = (ce.LastOutput ^ ce.IsInverting) ? 5 : 0;
            }
            mHs2 = G_WIDTH * (ce.InputCount / 2 + 1);
            setBbox(mPost1, mPost2, mHs2);
            if (ce.HasSchmittInputs) {
                Utils.CreateSchmitt(mLead1, mLead2, out mSchmittPoly, 1, .47f);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmGate)Elm;
            for (int i = 0; i != ce.InputCount; i++) {
                drawLead(mInPosts[i], mInGates[i]);
            }
            drawLead(mLead2, mPost2);
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            if (UseAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                var center = new Point();
                interpPoint(ref center, 0.5);
                drawCenteredLText(gateText, center, true);
            }
            if (ce.HasSchmittInputs) {
                g.DrawColor = CustomGraphics.WhiteColor;
                g.DrawPolygon(mSchmittPoly);
            }
            if (mLinePoints != null && UseAnsiGates()) {
                for (int i = 0; i != mLinePoints.Length - 1; i++) {
                    drawLead(mLinePoints[i], mLinePoints[i + 1]);
                }
            }
            if (ce.IsInverting) {
                g.DrawCircle(mCirclePos, CIRCLE_SIZE);
            }
            CurCount = updateDotCount(ce.Current, CurCount);
            drawDots(mLead2, mPost2, CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmGate)Elm;
            arr[0] = gateName;
            arr[1] = "Vout = " + Utils.VoltageText(ce.Volts[ce.InputCount]);
            arr[2] = "Iout = " + Utils.CurrentText(ce.Current);
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
                return new ElementInfo("閾値(V)", ce.HighVoltage);
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
                    DumpInfo.Flags |= FLAG_SCHMITT;
                } else {
                    DumpInfo.Flags &= ~FLAG_SCHMITT;
                }
                mLastSchmitt = ce.HasSchmittInputs = 0 != (DumpInfo.Flags & FLAG_SCHMITT);
                SetPoints();
            }
        }

        protected void createEuroGatePolygon() {
            mGatePolyEuro = new Point[4];
            interpLeadAB(ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, mHs2);
            interpLeadAB(ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, mHs2);
        }
    }
}
