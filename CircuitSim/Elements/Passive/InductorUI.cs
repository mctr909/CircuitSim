using System;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorUI : BaseUI {
        protected static string mLastReferenceName = "L";

        const int BODY_LEN = 24;

        public InductorUI(Point pos) : base(pos) {
            Elm = new InductorElm();
            ReferenceName = mLastReferenceName;
        }

        public InductorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new InductorElm(st);
                ReferenceName = st.nextToken();
            } catch (Exception ex) {
                throw new Exception("Inductor load error:{0}", ex);
            }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override string dump() {
            var ce = (InductorElm)Elm;
            return ce.Inductance + " " + ce.Current + " " + ReferenceName;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            mNameV = mPost1.X == mPost2.X;
            mNameH = mPost1.Y == mPost2.Y;
            if (mNameH) {
                interpPoint(ref mValuePos, 0.5, -11 * mDsign);
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InductorElm)Elm;
            double v1 = Elm.Volts[0];
            double v2 = Elm.Volts[1];
            int hs = 8;
            setBbox(mPost1, mPost2, hs);

            draw2Leads();
            drawCoil(mLead1, mLead2, v1, v2);

            drawValue(ce.Inductance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (InductorElm)Elm;
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コイル" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(ce.Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(ce.Power, "W");
        }

        public override string GetScopeText() {
            var ce = (InductorElm)Elm;
            return (string.IsNullOrEmpty(ReferenceName) ? "コイル" : ReferenceName) + " "
                + Utils.UnitText(ce.Inductance, "H");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InductorElm)Elm;
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
            var ce = (InductorElm)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Inductance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                mLastReferenceName = ReferenceName;
                setTextPos();
            }
            ce.Setup(ce.Inductance, Elm.Current);
        }
    }
}
