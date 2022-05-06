﻿using System;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class CapacitorUI : BaseUI {
        public static readonly int FLAG_BACK_EULER = 2;

        const int BODY_LEN = 6;
        const int HS = 6;

        Point[] mPlate1;
        Point[] mPlate2;

        public CapacitorUI(Point pos) : base(pos) {
            CirElm = new CapacitorElm();
            ReferenceName = "C";
        }

        public CapacitorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                CirElm = new CapacitorElm(st);
                ReferenceName = st.nextToken();
            } catch (Exception ex) {
                throw new Exception("Capacitor load error:{0}", ex);
            }
        }
        
        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override string dump() {
            var ce = (CapacitorElm)CirElm;
            return ce.Capacitance + " " + ce.VoltDiff + " " + ReferenceName;
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen - BODY_LEN) * 0.5 / mLen;
            /* calc leads */
            setLead1(f);
            setLead2(1 - f);
            /* calc plates */
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpPointAB(ref mPlate1[0], ref mPlate1[1], f, HS);
            interpPointAB(ref mPlate2[0], ref mPlate2[1], 1 - f, HS);
            setTextPos();
        }

        void setTextPos() {
            var ce = (CapacitorElm)CirElm;
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.UnitText(ce.Capacitance, "")).Width * 0.5;
                var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
                interpPoint(ref mValuePos, 0.5 - wv / mLen * mDsign, -12 * mDsign);
                interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 11 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, 3 * mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (CapacitorElm)CirElm;
            setBbox(mPoint1, mPoint2, HS);

            /* draw first lead and plate */
            drawLead(mPoint1, mLead1);
            drawLead(mPlate1[0], mPlate1[1]);
            /* draw second lead and plate */
            drawLead(mPoint2, mLead2);
            drawLead(mPlate2[0], mPlate2[1]);

            updateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, ce.CurCount);
                drawDots(mPoint2, mLead2, -ce.CurCount);
            }
            drawPosts();

            drawValue(ce.Capacitance);
            drawName();
        }

        public override void GetInfo(string[] arr) {
            var ce = (CapacitorElm)CirElm;
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コンデンサ" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "C = " + Utils.UnitText(ce.Capacitance, "F");
            arr[4] = "P = " + Utils.UnitText(ce.Power, "W");
        }

        public override string GetScopeText(Scope.VAL v) {
            base.GetScopeText(v);
            var ce = (CapacitorElm)CirElm;
            return "capacitor, " + Utils.UnitText(ce.Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (CapacitorElm)CirElm;
            if (n == 0) {
                return new ElementInfo("静電容量(F)", ce.Capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (CapacitorElm)CirElm;
            if (n == 0 && ei.Value > 0) {
                ce.Capacitance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
        }
    }
}
