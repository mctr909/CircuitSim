using System.Drawing;

namespace Circuit.Elements.Passive {
    class PolarCapacitorUI : CapacitorUI {
        Point mPlusPoint;

        public PolarCapacitorUI(Point pos) : base(pos) {
            CirElm = new PolarCapacitorElm();
        }

        public PolarCapacitorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new PolarCapacitorElm(st);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR_POLAR; } }

        protected override string dump() {
            return ((PolarCapacitorElm)CirElm).MaxNegativeVoltage + "";
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 4) / mLen;
            if (P2.Y > P1.Y) {
                mPlusPoint.Y += 4;
            }
            if (P1.Y > P2.Y) {
                mPlusPoint.Y += 3;
            }
            if (P1.Y == P2.Y) {
                interpPoint(ref mPlusPoint, f - 5 / mLen, 5 * mDsign);
            } else {
                interpPoint(ref mPlusPoint, f - 5 / mLen, -5 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g);
            int w = (int)g.GetTextSize("+").Width;
            g.DrawLeftText("+", mPlusPoint.X - w / 2, mPlusPoint.Y);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "capacitor (polarized)";
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                return new ElementInfo("耐逆電圧(V)", ((PolarCapacitorElm)CirElm).MaxNegativeVoltage, 0, 0);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2 && ei.Value >= 0) {
                ((PolarCapacitorElm)CirElm).MaxNegativeVoltage = ei.Value;
            }
            base.SetElementValue(n, ei);
        }
    }
}
