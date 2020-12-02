using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class CustomCompositeElm : CompositeElm {
        public static string lastModelName = "default";

        string modelName;
        CustomCompositeChipElm chip;
        int postCount;
        int inputCount, outputCount;
        CustomCompositeModel model;
        List<CustomCompositeModel> models;

        public CustomCompositeElm(int xx, int yy) : base(xx, yy) {
            /* use last model as default when creating new element in UI.
             * use default otherwise, to avoid infinite recursion when creating nested subcircuits. */
            modelName = (xx == 0 && yy == 0) ? "default" : lastModelName;

            mFlags |= FLAG_ESCAPE;
            updateModels();
        }

        public CustomCompositeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            modelName = CustomLogicModel.unescape(st.nextToken());
            updateModels(st);
        }

        protected override string dump() {
            /* insert model name before the elements */
            string s = dumpWithMask(0);
            s += " " + CustomLogicModel.escape(modelName);
            s += dumpElements();
            return s;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.CUSTOM_COMPOSITE; }

        public override string dumpModel() {
            string modelStr = "";

            /* dump models of all children */
            for (int i = 0; i < compElmList.Count; i++) {
                var ce = compElmList[i];
                string m = ce.dumpModel();
                if (string.IsNullOrEmpty(m)) {
                    if (string.IsNullOrEmpty(modelStr)) {
                        modelStr += "\n";
                    }
                    modelStr += m;
                }
            }
            if (model.dumped) {
                return modelStr;
            }
            /* dump our model */
            if (string.IsNullOrEmpty(modelStr)) {
                modelStr += "\n";
            }
            modelStr += model.dump();

            return modelStr;
        }

        public override void draw(Graphics g) {
            for (int i = 0; i != postCount; i++) {
                chip.Volts[i] = Volts[i];
                chip.pins[i].current = getCurrentIntoNode(i);
            }
            chip.IsSelected = needsHighlight();
            chip.draw(g);
            BoundingBox = chip.BoundingBox;
        }

        public override void setPoints() {
            chip = new CustomCompositeChipElm(X1, Y1);
            chip.X2 = X2;
            chip.Y2 = Y2;

            chip.sizeX = model.sizeX;
            chip.sizeY = model.sizeY;
            chip.allocPins(postCount);
            int i;
            for (i = 0; i != postCount; i++) {
                var pin = model.extList[i];
                chip.setPin(i, pin.pos, pin.side, pin.name);
            }

            chip.setPoints();
            for (i = 0; i != getPostCount(); i++) {
                setPost(i, chip.getPost(i));
            }
        }

        public override void updateModels() {
            updateModels(null);
        }

        public void updateModels(StringTokenizer st) {
            model = CustomCompositeModel.getModelWithName(modelName);
            if (model == null) {
                return;
            }
            postCount = model.extList.Count;
            var externalNodes = new int[postCount];
            int i;
            for (i = 0; i != postCount; i++) {
                externalNodes[i] = model.extList[i].node;
            }
            if (st == null) {
                st = new StringTokenizer(model.elmDump, " ");
            }
            loadComposite(st, model.nodeList, externalNodes);
            allocNodes();
            setPoints();
        }

        public override int getPostCount() { return postCount; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo(EditInfo.MakeLink("subcircuits.html", "Model Name"), 0, -1, -1);
                models = CustomCompositeModel.getModelList();
                ei.Choice = new ComboBox();
                int i;
                for (i = 0; i != models.Count; i++) {
                    var ccm = models[i];
                    ei.Choice.Items.Add(ccm.name);
                    if (ccm == model) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "Edit Model" };
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                model = models[ei.Choice.SelectedIndex];
                lastModelName = modelName = model.name;
                updateModels();
                setPoints();
                return;
            }
            if (n == 1) {
                if (model.name == "default") {
                    MessageBox.Show("Can't edit this model.");
                    return;
                }
                var dlg = new EditCompositeModelDialog();
                dlg.setModel(model);
                dlg.createDialog();
                CirSim.dialogShowing = dlg;
                dlg.Show();
                return;
            }
        }
    }
}
