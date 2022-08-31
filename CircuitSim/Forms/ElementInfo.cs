using System.Windows.Forms;

namespace Circuit {
    public class ElementInfo {
        public string Name { get; private set; }
        public string Text { get; set; }
        public double Value { get; set; }
        public bool Dimensionless { get; private set; }
        public bool NoSliders { get; private set; }
        public bool NewDialog { get; set; }

        public ComboBox Choice;
        public CheckBox CheckBox;
        public Button Button;
        public TextBox TextArea;
        public TextBox Textf;

        /// <summary>
        /// for slider dialog 
        /// </summary>
        public TextBox MinBox;
        /// <summary>
        /// for slider dialog 
        /// </summary>
        public TextBox MaxBox;
        /// <summary>
        /// for slider dialog 
        /// </summary>
        public TextBox LabelBox;

        public ElementInfo(string n = "") {
            Name = n;
        }

        public ElementInfo(string n, double val) {
            Name = n;
            Value = val;
            Textf = new TextBox() {
                Text = val.ToString()
            };
            Dimensionless = false;
        }

        public ElementInfo(string n, double val, double mn, double mx) {
            Name = n;
            Value = val;
            Textf = new TextBox() {
                Text = val.ToString()
            };
            Dimensionless = false;
        }

        public ElementInfo(string n, bool val) {
            Name = n;
            Value = 0;
            CheckBox = new CheckBox() {
                AutoSize = true,
                Text = n,
                Checked = val
            };
            Dimensionless = false;
        }

        public ElementInfo(string n, string val, bool multiLine = false) {
            Name = n;
            Value = 0;
            Text = val;
            if (multiLine) {
                TextArea = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    Width = 250,
                    ScrollBars = ScrollBars.Vertical,
                    Text = val
                };
            } else {
                Textf = new TextBox();
                Textf.Text = val;
            }
            Dimensionless = false;
        }

        public ElementInfo(string n, int index, string[] val) {
            Name = n;
            Value = 0;
            Choice = new ComboBox();
            foreach (var s in val) {
                Choice.Items.Add(s);
            }
            Choice.SelectedIndex = index;
            Dimensionless = false;
        }

        public ElementInfo SetDimensionless() { Dimensionless = true; return this; }

        public ElementInfo DisallowSliders() { NoSliders = true; return this; }

        public int ChangeFlag(int flags, int bit) {
            if (CheckBox.Checked) {
                return flags | bit;
            }
            return flags & ~bit;
        }

        public bool CanCreateAdjustable() {
            return Choice == null
                && CheckBox == null
                && Button == null
                && TextArea == null
                && !NoSliders;
        }
    }
}
