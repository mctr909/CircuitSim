using System.Windows.Forms;

namespace Circuit {
    class EditInfo {
        public string Name { get; private set; }
        public string Text { get; set; }
        public double Value { get; set; }
        public bool Dimensionless { get; private set; }
        public bool NoSliders { get; private set; }
        public bool NewDialog { get; set; }

        public Form widget;
        public ComboBox Choice;
        public CheckBox CheckBox;
        public Button Button;
        public TextBox TextArea;
        public TextBox Textf;

        /* for slider dialog */
        public TextBox MinBox;
        public TextBox MaxBox;
        public TextBox LabelBox;

        public EditInfo(string n, double val, double mn, double mx) {
            Name = n;
            Value = val;
            Dimensionless = false;
        }

        public EditInfo(string n, double val) {
            Name = n;
            Value = val;
            Dimensionless = false;
        }

        public EditInfo SetDimensionless() { Dimensionless = true; return this; }

        public EditInfo DisallowSliders() { NoSliders = true; return this; }

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
                && widget == null
                && !NoSliders;
        }

        public static string MakeLink(string file, string text) {
            return "<a href=\"" + file + "\" target=\"_blank\">" + text + "</a>";
        }
    }
}
