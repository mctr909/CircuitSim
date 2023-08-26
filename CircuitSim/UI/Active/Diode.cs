using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Diode : BaseUI {
        public const int FLAG_FWDROP = 1;
        public const int FLAG_MODEL = 2;
        protected const int HS = 5;
        protected int BODY_LEN = 9;

        protected PointF[] mPoly;
        protected PointF[] mCathode;

        protected List<DiodeModel> mModels;
        protected bool mCustomModelUI;

        public Diode(Point pos) : base(pos) { }

        public Diode(Point pos, string referenceName) : base(pos) {
            Elm = new ElmDiode();
            DumpInfo.ReferenceName = referenceName;
            setup();
        }

        public Diode(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public Diode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmDiode(st, 0 != (f & FLAG_FWDROP), 0 != (f & FLAG_MODEL));
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.DIODE; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.DIODE; } }

        protected override void dump(List<object> optionList) {
            DumpInfo.Flags |= FLAG_MODEL;
            var ce = (ElmDiode)Elm;
            optionList.Add(Utils.Escape(ce.mModelName));
        }

        public override void UpdateModels() {
            setup();
        }

        public override string DumpModel() {
            var ce = (ElmDiode)Elm;
            if (ce.mModel.BuiltIn || ce.mModel.Dumped) {
                return null;
            }
            return ce.mModel.Dump();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mCathode = new PointF[4];
            interpLeadAB(ref mCathode[0], ref mCathode[1], (BODY_LEN - 1.0) / BODY_LEN, HS);
            interpLeadAB(ref mCathode[3], ref mCathode[2], 1, HS);
            var pa = new PointF[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            mPoly = new PointF[] { pa[0], pa[1], mLead2 };
            setTextPos();
            setBbox(HS);
        }

        protected void setTextPos() {
            if (mHorizontal) {
                interpPost(ref mNamePos, 0.5, 13 * mDsign);
            } else if (mVertical) {
                interpPost(ref mNamePos, 0.5, -22 * mDsign);
            } else {
                interpPost(ref mNamePos, 0.5, -10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            drawDiode();
            doDots();
            drawPosts();
            drawName();
        }

        protected void drawDiode() {
            draw2Leads();
            /* draw arrow thingy */
            fillPolygon(mPoly);
            /* draw thing arrow is pointing to */
            if (mCathode.Length < 4) {
                drawLine(mCathode[0], mCathode[1]);
            } else {
                fillPolygon(mCathode);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmDiode)Elm;
            if (ce.mModel.OldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + ce.mModelName + ")";
            }
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "Vd = " + Utils.VoltageText(ce.GetVoltageDiff());
            if (ce.mModel.OldStyle) {
                arr[3] = "Vf = " + Utils.VoltageText(ce.mModel.FwDrop);
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmDiode)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            if (!mCustomModelUI && r == 1) {
                var ei = new ElementInfo("モデル");
                mModels = DiodeModel.GetModelList(this is DiodeZener);
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
            return base.GetElementInfo(r, c);
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmDiode)Elm;
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
            base.SetElementValue(n, c, ei);
            if (n == 0) {
                DumpInfo.ReferenceName = ei.Text;
                setTextPos();
            }
        }

        protected void setup() {
            ((ElmDiode)Elm).Setup();
        }

        void setLastModelName(string n) {
            ElmDiode.lastModelName = n;
        }
    }
}
