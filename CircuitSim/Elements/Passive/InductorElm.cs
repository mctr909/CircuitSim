using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorElm : CircuitElm {
        const int BODY_LEN = 24;

        Inductor mInd;

        public InductorElm(Point pos) : base(pos) {
            mInd = new Inductor(mCir);
            Inductance = 0.001;
            ReferenceName = "L";
            mInd.Setup(Inductance, mCirCurrent, mFlags);
        }

        public InductorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mInd = new Inductor(mCir);
            try {
                Inductance = st.nextTokenDouble();
                mCirCurrent = st.nextTokenDouble();
                ReferenceName = st.nextToken();
            } catch { }
            mInd.Setup(Inductance, mCirCurrent, mFlags);
        }

        public double Inductance { get; set; }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override bool CirNonLinear { get { return mInd.NonLinear(); } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override string dump() {
            return Inductance + " " + mCirCurrent + " " + ReferenceName;
        }

        protected override void cirCalculateCurrent() {
            var voltdiff = CirVolts[0] - CirVolts[1];
            mCirCurrent = mInd.CalculateCurrent(voltdiff);
        }

        public override void CirStamp() { mInd.Stamp(CirNodes[0], CirNodes[1]); }

        public override void CirStartIteration() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            mInd.StartIteration(voltdiff);
        }

        public override void CirDoStep() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            mInd.DoStep(voltdiff);
        }

        public override void CirReset() {
            mCirCurrent = CirVolts[0] = CirVolts[1] = mCirCurCount = 0;
            mInd.Reset();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.UnitText(Inductance, "")).Width * 0.5;
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
            double v1 = CirVolts[0];
            double v2 = CirVolts[1];
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();
            drawCoil(mLead1, mLead2, v1, v2);

            drawValue(Inductance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = string.IsNullOrEmpty(ReferenceName) ? "コイル" : ReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(CirPower, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("インダクタンス(H)", Inductance, 0, 0);
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
                ei.CheckBox.Checked = mInd.IsTrapezoidal;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                Inductance = ei.Value;
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
            mInd.Setup(Inductance, mCirCurrent, mFlags);
        }
    }
}
