using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class CapacitorUI : BaseUI {
        public static readonly int FLAG_BACK_EULER = 2;
        protected static string mLastReferenceName = "C";

        const int BODY_LEN = 5;
        const int HS = 6;

        Point[] mPlate1;
        Point[] mPlate2;

        public CapacitorUI(Point pos, int dummy) : base(pos) {
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public CapacitorUI(Point pos) : base(pos) {
            Elm = new CapacitorElm();
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public CapacitorUI(Point p1, Point p2, int f) : base(p1, p2, f) {
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public CapacitorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new CapacitorElm(st);
                DumpInfo.ReferenceName = st.nextToken();
            } catch (Exception ex) {
                throw new Exception("Capacitor load error:{0}", ex);
            }
        }
        
        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (CapacitorElm)Elm;
            optionList.Add(ce.Capacitance);
            optionList.Add(ce.VoltDiff.ToString("0.000000"));
        }

        public override void SetPoints() {
            base.SetPoints();
            var f1 = 0.5 - BODY_LEN * 0.5 / mLen;
            var f2 = 1.0 - f1;
            /* calc leads */
            setLead1(f1);
            setLead2(f2);
            /* calc plates */
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpPointAB(ref mPlate1[0], ref mPlate1[1], f1, HS);
            interpPointAB(ref mPlate2[0], ref mPlate2[1], f2, HS);
            setTextPos();
        }

        void setTextPos() {
            mNameV = mPost1.X == mPost2.X;
            mNameH = mPost1.Y == mPost2.Y;
            if (mNameH) {
                interpPoint(ref mValuePos, 0.5, -12 * mDsign);
                interpPoint(ref mNamePos, 0.5, 11 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, 3 * mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (CapacitorElm)Elm;
            setBbox(mPost1, mPost2, HS);

            /* draw first lead and plate */
            drawLead(mPost1, mLead1);
            drawLead(mPlate1[0], mPlate1[1]);
            /* draw second lead and plate */
            drawLead(mPost2, mLead2);
            drawLead(mPlate2[0], mPlate2[1]);

            updateDotCount();
            if (CirSimForm.Sim.DragElm != this) {
                drawDots(mPost1, mLead1, ce.CurCount);
                drawDots(mPost2, mLead2, -ce.CurCount);
            }
            drawPosts();

            drawValue(ce.Capacitance);
            drawName();
        }

        public override void GetInfo(string[] arr) {
            var ce = (CapacitorElm)Elm;
            arr[0] = string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コンデンサ" : DumpInfo.ReferenceName;
            getBasicInfo(arr);
            arr[3] = "C = " + Utils.UnitText(ce.Capacitance, "F");
            arr[4] = "P = " + Utils.UnitText(ce.Power, "W");
        }

        public override string GetScopeText() {
            var ce = (CapacitorElm)Elm;
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コンデンサ" : DumpInfo.ReferenceName) + " "
                + Utils.UnitText(ce.Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (CapacitorElm)Elm;
            if (n == 0) {
                return new ElementInfo("キャパシタンス(F)", ce.Capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = DumpInfo.ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (CapacitorElm)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Capacitance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Textf.Text;
                mLastReferenceName = DumpInfo.ReferenceName;
                setTextPos();
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var ce = (CapacitorElm)Elm;
            return new EventHandler((s, e) => {
                var trb = adj.Slider;
                ce.Capacitance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                CirSimForm.Sim.NeedAnalyze();
            });
        }
    }
}
