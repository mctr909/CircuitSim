using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Capacitor : BaseUI {
        public static readonly int FLAG_BACK_EULER = 2;
        protected static string mLastReferenceName = "C";

        const int BODY_LEN = 5;
        const int HS = 6;

        PointF[] mPlate1;
        PointF[] mPlate2;

        public Capacitor(Point pos, int dummy) : base(pos) {
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public Capacitor(Point pos) : base(pos) {
            Elm = new ElmCapacitor();
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public Capacitor(Point p1, Point p2, int f) : base(p1, p2, f) {
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public Capacitor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmCapacitor();
            Elm = elm;
            try {
                elm.Capacitance = st.nextTokenDouble();
                elm.VoltDiff = st.nextTokenDouble();
            } catch (Exception ex) {
                throw new Exception("Capacitor load error:{0}", ex);
            }
        }
        
        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmCapacitor)Elm;
            optionList.Add(ce.Capacitance);
            optionList.Add(ce.VoltDiff.ToString("0.000000"));
        }

        public override void SetPoints() {
            base.SetPoints();
            var f1 = 0.5 - BODY_LEN * 0.5 / mLen;
            var f2 = 0.5 + BODY_LEN * 0.5 / mLen;
            var dw = 0.8 / mLen;
            /* calc leads */
            setLead1(f1 - 0.1 / mLen);
            setLead2(f2 + 0.1 / mLen);
            /* calc plates */
            mPlate1 = new PointF[4];
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate1[0], f1 - dw, -HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate1[1], f1 - dw, HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate1[2], f1 + dw, HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate1[3], f1 + dw, -HS);
            mPlate2 = new PointF[4];
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate2[0], f2 - dw, -HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate2[1], f2 - dw, HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate2[2], f2 + dw, HS);
            Utils.InterpPoint(Elm.Post[0], Elm.Post[1], ref mPlate2[3], f2 + dw, -HS);
            setTextPos();
        }

        void setTextPos() {
            if (mHorizontal) {
                interpPoint(ref mValuePos, 0.5, -11 * mDsign);
                interpPoint(ref mNamePos, 0.5, 12 * mDsign);
            } else if (mVertical) {
                interpPoint(ref mValuePos, 0.5, 3 * mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmCapacitor)Elm;

            setBbox(HS);
            draw2Leads();

            /* draw first lead and plate */
            fillPolygon(mPlate1);
            /* draw second lead and plate */
            fillPolygon(mPlate2);

            updateDotCount();
            if (CirSimForm.DragElm != this) {
                drawCurrentA(CurCount);
                drawCurrentB(CurCount);
            }
            drawPosts();

            drawValue(ce.Capacitance);
            drawName();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmCapacitor)Elm;
            arr[0] = string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コンデンサ" : DumpInfo.ReferenceName;
            getBasicInfo(arr);
            arr[3] = "C = " + Utils.UnitText(ce.Capacitance, "F");
        }

        public override string GetScopeText() {
            var ce = (ElmCapacitor)Elm;
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コンデンサ" : DumpInfo.ReferenceName) + " "
                + Utils.UnitText(ce.Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmCapacitor)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("キャパシタンス(F)", ce.Capacitance);
            }
            if (r == 1) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmCapacitor)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Capacitance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Text;
                mLastReferenceName = DumpInfo.ReferenceName;
                setTextPos();
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var ce = (ElmCapacitor)Elm;
            return new EventHandler((s, e) => {
                var trb = adj.Slider;
                ce.Capacitance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
