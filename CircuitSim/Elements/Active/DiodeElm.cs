using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class DiodeElm : CircuitElm {
        public const int FLAG_FWDROP = 1;
        public const int FLAG_MODEL = 2;
        protected const int BODY_LEN = 12;
        protected const int HS = 6;

        static string lastModelName = "default";

        protected string mModelName;
        protected DiodeModel mModel;
        protected Point[] mPoly;
        protected Point[] mCathode;

        Diode mDiode;
        bool mHasResistance;
        bool mCustomModelUI;
        int mDiodeEndNode;
        List<DiodeModel> mModels;

        public DiodeElm(Point pos) : base(pos) {
            mModelName = lastModelName;
            mDiode = new Diode(mCir);
            ReferenceName = "D";
            setup();
        }

        public DiodeElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            const double defaultdrop = 0.805904783;
            mDiode = new Diode(mCir);
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            try {
                ReferenceName = st.nextToken();
            } catch { }
            if (0 != (f & FLAG_MODEL)) {
                try {
                    mModelName = CustomLogicModel.unescape(st.nextToken());
                } catch { }
            } else {
                if (0 != (f & FLAG_FWDROP)) {
                    try {
                        fwdrop = st.nextTokenDouble();
                    } catch { }
                }
                mModel = DiodeModel.GetModelWithParameters(fwdrop, zvoltage);
                mModelName = mModel.Name;
            }
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.DIODE; } }

        public override int CirInternalNodeCount { get { return mHasResistance ? 1 : 0; } }

        public override bool CirNonLinear { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.DIODE; } }

        protected override string dump() {
            mFlags |= FLAG_MODEL;
            return ReferenceName + " " + CustomLogicModel.escape(mModelName);
        }

        protected void setup() {
            mModel = DiodeModel.GetModelWithNameOrCopy(mModelName, mModel);
            mModelName = mModel.Name;
            mDiode.Setup(mModel);
            mHasResistance = (mModel.SeriesResistance > 0);
            mDiodeEndNode = (mHasResistance) ? 2 : 1;
            cirAllocNodes();
        }

        public override void UpdateModels() {
            setup();
        }

        public override string DumpModel() {
            if (mModel.BuiltIn || mModel.Dumped) {
                return null;
            }
            return mModel.Dump();
        }

        public override void CirStamp() {
            if (mHasResistance) {
                /* create diode from node 0 to internal node */
                mDiode.Stamp(CirNodes[0], CirNodes[2]);
                /* create resistor from internal node to node 1 */
                mCir.StampResistor(CirNodes[1], CirNodes[2], mModel.SeriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                mDiode.Stamp(CirNodes[0], CirNodes[1]);
            }
        }

        public override void CirDoStep() {
            mDiode.DoStep(CirVolts[0] - CirVolts[mDiodeEndNode]);
        }

        public override void CirStepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCirCurrent) > 1e12) {
                mCir.Stop("max current exceeded", this);
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mCathode = new Point[2];
            interpLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            mPoly = new Point[] { pa[0], pa[1], mLead2 };
            setTextPos();
        }

        protected void setTextPos() {
            mNameV = mPoint1.X == mPoint2.X;
            if (mPoint1.Y == mPoint2.Y) {
                var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
                interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 13 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mNamePos, 0.5, -22 * mDsign);
            } else {
                interpPoint(ref mNamePos, 0.5, -10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            drawDiode(g);
            doDots();
            drawPosts();
            drawName();
        }

        public override void CirReset() {
            mDiode.Reset();
            CirVolts[0] = CirVolts[1] = mCirCurCount = 0;
            if (mHasResistance) {
                CirVolts[2] = 0;
            }
        }

        protected void drawDiode(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, HS);

            draw2Leads();

            /* draw arrow thingy */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mPoly);
            /* draw thing arrow is pointing to */
            drawLead(mCathode[0], mCathode[1]);
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = mDiode.CalculateCurrent(CirVolts[0] - CirVolts[mDiodeEndNode]);
        }

        public override void GetInfo(string[] arr) {
            if (mModel.OldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + mModelName + ")";
            }
            arr[1] = "I = " + Utils.CurrentText(mCirCurrent);
            arr[2] = "Vd = " + Utils.VoltageText(CirVoltageDiff);
            arr[3] = "P = " + Utils.UnitText(CirPower, "W");
            if (mModel.OldStyle) {
                arr[4] = "Vf = " + Utils.VoltageText(mModel.FwDrop);
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (!mCustomModelUI && n == 1) {
                var ei = new ElementInfo("モデル", 0, -1, -1);
                mModels = DiodeModel.GetModelList(this is ZenerElm);
                ei.Choice = new ComboBox();
                for (int i = 0; i != mModels.Count; i++) {
                    var dm = mModels[i];
                    ei.Choice.Items.Add(dm.GetDescription());
                    if (dm == mModel) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (!mCustomModelUI && n == 1) {
                int ix = ei.Choice.SelectedIndex;
                if (ix >= mModels.Count) {
                    mModels = null;
                    mCustomModelUI = true;
                    ei.NewDialog = true;
                    return;
                }
                mModel = mModels[ei.Choice.SelectedIndex];
                mModelName = mModel.Name;
                setup();
                return;
            }
            base.SetElementValue(n, ei);
        }

        void setLastModelName(string n) {
            lastModelName = n;
        }
    }
}
