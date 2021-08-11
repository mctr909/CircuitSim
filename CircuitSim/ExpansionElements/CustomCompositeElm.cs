using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class CustomCompositeElm : CompositeElm {
        public static string lastModelName = "default";

        string modelName;
        CustomCompositeChipElm chip;
        int postCount;
        int inputCount;
        int outputCount;
        CustomCompositeModel model;
        List<CustomCompositeModel> models;

        public CustomCompositeElm(Point pos) : base(pos) {
            /* use last model as default when creating new element in UI.
             * use default otherwise, to avoid infinite recursion when creating nested subcircuits. */
            modelName = (pos.X == 0 && pos.Y == 0) ? "default" : lastModelName;

            mFlags |= FLAG_ESCAPE;
            UpdateModels();
        }

        public CustomCompositeElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            modelName = CustomLogicModel.unescape(st.nextToken());
            updateModels(st);
        }

        public override int PostCount { get { return postCount; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CUSTOM_COMPOSITE; } }

        protected override string dump() {
            /* insert model name before the elements */
            string s = dumpWithMask(0);
            s += " " + CustomLogicModel.escape(modelName);
            s += dumpElements();
            return s;
        }

        public override string DumpModel() {
            string modelStr = "";

            /* dump models of all children */
            for (int i = 0; i < compElmList.Count; i++) {
                var ce = compElmList[i];
                string m = ce.DumpModel();
                if (string.IsNullOrEmpty(m)) {
                    if (string.IsNullOrEmpty(modelStr)) {
                        modelStr += "\n";
                    }
                    modelStr += m;
                }
            }
            if (model.Dumped) {
                return modelStr;
            }
            /* dump our model */
            if (string.IsNullOrEmpty(modelStr)) {
                modelStr += "\n";
            }
            modelStr += model.Dump();

            return modelStr;
        }

        public override void Draw(CustomGraphics g) {
            for (int i = 0; i != postCount; i++) {
                chip.Volts[i] = Volts[i];
                chip.pins[i].current = GetCurrentIntoNode(i);
            }
            chip.IsSelected = NeedsHighlight;
            chip.Draw(g);
            BoundingBox = chip.BoundingBox;
        }

        public override void SetPoints() {
            chip = new CustomCompositeChipElm(P1);
            chip.P2.X = P2.X;
            chip.P2.Y = P2.Y;

            chip.sizeX = model.SizeX;
            chip.sizeY = model.SizeY;
            chip.allocPins(postCount);
            int i;
            for (i = 0; i != postCount; i++) {
                var pin = model.ExtList[i];
                chip.setPin(i, pin.pos, pin.side, pin.name);
            }

            chip.SetPoints();
            for (i = 0; i != PostCount; i++) {
                setPost(i, chip.GetPost(i));
            }
        }

        public override void UpdateModels() {
            updateModels(null);
        }

        public void updateModels(StringTokenizer st) {
            model = CustomCompositeModel.GetModelWithName(modelName);
            if (model == null) {
                return;
            }
            postCount = model.ExtList.Count;
            var externalNodes = new int[postCount];
            int i;
            for (i = 0; i != postCount; i++) {
                externalNodes[i] = model.ExtList[i].node;
            }
            if (st == null) {
                st = new StringTokenizer(model.ElmDump, " ");
            }
            loadComposite(st, model.NodeList, externalNodes);
            allocNodes();
            SetPoints();
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo(ElementInfo.MakeLink("subcircuits.html", "Model Name"), 0, -1, -1);
                models = CustomCompositeModel.GetModelList();
                ei.Choice = new ComboBox();
                int i;
                for (i = 0; i != models.Count; i++) {
                    var ccm = models[i];
                    ei.Choice.Items.Add(ccm.Name);
                    if (ccm == model) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "Edit Model" };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                model = models[ei.Choice.SelectedIndex];
                lastModelName = modelName = model.Name;
                UpdateModels();
                SetPoints();
                return;
            }
            if (n == 1) {
                if (model.Name == "default") {
                    MessageBox.Show("Can't edit this model.");
                    return;
                }
                var dlg = new EditCompositeModelDialog();
                dlg.SetModel(model);
                dlg.CreateDialog();
                CirSim.DialogShowing = dlg;
                dlg.Show();
                return;
            }
        }
    }
}
