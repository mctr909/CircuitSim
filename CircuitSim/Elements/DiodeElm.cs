using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;

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

        const int hs = 8;
        Point[] poly;
        Point[] cathode;

        bool customModelUI;
        List<DiodeModel> models;

        public DiodeElm(int xx, int yy) : base(xx, yy) {
            modelName = lastModelName;
            diode = new Diode(sim, cir);
            setup();
        }

        public DiodeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            const double defaultdrop = .805904783;
            diode = new Diode(sim, cir);
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

        public override bool nonLinear() { return true; }

        protected void setup() {
            /*Console.WriteLine("setting up for model " + modelName + " " + model); */
            model = DiodeModel.getModelWithNameOrCopy(modelName, model);
            modelName = model.name;   /* in case we couldn't find that model */
            diode.setup(model);
            hasResistance = (model.seriesResistance > 0);
            diodeEndNode = (hasResistance) ? 2 : 1;
            allocNodes();
        }

        public override int getInternalNodeCount() { return hasResistance ? 1 : 0; }

        public override void updateModels() {
            setup();
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.DIODE; }

        public override string dump() {
            mFlags |= FLAG_MODEL;
            /*if (modelName == null) {
                Console.WriteLine("model name is null??");
                modelName = "default";
            }*/
            return base.dump() + " " + CustomLogicModel.escape(modelName);
        }

        public override string dumpModel() {
            if (model.builtIn || model.dumped) {
                return null;
            }
            return model.dump();
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads(16);
            cathode = newPointArray(2);
            var pa = newPointArray(2);
            interpPoint(mLead1, mLead2, ref pa[0], ref pa[1], 0, hs);
            interpPoint(mLead1, mLead2, ref cathode[0], ref cathode[1], 1, hs);
            poly = createPolygon(pa[0], pa[1], mLead2).ToArray();
        }

        public override void draw(Graphics g) {
            drawDiode(g);
            doDots(g);
            drawPosts(g);
        }

        public override void reset() {
            diode.reset();
            Volts[0] = Volts[1] = mCurCount = 0;
            if (hasResistance) {
                Volts[2] = 0;
            }
        }

        void drawDiode(Graphics g) {
            setBbox(mPoint1, mPoint2, hs);

            double v1 = Volts[0];
            double v2 = Volts[1];

            draw2Leads(g);

            /* draw arrow thingy */
            fillPolygon(g, getVoltageColor(v1), poly);
            /* draw thing arrow is pointing to */
            drawThickLine(g, getVoltageColor(v2), cathode[0], cathode[1]);
        }

        public override void stamp() {
            if (hasResistance) {
                /* create diode from node 0 to internal node */
                diode.stamp(Nodes[0], Nodes[2]);
                /* create resistor from internal node to node 1 */
                cir.StampResistor(Nodes[1], Nodes[2], model.seriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                diode.stamp(Nodes[0], Nodes[1]);
            }
        }

        public override void doStep() {
            diode.doStep(Volts[0] - Volts[diodeEndNode]);
        }

        public override void calculateCurrent() {
            mCurrent = diode.calculateCurrent(Volts[0] - Volts[diodeEndNode]);
        }

        public override void getInfo(string[] arr) {
            if (model.oldStyle) {
                arr[0] = "diode";
            } else {
                arr[0] = "diode (" + modelName + ")";
            }
            arr[1] = "I = " + getCurrentText(getCurrent());
            arr[2] = "Vd = " + getVoltageText(getVoltageDiff());
            arr[3] = "P = " + getUnitText(getPower(), "W");
            if (model.oldStyle) {
                arr[4] = "Vf = " + getVoltageText(model.fwdrop);
            }
        }

        public override EditInfo getEditInfo(int n) {
            if (!customModelUI && n == 0) {
                var ei = new EditInfo("Model", 0, -1, -1);
                models = DiodeModel.getModelList(typeof(ZenerElm) == GetType());
                ei.choice = new ComboBox();
                for (int i = 0; i != models.Count; i++) {
                    var dm = models[i];
                    ei.choice.Items.Add(dm.getDescription());
                    if (dm == model) {
                        ei.choice.SelectedIndex = i;
                    }
                }
                ei.choice.Items.Add("Custom");
                return ei;
            }
            if (n == 0) {
                var ei = new EditInfo("Model Name", 0, -1, -1);
                ei.text = modelName;
                return ei;
            }
            if (n == 1) {
                if (model.readOnly && !customModelUI) {
                    return null;
                }
                var ei = new EditInfo("", 0, -1, -1);
                ei.button = new Button() { Text = "Edit Model" };
                return ei;
            }
            if (n == 2) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.button = new Button() { Text = "Create Simple Model" };
                return ei;
            }
            return base.getEditInfo(n);
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (!customModelUI && n == 0) {
                int ix = ei.choice.SelectedIndex;
                if (ix >= models.Count) {
                    models = null;
                    customModelUI = true;
                    ei.newDialog = true;
                    return;
                }
                model = models[ei.choice.SelectedIndex];
                modelName = model.name;
                setup();
                return;
            }
            if (n == 0) {
                /* the text field may not have been created yet, check to avoid exception */
                if (ei.textf == null) {
                    return;
                }
                modelName = ei.textf.Text;
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
                var editDialog = new EditDialog(model, sim);
                CirSim.diodeModelEditDialog = editDialog;
                editDialog.Show();
                return;
            }
            if (n == 2) {
                var dlg = new InputDialog("Fwd Voltage @ 1A", "0.8");
                double fwdrop;
                if (double.TryParse(dlg.Value, out fwdrop)) {
                    if (fwdrop > 0) {
                        model = DiodeModel.getModelWithVoltageDrop(fwdrop);
                        modelName = model.name;
                        ei.newDialog = true;
                        return;
                    }
                }
            }
            base.setEditValue(n, ei);
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.DIODE; }

        void setLastModelName(string n) {
            lastModelName = n;
        }

        public override void stepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCurrent) > 1e12) {
                cir.Stop("max current exceeded", this);
            }
        }
    }
}
