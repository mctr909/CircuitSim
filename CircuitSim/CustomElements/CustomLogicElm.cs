using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Circuit.ActiveElements;

namespace Circuit.CustomElements {
    class CustomLogicElm : ChipElm {
        string modelName;
        int postCount;
        int inputCount, outputCount;
        CustomLogicModel model;
        bool[] lastValues;
        bool[] patternValues;
        bool[] highImpedance;

        static string lastModelName = "default";

        public CustomLogicElm(Point pos) : base(pos) {
            modelName = lastModelName;
            SetupPins();
        }

        public CustomLogicElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            modelName = CustomLogicModel.unescape(st.nextToken());
            UpdateModels();
            int i;
            for (i = 0; i != PostCount; i++) {
                if (pins[i].output) {
                    Volts[i] = st.nextTokenDouble();
                    pins[i].value = Volts[i] > 2.5;
                }
            }
        }

        public override int PostCount { get { return postCount; } }

        public override int VoltageSourceCount { get { return outputCount; } }

        public override bool NonLinear { get { return hasTriState; } }

        public override int InternalNodeCount {
            /* for tri-state outputs, we need an internal node to connect a voltage source to, and then connect a resistor from there to the output.
             * we do this for all outputs if any of them are tri-state */
            get { return hasTriState ? outputCount : 0; }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.CUSTOM_LOGIC; } }

        /* keep track of whether we have any tri-state outputs.
         * if not, then we can simplify things quite a bit, making the simulation faster */
        bool hasTriState { get { return model == null ? false : model.triState; } }

        protected override string dump() {
            var s = base.dump();
            s += " " + CustomLogicModel.escape(modelName);
            /* the code to do this in ChipElm doesn't work here because we don't know
             * how many pins to read until we read the model name!  So we have to
             * duplicate it here. */
            for (int i = 0; i != PostCount; i++) {
                if (pins[i].output) {
                    s += " " + Volts[i];
                }
            }
            return s;
        }

        public override string DumpModel() {
            if (model.dumped) {
                return "";
            }
            return model.dump();
        }

        public override void UpdateModels() {
            model = CustomLogicModel.getModelWithNameOrCopy(modelName, model);
            SetupPins();
            allocNodes();
            SetPoints();
        }

        public override void SetupPins() {
            if (modelName == null) {
                postCount = bits;
                allocNodes();
                return;
            }
            model = CustomLogicModel.getModelWithName(modelName);
            inputCount = model.inputs.Length;
            outputCount = model.outputs.Length;
            sizeY = inputCount > outputCount ? inputCount : outputCount;
            if (sizeY == 0) {
                sizeY = 1;
            }
            sizeX = 2;
            postCount = inputCount + outputCount;
            pins = new Pin[postCount];
            for (int i = 0; i != inputCount; i++) {
                pins[i] = new Pin(this, i, SIDE_W, model.inputs[i]);
                pins[i].fixName();
            }
            for (int i = 0; i != outputCount; i++) {
                pins[i + inputCount] = new Pin(this, i, SIDE_E, model.outputs[i]);
                pins[i + inputCount].output = true;
                pins[i + inputCount].fixName();
            }
            lastValues = new bool[postCount];
            patternValues = new bool[26];
            highImpedance = new bool[postCount];
        }

        public override void Stamp() {
            int add = hasTriState ? outputCount : 0;
            for (int i = 0; i != PostCount; i++) {
                var p = pins[i];
                if (p.output) {
                    mCir.StampVoltageSource(0, Nodes[i + add], p.voltSource);
                    if (hasTriState) {
                        mCir.StampNonLinear(Nodes[i + add]);
                        mCir.StampNonLinear(Nodes[i]);
                    }
                }
            }
        }

        public override void DoStep() {
            for (int i = 0; i != PostCount; i++) {
                var p = pins[i];
                if (!p.output) {
                    p.value = Volts[i] > 2.5;
                }
            }
            execute();
            int add = hasTriState ? outputCount : 0;
            for (int i = 0; i != PostCount; i++) {
                var p = pins[i];
                if (p.output) {
                    /* connect output voltage source (to internal node if tri-state, otherwise connect directly to output) */
                    mCir.UpdateVoltageSource(0, Nodes[i + add], p.voltSource, p.value ? 5 : 0);

                    /* add resistor for tri-state if necessary */
                    if (hasTriState) {
                        mCir.StampResistor(Nodes[i + add], Nodes[i], highImpedance[i] ? 1e8 : 1e-3);
                    }
                }
            }
        }

        protected override void execute() {
            for (int i = 0; i != model.rulesLeft.Count; i++) {
                /* check for a match */
                string rl = model.rulesLeft[i];
                int j;
                for (j = 0; j != rl.Length; j++) {
                    char x = rl.ElementAt(j);
                    if (x == '0' || x == '1') {
                        if (pins[j].value == (x == '1')) {
                            continue;
                        }
                        break;
                    }

                    /* don't care */
                    if (x == '?') {
                        continue;
                    }
                    /* up transition */
                    if (x == '+') {
                        if (pins[j].value && !lastValues[j]) {
                            continue;
                        }
                        break;
                    }
                    /* down transition */
                    if (x == '-') {
                        if (!pins[j].value && lastValues[j]) {
                            continue;
                        }
                        break;
                    }
                    /* save pattern values */
                    if (x >= 'a' && x <= 'z') {
                        patternValues[x - 'a'] = pins[j].value;
                        continue;
                    }
                    /* compare pattern values */
                    if (x >= 'A' && x <= 'z') {
                        if (patternValues[x - 'A'] != pins[j].value) {
                            break;
                        }
                        continue;
                    }
                }
                if (j != rl.Length) {
                    continue;
                }

                /* success */
                string rr = model.rulesRight[i];
                for (j = 0; j != rr.Length; j++) {
                    char x = rr.ElementAt(j);
                    highImpedance[j + inputCount] = false;
                    if (x >= 'a' && x <= 'z') {
                        pins[j + inputCount].value = patternValues[x - 'a'];
                    } else if (x == '_') {
                        highImpedance[j + inputCount] = true;
                    } else {
                        pins[j + inputCount].value = (x == '1');
                    }
                }
                /* save values for transition checking */
                for (j = 0; j != postCount; j++) {
                    lastValues[j] = pins[j].value;
                }
                break;
            }
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = model.infoText;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                var ei = new ElementInfo("Model Name", 0, -1, -1);
                ei.Text = modelName;
                ei.DisallowSliders();
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.Button = new Button() { Text = "Edit Model" };
                return ei;
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2) {
                modelName = lastModelName = ei.Textf.Text;
                model = CustomLogicModel.getModelWithNameOrCopy(modelName, model);
                SetupPins();
                allocNodes();
                SetPoints();
                return;
            }
            if (n == 3) {
                var editDialog = new ElementInfoDialog(model, Sim);
                CirSim.CustomLogicEditDialog = editDialog;
                var pos = Sim.DisplayLocation;
                editDialog.Show(pos.X + P1.X, pos.Y + P1.Y);
                return;
            }
            base.SetElementValue(n, ei);
        }
    }
}
