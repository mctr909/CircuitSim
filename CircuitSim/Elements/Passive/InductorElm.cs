using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorElm : CircuitElm {
        const int BODY_LEN = 24;

        Inductor mInd;
        Point mValuePos;
        Point mNamePos;
        string mReferenceName = "";

        public InductorElm(Point pos) : base(pos) {
            mInd = new Inductor(mCir);
            Inductance = 0.001;
            mInd.setup(Inductance, mCurrent, mFlags);
        }

        public InductorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mInd = new Inductor(mCir);
            try {
                Inductance = st.nextTokenDouble();
                mCurrent = st.nextTokenDouble();
                mReferenceName = st.nextToken();
            } catch { }
            mInd.setup(Inductance, mCurrent, mFlags);
        }

        public double Inductance { get; set; }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override bool NonLinear { get { return mInd.nonLinear(); } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override string dump() {
            return Inductance + " " + mCurrent + " " + mReferenceName;
        }

        protected override void calculateCurrent() {
            var voltdiff = Volts[0] - Volts[1];
            mCurrent = mInd.calculateCurrent(voltdiff);
        }

        public override void Stamp() { mInd.stamp(Nodes[0], Nodes[1]); }

        public override void StartIteration() {
            mInd.startIteration(Volts[0] - Volts[1]);
        }

        public override void DoStep() {
            double voltdiff = Volts[0] - Volts[1];
            mInd.doStep(voltdiff);
        }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = mCurCount = 0;
            mInd.reset();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            if (mPoint1.Y == mPoint2.Y) {
                var wv = Context.GetTextSize(Utils.ShortUnitText(Inductance, "")).Width * 0.5;
                var wn = Context.GetTextSize(mReferenceName).Width * 0.5;
                interpPoint(ref mValuePos, 0.5 + wv / mLen * mDsign, 10 * mDsign);
                interpPoint(ref mNamePos, 0.5 - wn / mLen * mDsign, -12 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                interpPoint(ref mValuePos, 0.5, -4 * mDsign);
                interpPoint(ref mNamePos, 0.5, 4 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, -8 * mDsign);
                interpPoint(ref mNamePos, 0.5, 8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            double v1 = Volts[0];
            double v2 = Volts[1];
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();
            drawCoil(mLead1, mLead2, v1, v2);

            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.ShortUnitText(Inductance, "");
                g.DrawRightText(s, mValuePos.X, mValuePos.Y);
                g.DrawLeftText(mReferenceName, mNamePos.X, mNamePos.Y);
            }
            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = string.IsNullOrEmpty(mReferenceName) ? "コイル" : mReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("インダクタンス(H)", Inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = mReferenceName;
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
                mReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            mInd.setup(Inductance, mCurrent, mFlags);
        }
    }
}
