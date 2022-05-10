using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class DiodeUI : BaseUI {
        public const int FLAG_FWDROP = 1;
        public const int FLAG_MODEL = 2;
        protected const int HS = 5;
        protected int BODY_LEN = 9;

        protected Point[] mPoly;
        protected Point[] mCathode;

        protected List<DiodeModel> mModels;
        protected bool mCustomModelUI;

        public DiodeUI(Point pos) : base(pos) { }

        public DiodeUI(Point pos, string referenceName) : base(pos) {
            Elm = new DiodeElm();
            ReferenceName = referenceName;
            setup();
        }

        public DiodeUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public DiodeUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                ReferenceName = st.nextToken();
            } catch { }
            Elm = new DiodeElm(st, 0 != (f & FLAG_FWDROP), 0 != (f & FLAG_MODEL));
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.DIODE; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.DIODE; } }

        protected override string dump() {
            mFlags |= FLAG_MODEL;
            var ce = (DiodeElm)Elm;
            return ReferenceName + " " + CustomLogicModel.escape(ce.mModelName);
        }

        public override void UpdateModels() {
            setup();
        }

        public override string DumpModel() {
            var ce = (DiodeElm)Elm;
            if (ce.mModel.BuiltIn || ce.mModel.Dumped) {
                return null;
            }
            return ce.mModel.Dump();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mCathode = new Point[4];
            interpLeadAB(ref mCathode[0], ref mCathode[1], (BODY_LEN - 1.0) / BODY_LEN, HS);
            interpLeadAB(ref mCathode[3], ref mCathode[2], 1, HS);
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            mPoly = new Point[] { pa[0], pa[1], mLead2 };
            setTextPos();
        }

        protected void setTextPos() {
            mNameV = mPost1.X == mPost2.X;
            if (mPost1.Y == mPost2.Y) {
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

        protected void drawDiode(CustomGraphics g) {
            setBbox(mPost1, mPost2, HS);

            draw2Leads();

            var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            /* draw arrow thingy */
            g.FillPolygon(color, mPoly);
            /* draw thing arrow is pointing to */
            if (mCathode.Length < 4) {
                drawLead(mCathode[0], mCathode[1]);
            } else {
                g.FillPolygon(color, mCathode);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (DiodeElm)Elm;
            if (ce.mModel.OldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + ce.mModelName + ")";
            }
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "Vd = " + Utils.VoltageText(ce.VoltageDiff);
            arr[3] = "P = " + Utils.UnitText(ce.Power, "W");
            if (ce.mModel.OldStyle) {
                arr[4] = "Vf = " + Utils.VoltageText(ce.mModel.FwDrop);
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (DiodeElm)Elm;
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (!mCustomModelUI && n == 1) {
                var ei = new ElementInfo("モデル", 0, -1, -1);
                mModels = DiodeModel.GetModelList(this is DiodeUIZener);
                ei.Choice = new ComboBox();
                for (int i = 0; i != mModels.Count; i++) {
                    var dm = mModels[i];
                    ei.Choice.Items.Add(dm.GetDescription());
                    if (dm == ce.mModel) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (DiodeElm)Elm;
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
                ce.mModel = mModels[ei.Choice.SelectedIndex];
                ce.mModelName = ce.mModel.Name;
                setup();
                return;
            }
            base.SetElementValue(n, ei);
        }

        protected void setup() {
            ((DiodeElm)Elm).Setup();
        }

        void setLastModelName(string n) {
            DiodeElm.lastModelName = n;
        }
    }
}
