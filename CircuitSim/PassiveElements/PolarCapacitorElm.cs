﻿using System.Drawing;

namespace Circuit.PassiveElements {
    class PolarCapacitorElm : CapacitorElm {
        double mMaxNegativeVoltage;
        Point mPlusPoint;

        public PolarCapacitorElm(Point pos) : base(pos) {
            mMaxNegativeVoltage = 1;
        }

        public PolarCapacitorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            mMaxNegativeVoltage = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR_POLAR; } }

        protected override string dump() {
            return mMaxNegativeVoltage + "";
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
                Utils.InterpPoint(mPoint1, mPoint2, ref mPlusPoint, f - 5 / mLen, 8 * mDsign);
            } else {
                Utils.InterpPoint(mPoint1, mPoint2, ref mPlusPoint, f - 5 / mLen, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g);
            g.TextColor = WhiteColor;
            int w = (int)g.GetTextSize("+").Width;
            g.DrawLeftText("+", mPlusPoint.X - w / 2, mPlusPoint.Y);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "capacitor (polarized)";
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                return new ElementInfo("Max Reverse Voltage", mMaxNegativeVoltage, 0, 0);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2 && ei.Value >= 0) {
                mMaxNegativeVoltage = ei.Value;
            }
            base.SetElementValue(n, ei);
        }

        public override void StepFinished() {
            if (VoltageDiff < 0 && VoltageDiff < -mMaxNegativeVoltage) {
                mCir.Stop("capacitor exceeded max reverse voltage", this);
            }
        }
    }
}
