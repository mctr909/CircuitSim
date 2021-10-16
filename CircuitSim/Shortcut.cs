using System.Windows.Forms;

namespace Circuit {
    struct SHORTCUT {
        public Keys Key { get; private set; }
        public string Name { get; private set; }

        public SHORTCUT(Keys k, bool c = true, bool s = false, bool a = false) {
            Key = k;
            Name = "";
            if (k != Keys.None) {
                Name = name(k);
                if (a || 0 < (k & Keys.Alt)) {
                    Key |= Keys.Alt;
                    Name += " + Alt";
                }
                if (c || 0 < (k & Keys.Control)) {
                    Key |= Keys.Control;
                    Name += " + Ctrl";
                }
                if (s || 0 < (k & Keys.Shift)) {
                    Key |= Keys.Shift;
                    Name += " + Shift";
                }
            }
        }

        static string name(Keys k) {
            switch (k) {
            case Keys.D0:
            case Keys.D1:
            case Keys.D2:
            case Keys.D3:
            case Keys.D4:
            case Keys.D5:
            case Keys.D6:
            case Keys.D7:
            case Keys.D8:
            case Keys.D9:
                return k.ToString().Replace("D", "");
            case Keys.Delete:
                return "Del";
            case Keys.Oemplus:
                return "+";
            case Keys.OemMinus:
                return "-";
            default:
                return k.ToString();
            }
        }
    }
}
