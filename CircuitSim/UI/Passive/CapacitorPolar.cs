using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class CapacitorPolar : Capacitor {
        Point mPlusPoint;

        public CapacitorPolar(Point pos) : base(pos) {
            Elm = new ElmPolarCapacitor();
        }

        public CapacitorPolar(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmPolarCapacitor();
            Elm = elm;
            elm.Capacitance = st.nextTokenDouble();
            elm.VoltDiff = st.nextTokenDouble();
            elm.MaxNegativeVoltage = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR_POLAR; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(((ElmPolarCapacitor)Elm).MaxNegativeVoltage);
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 4) / mLen;
            if (DumpInfo.P2.Y > DumpInfo.P1.Y) {
                mPlusPoint.Y += 4;
            }
            if (DumpInfo.P1.Y > DumpInfo.P2.Y) {
                mPlusPoint.Y += 3;
            }
            if (DumpInfo.P1.Y == DumpInfo.P2.Y) {
                interpPost(ref mPlusPoint, f - 5 / mLen, 5 * mDsign);
            } else {
                interpPost(ref mPlusPoint, f - 5 / mLen, -5 * mDsign);
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

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 2) {
                return new ElementInfo("耐逆電圧(V)", ((ElmPolarCapacitor)Elm).MaxNegativeVoltage);
            }
            return base.GetElementInfo(r, c);
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 2 && ei.Value >= 0) {
                ((ElmPolarCapacitor)Elm).MaxNegativeVoltage = ei.Value;
            }
            base.SetElementValue(n, c, ei);
        }
    }
}
