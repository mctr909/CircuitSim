using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Circuit {
    class CustomLogicModel : Editable {
        static int FLAG_SCHMITT = 1;
        static Dictionary<string, CustomLogicModel> modelMap;

        int flags;
        string name;
        public string[] inputs;
        public string[] outputs;
        public string infoText;
        string rules;
        public List<string> rulesLeft;
        public List<string>  rulesRight;
        public bool dumped;
        public bool triState;

        CustomLogicModel() {
            inputs = listToArray("A,B");
            outputs = listToArray("C,D");
            rulesLeft = new List<string>();
            rulesRight = new List<string>();
            rules = "";
        }

        CustomLogicModel(CustomLogicModel copy) {
            flags = copy.flags;
            inputs = copy.inputs;
            outputs = copy.outputs;
            infoText = copy.infoText;
            rules = copy.rules;
            rulesLeft = copy.rulesLeft;
            rulesRight = copy.rulesRight;
        }

        public static CustomLogicModel getModelWithName(string name) {
            if (modelMap == null) {
                modelMap = new Dictionary<string, CustomLogicModel>();
            }
            if (modelMap.ContainsKey(name)) {
                return modelMap[name];
            }
            var lm = new CustomLogicModel();
            lm.name = name;
            lm.infoText = (name == "default") ? "custom logic" : name;
            modelMap.Add(name, lm);
            return lm;
        }

        public static CustomLogicModel getModelWithNameOrCopy(string name, CustomLogicModel oldmodel) {
            if (modelMap == null) {
                modelMap = new Dictionary<string, CustomLogicModel>();
            }
            if (modelMap.ContainsKey(name)) {
                return modelMap[name];
            }
            if (null == oldmodel) {
                return getModelWithName(name);
            }
            var lm = new CustomLogicModel(oldmodel);
            lm.name = name;
            lm.infoText = name;
            modelMap.Add(name, lm);
            return lm;
        }

        public static void clearDumpedFlags() {
            if (modelMap == null) {
                return;
            }
            foreach (var it in modelMap) {
                it.Value.dumped = false;
            }
        }

        public static void undumpModel(StringTokenizer st) {
            string name = unescape(st.nextToken());
            var model = getModelWithName(name);
            model.undump(st);
        }

        void undump(StringTokenizer st) {
            flags = st.nextTokenInt();
            inputs = listToArray(unescape(st.nextToken()));
            outputs = listToArray(unescape(st.nextToken()));
            infoText = unescape(st.nextToken());
            rules = unescape(st.nextToken());
            parseRules();
        }

        string arrayToList(string[] arr) {
            if (arr == null) {
                return "";
            }
            if (arr.Length == 0) {
                return "";
            }
            string x = arr[0];
            int i;
            for (i = 1; i < arr.Length; i++) {
                x += "," + arr[i];
            }
            return x;
        }

        string[] listToArray(string arr) {
            return arr.Split(',');
        }

        public ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("Inputs", 0, -1, -1);
                ei.Text = arrayToList(inputs);
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("Outputs", 0, -1, -1);
                ei.Text = arrayToList(outputs);
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("Info Text", 0, -1, -1);
                ei.Text = infoText;
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo(ElementInfo.MakeLink("customlogic.html", "Definition"), 0, -1, -1);
                ei.TextArea = new TextBox();
                ei.TextArea.Multiline = true;
                ei.TextArea.Text = rules;
                ei.TextArea.Height = 80;
                ei.TextArea.Width = 120;
                return ei;
            }
            /*
             * not implemented
            if (n == 4) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new Checkbox("Schmitt", (flags & FLAG_SCHMITT) != 0);
                return ei;
            }
            */
            return null;
        }

        public void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                inputs = listToArray(ei.Textf.Text);
            }
            if (n == 1) {
                outputs = listToArray(ei.Textf.Text);
            }
            if (n == 2) {
                infoText = ei.Textf.Text;
            }
            if (n == 3) {
                rules = ei.TextArea.Text;
                parseRules();
            }
            if (n == 4) {
                if (ei.CheckBox.Checked) {
                    flags |= FLAG_SCHMITT;
                } else {
                    flags &= ~FLAG_SCHMITT;
                }
            }
            CirSim.Sim.UpdateModels();
        }

        void parseRules() {
            var lines = rules.Replace("\r", "").Split('\n');
            int i;
            rulesLeft = new List<string>();
            rulesRight = new List<string>();
            triState = false;
            for (i = 0; i != lines.Length; i++) {
                string s = lines[i].ToLower();
                if (s.Length == 0 || s.StartsWith("#")) {
                    continue;
                }
                var s0 = s.Replace(" ", "").Split('=');
                if (s0.Length != 2) {
                    MessageBox.Show("Error on line " + (i + 1) + " of model description");
                    return;
                }
                if (s0[0].Length < inputs.Length) {
                    MessageBox.Show("Model must have >= " + inputs.Length + " digits on left side");
                    return;
                }
                if (s0[0].Length > inputs.Length + outputs.Length) {
                    MessageBox.Show("Model must have <= " + (inputs.Length + outputs.Length) + " digits on left side");
                    return;
                }
                if (s0[1].Length != outputs.Length) {
                    MessageBox.Show("Model must have " + outputs.Length + " digits on right side");
                    return;
                }
                string rl = s0[0];
                var used = new bool[26];
                int j;
                string newRl = "";
                for (j = 0; j != rl.Length; j++) {
                    char x = rl.ElementAt(j);
                    if (x == '?' || x == '+' || x == '-' || x == '0' || x == '1') {
                        newRl += x;
                        continue;
                    }
                    if (x < 'a' || x > 'z') {
                        MessageBox.Show("Error on line " + (i + 1) + " of model description");
                        return;
                    }
                    /* if a letter appears twice, capitalize it the 2nd time so we can compare */
                    if (used[x - 'a']) {
                        newRl += (char)(x + 'A' - 'a');
                        continue;
                    }
                    used[x - 'a'] = true;
                    newRl += x;
                }
                string rr = s0[1];
                if (rr.Contains("_")) {
                    triState = true;
                }
                rulesLeft.Add(newRl);
                rulesRight.Add(s0[1]);
            }
        }

        public string dump() {
            dumped = true;
            if (rules.Length > 0 && !rules.EndsWith("\n")) {
                rules += "\n";
            }
            return "! " + escape(name)
                + " " + flags
                + " " + escape(arrayToList(inputs))
                + " " + escape(arrayToList(outputs))
                + " " + escape(infoText)
                + " " + escape(rules);
        }

        public static string escape(string s) {
            if (s.Length == 0) {
                return "\\0";
            }
            return s.Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("+", "\\p")
                .Replace("=", "\\q")
                .Replace("#", "\\h")
                .Replace("&", "\\a")
                .Replace("\r", "\\r")
                .Replace(" ", "\\s");
        }

        public static string unescape(string s) {
            if (s == "\\0") {
                return "";
            }
            for (int i = 0; i < s.Length; i++) {
                if (s.ElementAt(i) == '\\') {
                    char c = s.ElementAt(i + 1);
                    if (c == 'n') {
                        s = s.Substring(0, i) + "\n" + s.Substring(i + 2);
                    } else if (c == 'r') {
                        s = s.Substring(0, i) + "\r" + s.Substring(i + 2);
                    } else if (c == 's') {
                        s = s.Substring(0, i) + " " + s.Substring(i + 2);
                    } else if (c == 'p') {
                        s = s.Substring(0, i) + "+" + s.Substring(i + 2);
                    } else if (c == 'q') {
                        s = s.Substring(0, i) + "=" + s.Substring(i + 2);
                    } else if (c == 'h') {
                        s = s.Substring(0, i) + "#" + s.Substring(i + 2);
                    } else if (c == 'a') {
                        s = s.Substring(0, i) + "&" + s.Substring(i + 2);
                    } else {
                        s = s.Substring(0, i) + s.Substring(i + 1);
                    }
                }
            }
            return s;
        }
    }
}
