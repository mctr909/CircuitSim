using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Gate {
    abstract class GateUI : BaseUI {
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

        public GateUI(Point pos) : base(pos) {
            mNoDiagonal = true;
            if (mLastSchmitt) {
                mFlags |= FLAG_SCHMITT;
            }
            mFlags |= FLAG_SMALL;
        }

        public GateUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = true;
            mFlags |= FLAG_SMALL;
        }

        public static bool UseAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

        protected override string dump() {
            var ce = (GateElm)CirElm;
            return ce.InputCount + " " + ce.Volts[ce.InputCount] + " " + ce.HighVoltage;
        }

        public override Point GetPost(int n) {
            var ce = (GateElm)CirElm;
            if (n == ce.InputCount) {
                return mPost2;
            }
            return mInPosts[n];
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (GateElm)CirElm;
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
            var ce = (GateElm)CirElm;
            for (int i = 0; i != ce.InputCount; i++) {
                drawLead(mInPosts[i], mInGates[i]);
            }
            drawLead(mLead2, mPost2);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            if (UseAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                var center = new Point();
                interpPoint(ref center, 0.5);
                drawCenteredLText(gateText, center, true);
            }
            if (ce.HasSchmittInputs) {
                g.LineColor = CustomGraphics.WhiteColor;
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
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPost2, ce.CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (GateElm)CirElm;
            arr[0] = gateName;
            arr[1] = "Vout = " + Utils.VoltageText(ce.Volts[ce.InputCount]);
            arr[2] = "Iout = " + Utils.CurrentText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (GateElm)CirElm;
            if (n == 0) {
                return new ElementInfo("入力数", ce.InputCount, 1, 8).SetDimensionless();
            }
            if (n == 1) {
                return new ElementInfo("閾値(V)", ce.HighVoltage, 1, 10);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "シュミットトリガー",
                    Checked = ce.HasSchmittInputs
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (GateElm)CirElm;
            if (n == 0 && ei.Value >= 1) {
                ce.InputCount = (int)ei.Value;
                SetPoints();
            }
            if (n == 1) {
                ce.HighVoltage = GateElm.LastHighVoltage = ei.Value;
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
            mGatePolyEuro = new Point[4];
            interpLeadAB(ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, mHs2);
            interpLeadAB(ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, mHs2);
        }
    }
}
