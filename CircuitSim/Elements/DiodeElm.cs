using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class DiodeElm : CircuitElm {
        Diode diode;
        public const int FLAG_FWDROP = 1;
        public const int FLAG_MODEL = 2;
        protected string modelName;
        protected DiodeModel model;
        static string lastModelName = "default";
        bool hasResistance;
        int diodeEndNode;

        const int hs = 6;
        Point[] poly;
        Point[] cathode;

        bool customModelUI;
        List<DiodeModel> models;

        public DiodeElm(int xx, int yy) : base(xx, yy) {
            modelName = lastModelName;
            diode = new Diode(Sim, mCir);
            setup();
        }

        public DiodeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            const double defaultdrop = .805904783;
            diode = new Diode(Sim, mCir);
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            if ((f & FLAG_MODEL) != 0) {
                modelName = CustomLogicModel.unescape(st.nextToken());
            } else {
                if ((f & FLAG_FWDROP) > 0) {
                    try {
                        fwdrop = st.nextTokenDouble();
                    } catch { }
                }
                model = DiodeModel.getModelWithParameters(fwdrop, zvoltage);
                modelName = model.name;
                /*Console.WriteLine("model name wparams = " + modelName); */
            }
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.DIODE; } }

        public override int InternalNodeCount { get { return hasResistance ? 1 : 0; } }

        public override bool NonLinear { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.DIODE; } }

        protected override string dump() {
            mFlags |= FLAG_MODEL;
            /*if (modelName == null) {
                Console.WriteLine("model name is null??");
                modelName = "default";
            }*/
            return CustomLogicModel.escape(modelName);
        }

        protected void setup() {
            /*Console.WriteLine("setting up for model " + modelName + " " + model); */
            model = DiodeModel.getModelWithNameOrCopy(modelName, model);
            modelName = model.name;   /* in case we couldn't find that model */
            diode.setup(model);
            hasResistance = (model.seriesResistance > 0);
            diodeEndNode = (hasResistance) ? 2 : 1;
            allocNodes();
        }

        public override void UpdateModels() {
            setup();
        }

        public override string DumpModel() {
            if (model.builtIn || model.dumped) {
                return null;
            }
            return model.dump();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(12);
            cathode = new Point[2];
            var pa = new Point[2];
            Utils.InterpPoint(mLead1, mLead2, ref pa[0], ref pa[1], 0, hs);
            Utils.InterpPoint(mLead1, mLead2, ref cathode[0], ref cathode[1], 1, hs);
            poly = new Point[] { pa[0], pa[1], mLead2 };
        }

        public override void Draw(CustomGraphics g) {
            drawDiode(g);
            doDots(g);
            drawPosts(g);
        }

        public override void Reset() {
            diode.reset();
            Volts[0] = Volts[1] = mCurCount = 0;
            if (hasResistance) {
                Volts[2] = 0;
            }
        }

        void drawDiode(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, hs);

            double v1 = Volts[0];
            double v2 = Volts[1];

            draw2Leads(g);

            /* draw arrow thingy */
            g.FillPolygon(getVoltageColor(v1), poly);
            /* draw thing arrow is pointing to */
            g.DrawThickLine(getVoltageColor(v2), cathode[0], cathode[1]);
        }

        public override void Stamp() {
            if (hasResistance) {
                /* create diode from node 0 to internal node */
                diode.stamp(Nodes[0], Nodes[2]);
                /* create resistor from internal node to node 1 */
                mCir.StampResistor(Nodes[1], Nodes[2], model.seriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                diode.stamp(Nodes[0], Nodes[1]);
            }
        }

        public override void DoStep() {
            diode.doStep(Volts[0] - Volts[diodeEndNode]);
        }

        protected override void calculateCurrent() {
            mCurrent = diode.calculateCurrent(Volts[0] - Volts[diodeEndNode]);
        }

        public override void GetInfo(string[] arr) {
            if (model.oldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + modelName + ")";
            }
            arr[1] = "I = " + Utils.CurrentText(mCurrent);
            arr[2] = "Vd = " + Utils.VoltageText(VoltageDiff);
            arr[3] = "P = " + Utils.UnitText(Power, "W");
            if (model.oldStyle) {
                arr[4] = "Vf = " + Utils.VoltageText(model.fwdrop);
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (!customModelUI && n == 0) {
                var ei = new ElementInfo("Model", 0, -1, -1);
                models = DiodeModel.getModelList(this is ZenerElm);
                ei.Choice = new ComboBox();
                for (int i = 0; i != models.Count; i++) {
                    var dm = models[i];
                    ei.Choice.Items.Add(dm.getDescription());
                    if (dm == model) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                ei.Choice.Items.Add("Custom");
                return ei;
            }
            if (n == 0) {
                var ei = new ElementInfo("Model Name", 0, -1, -1);
                ei.Text = modelName;
                return ei;
            }
            if (n == 1) {
                if (model.readOnly && !customModelUI) {
                    return null;
                }
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "Edit Model" };
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "Create Simple Model" };
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (!customModelUI && n == 0) {
                int ix = ei.Choice.SelectedIndex;
                if (ix >= models.Count) {
                    models = null;
                    customModelUI = true;
                    ei.NewDialog = true;
                    return;
                }
                model = models[ei.Choice.SelectedIndex];
                modelName = model.name;
                setup();
                return;
            }
            if (n == 0) {
                /* the text field may not have been created yet, check to avoid exception */
                if (ei.Textf == null) {
                    return;
                }
                modelName = ei.Textf.Text;
                setLastModelName(modelName);
                model = DiodeModel.getModelWithNameOrCopy(modelName, model);
                setup();
                return;
            }
            if (n == 1) {
                if (model.readOnly) {
                    MessageBox.Show("This model cannot be modified.\r\nChange the model name to allow customization.");
                    return;
                }
                CirSim.DiodeModelEditDialog = new ElementInfoDialog(model, Sim);
                CirSim.DiodeModelEditDialog.Show();
                return;
            }
            if (n == 2) {
                var dlg = new InputDialog("Fwd Voltage @ 1A", "0.8");
                double fwdrop;
                if (double.TryParse(dlg.Value, out fwdrop)) {
                    if (fwdrop > 0) {
                        model = DiodeModel.getModelWithVoltageDrop(fwdrop);
                        modelName = model.name;
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

        public override void StepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCurrent) > 1e12) {
                mCir.Stop("max current exceeded", this);
            }
        }
    }
}
