using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorElm : CircuitElm {
        const int BODY_LEN = 24;

        public InductorElm(Point pos) : base(pos) {
            CirElm = new InductorElmE();
            ReferenceName = "L";
        }

        public InductorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                CirElm = new InductorElmE(st.nextTokenDouble(), st.nextTokenDouble(), mFlags);
                ReferenceName = st.nextToken();
            } catch { }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override string dump() {
            var ce = (InductorElmE)CirElm;
            return ce.Inductance + " " + ce.mCirCurrent + " " + ReferenceName;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            var ce = (InductorElmE)CirElm;
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
            var ce = (InductorElmE)CirElm;
            double v1 = CirElm.CirVolts[0];
            double v2 = CirElm.CirVolts[1];
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
            var ce = (InductorElmE)CirElm;
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コイル" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(ce.Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(ce.CirPower, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InductorElmE)CirElm;
            if (n == 0) {
                return new ElementInfo("インダクタンス(H)", ce.Inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "台形近似";
                ei.CheckBox.Checked = ((InductorElmE)CirElm).Ind.IsTrapezoidal;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (InductorElmE)CirElm;
            if (n == 0 && ei.Value > 0) {
                ce.Inductance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            ce.Ind.Setup(ce.Inductance, CirElm.mCirCurrent, mFlags);
        }
    }
}
