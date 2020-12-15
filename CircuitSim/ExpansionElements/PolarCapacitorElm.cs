using System.Drawing;

namespace Circuit.Elements {
    class PolarCapacitorElm : CapacitorElm {
        double maxNegativeVoltage;
        Point plusPoint;

        public PolarCapacitorElm(int xx, int yy) : base(xx, yy) {
            maxNegativeVoltage = 1;
        }

        public PolarCapacitorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            maxNegativeVoltage = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR_POLAR; } }

        protected override string dump() {
            return maxNegativeVoltage + "";
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 4) / mLen;
            if (Y2 > Y1) {
                plusPoint.Y += 4;
            }
            if (Y1 > Y2) {
                plusPoint.Y += 3;
            }
            if (Y1 == Y2) {
                plusPoint = Utils.InterpPoint(mPoint1, mPoint2, f - 5 / mLen, 8 * mDsign);
            } else {
                plusPoint = Utils.InterpPoint(mPoint1, mPoint2, f - 5 / mLen, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g);
            g.TextColor = WhiteColor;
            int w = (int)g.GetTextSize("+").Width;
            g.DrawLeftText("+", plusPoint.X - w / 2, plusPoint.Y);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "capacitor (polarized)";
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 2) {
                return new EditInfo("Max Reverse Voltage", maxNegativeVoltage, 0, 0);
            }
            return base.GetEditInfo(n);
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 2 && ei.Value >= 0) {
                maxNegativeVoltage = ei.Value;
            }
            base.SetEditValue(n, ei);
        }

        public override void StepFinished() {
            if (VoltageDiff < 0 && VoltageDiff < -maxNegativeVoltage) {
                mCir.Stop("capacitor exceeded max reverse voltage", this);
            }
        }
    }
}
