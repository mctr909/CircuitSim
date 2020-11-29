using System.Windows.Forms;

namespace Circuit {
    class EditInfo {
        public string name { get; private set; }
        public string text;
        public double value;
        public bool dimensionless;
        public bool noSliders;
        public bool newDialog;

        public Form widget;
        public ComboBox choice;
        public CheckBox checkbox;
        public Button button;
        public TextBox textArea;
        public TextBox textf;

        /* for slider dialog */
        public TextBox minBox;
        public TextBox maxBox;
        public TextBox labelBox;

        public EditInfo(string n, double val, double mn, double mx) {
            name = n;
            value = val;
            dimensionless = false;
        }

        public EditInfo(string n, double val) {
            name = n;
            value = val;
            dimensionless = false;
        }

        public EditInfo setDimensionless() { dimensionless = true; return this; }

        public EditInfo disallowSliders() { noSliders = true; return this; }

        public int changeFlag(int flags, int bit) {
            if (checkbox.Checked) {
                return flags | bit;
            }
            return flags & ~bit;
        }

        public bool canCreateAdjustable() {
            return choice == null &&
                checkbox == null &&
                button == null &&
                textArea == null &&
                widget == null &&
                !noSliders;
        }

        public static string makeLink(string file, string text) {
            return "<a href=\"" + file + "\" target=\"_blank\">" + text + "</a>";
        }
    }
}
