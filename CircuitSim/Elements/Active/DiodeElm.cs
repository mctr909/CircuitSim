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
            setup();
        }

        public DiodeElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            const double defaultdrop = .805904783;
            mDiode = new Diode(mCir);
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            if ((f & FLAG_MODEL) != 0) {
                mModelName = CustomLogicModel.unescape(st.nextToken());
            } else {
                if ((f & FLAG_FWDROP) > 0) {
                    try {
                        fwdrop = st.nextTokenDouble();
                    } catch { }
                }
                mModel = DiodeModel.getModelWithParameters(fwdrop, zvoltage);
                mModelName = mModel.name;
                /*Console.WriteLine("model name wparams = " + modelName); */
            }
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.DIODE; } }

        public override int InternalNodeCount { get { return mHasResistance ? 1 : 0; } }

        public override bool NonLinear { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.DIODE; } }

        protected override string dump() {
            mFlags |= FLAG_MODEL;
            return CustomLogicModel.escape(mModelName);
        }

        protected void setup() {
            /*Console.WriteLine("setting up for model " + modelName + " " + model); */
            mModel = DiodeModel.getModelWithNameOrCopy(mModelName, mModel);
            mModelName = mModel.name;
            mDiode.setup(mModel);
            mHasResistance = (mModel.seriesResistance > 0);
            mDiodeEndNode = (mHasResistance) ? 2 : 1;
            allocNodes();
        }

        public override void UpdateModels() {
            setup();
        }

        public override string DumpModel() {
            if (mModel.builtIn || mModel.dumped) {
                return null;
            }
            return mModel.dump();
        }

        public override void Stamp() {
            if (mHasResistance) {
                /* create diode from node 0 to internal node */
                mDiode.stamp(Nodes[0], Nodes[2]);
                /* create resistor from internal node to node 1 */
                mCir.StampResistor(Nodes[1], Nodes[2], mModel.seriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                mDiode.stamp(Nodes[0], Nodes[1]);
            }
        }

        public override void DoStep() {
            mDiode.doStep(Volts[0] - Volts[mDiodeEndNode]);
        }

        public override void StepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCurrent) > 1e12) {
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
        }

        public override void Draw() {
            drawDiode();
            doDots();
            drawPosts();
        }

        public override void Reset() {
            mDiode.reset();
            Volts[0] = Volts[1] = mCurCount = 0;
            if (mHasResistance) {
                Volts[2] = 0;
            }
        }

        protected void drawDiode() {
            setBbox(mPoint1, mPoint2, HS);

            draw2Leads();

            /* draw arrow thingy */
            drawVoltage(0, mPoly);
            /* draw thing arrow is pointing to */
            drawVoltage(1, mCathode[0], mCathode[1]);
        }

        protected override void calculateCurrent() {
            mCurrent = mDiode.calculateCurrent(Volts[0] - Volts[mDiodeEndNode]);
        }

        public override void GetInfo(string[] arr) {
            if (mModel.oldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + mModelName + ")";
            }
            arr[1] = "I = " + Utils.CurrentText(mCurrent);
            arr[2] = "Vd = " + Utils.VoltageText(VoltageDiff);
            arr[3] = "P = " + Utils.UnitText(Power, "W");
            if (mModel.oldStyle) {
                arr[4] = "Vf = " + Utils.VoltageText(mModel.fwdrop);
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (!mCustomModelUI && n == 0) {
                var ei = new ElementInfo("モデル", 0, -1, -1);
                mModels = DiodeModel.getModelList(this is ZenerElm);
                ei.Choice = new ComboBox();
                for (int i = 0; i != mModels.Count; i++) {
                    var dm = mModels[i];
                    ei.Choice.Items.Add(dm.getDescription());
                    if (dm == mModel) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                ei.Choice.Items.Add("カスタム");
                return ei;
            }
            if (n == 0) {
                var ei = new ElementInfo("モデル名", 0, -1, -1);
                ei.Text = mModelName;
                return ei;
            }
            if (n == 1) {
                if (mModel.readOnly && !mCustomModelUI) {
                    return null;
                }
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "編集" };
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "モデル作成" };
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (!mCustomModelUI && n == 0) {
                int ix = ei.Choice.SelectedIndex;
                if (ix >= mModels.Count) {
                    mModels = null;
                    mCustomModelUI = true;
                    ei.NewDialog = true;
                    return;
                }
                mModel = mModels[ei.Choice.SelectedIndex];
                mModelName = mModel.name;
                setup();
                return;
            }
            if (n == 0) {
                /* the text field may not have been created yet, check to avoid exception */
                if (ei.Textf == null) {
                    return;
                }
                mModelName = ei.Textf.Text;
                setLastModelName(mModelName);
                mModel = DiodeModel.getModelWithNameOrCopy(mModelName, mModel);
                setup();
                return;
            }
            if (n == 1) {
                if (mModel.readOnly) {
                    MessageBox.Show("This model cannot be modified.\r\nChange the model name to allow customization.");
                    return;
                }
                CirSim.DiodeModelEditDialog = new ElementInfoDialog(mModel);
                CirSim.DiodeModelEditDialog.Show();
                return;
            }
            if (n == 2) {
                var dlg = new InputDialog("順電圧 @ 1A", "0.8");
                double fwdrop;
                if (double.TryParse(dlg.Value, out fwdrop)) {
                    if (fwdrop > 0) {
                        mModel = DiodeModel.getModelWithVoltageDrop(fwdrop);
                        mModelName = mModel.name;
                        ei.NewDialog = true;
                        return;
                    }
                }
            }
            base.SetElementValue(n, ei);
        }

        void setLastModelName(string n) {
            lastModelName = n;
        }
    }
}
