using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class CapacitorPolar : Capacitor {
        PointF mPlusPoint;

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
            optionList.Add(((ElmPolarCapacitor)Elm).Capacitance.ToString("g3"));
            optionList.Add(((ElmPolarCapacitor)Elm).VoltDiff.ToString("g3"));
            optionList.Add(((ElmPolarCapacitor)Elm).MaxNegativeVoltage);
        }

        public override void SetPoints() {
            base.SetPoints();
            var f = (Post.Len / 2 - 4) / Post.Len;
            if (Post.A.Y == Post.B.Y) {
                interpPost(ref mPlusPoint, f - 5 / Post.Len, 5 * Post.Dsign);
            } else {
                interpPost(ref mPlusPoint, f - 5 / Post.Len, -5 * Post.Dsign);
            }
            if (Post.B.Y > Post.A.Y) {
                mPlusPoint.Y += 1;
            }
            if (Post.A.Y > Post.B.Y) {
                mPlusPoint.Y += 3;
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g);
            drawCenteredText("+", mPlusPoint);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (ElmCapacitor)Elm;
            arr[1] = "有極性コンデンサ：" + Utils.UnitText(ce.Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 2) {
                return new ElementInfo("耐逆電圧", ((ElmPolarCapacitor)Elm).MaxNegativeVoltage);
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
