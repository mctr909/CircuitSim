using System.Windows.Forms;

namespace Circuit {
    class ElementInfo {
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

        public ElementInfo() { }

        public ElementInfo(string n, double val, double mn, double mx) {
            Name = n;
            Value = val;
            Dimensionless = false;
        }

        public ElementInfo(string n, double val) {
            Name = n;
            Value = val;
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

        public static string MakeLink(string file, string text) {
            return "<a href=\"" + file + "\" target=\"_blank\">" + text + "</a>";
        }
    }
}
