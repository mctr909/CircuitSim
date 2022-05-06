using System;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorUI : BaseUI {
        const int BODY_LEN = 24;

        public InductorUI(Point pos) : base(pos) {
            CirElm = new InductorElm();
            ReferenceName = "L";
        }

        public InductorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                CirElm = new InductorElm(st);
                ReferenceName = st.nextToken();
            } catch (Exception ex) {
                throw new Exception("Inductor load error:{0}", ex);
            }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override string dump() {
            var ce = (InductorElm)CirElm;
            return ce.Inductance + " " + ce.Current + " " + ReferenceName;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            var ce = (InductorElm)CirElm;
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.UnitText(ce.Inductance, "")).Width * 0.5;
                var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
                interpPoint(ref mValuePos, 0.5 - wv / mLen * mDsign, -11 * mDsign);
                interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 10 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, mDsign);
                interpPoint(ref mNamePos, 0.5, -19 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InductorElm)CirElm;
            double v1 = CirElm.Volts[0];
            double v2 = CirElm.Volts[1];
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();
            drawCoil(mLead1, mLead2, v1, v2);

            drawValue(ce.Inductance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (InductorElm)CirElm;
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コイル" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(ce.Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(ce.Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InductorElm)CirElm;
            if (n == 0) {
                return new ElementInfo("インダクタンス(H)", ce.Inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (InductorElm)CirElm;
            if (n == 0 && ei.Value > 0) {
                ce.Inductance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            ce.Ind.Setup(ce.Inductance, CirElm.Current);
        }
    }
}
