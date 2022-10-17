using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit {
    public class ElementInfo {
        public string Name { get; private set; }
        public string Text { get; set; }
        public double Value { get; set; }
        public bool NoSliders { get; private set; } = true;
        public bool NewDialog { get; set; }

        public ComboBox Choice;
        public CheckBox CheckBox;
        public Button Button;
        public TextBox Textf;

        public TextBox MinBox;
        public TextBox MaxBox;
        public TextBox LabelBox;

        public ElementInfo(string name = "") {
            Name = name;
        }

        public ElementInfo(string name, string text, bool multiLine = false) {
            Name = name;
            Value = 0;
            Text = text;
            if (multiLine) {
                Textf = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    Width = 300,
                    ScrollBars = ScrollBars.Vertical,
                    Text = text
                };
                Textf.Font = new Font("Arial", 9);
            } else {
                Textf = new TextBox() {
                    Text = text
                };
                Textf.Font = new Font("Arial", 9);
            }
        }

        public ElementInfo(string name, EventHandler e) {
            Button = new Button() {
                AutoSize = true,
                Text = name
            };
            Button.Click += e;
        }

        public ElementInfo(string name, double val, bool noSliders = false) {
            Name = name;
            Value = val;
            Textf = new TextBox() {
                Text = val.ToString()
            };
            Textf.Font = new Font("Arial", 9);
            NoSliders = noSliders;
        }

        public ElementInfo(string name, bool val) {
            Name = name;
            Value = 0;
            CheckBox = new CheckBox() {
                AutoSize = true,
                Text = name,
                Checked = val
            };
        }

        public ElementInfo(string name, int defaultIndex, string[] val) {
            Name = name;
            Value = 0;
            Choice = new ComboBox();
            Choice.AutoSize = true;
            foreach (var s in val) {
                Choice.Items.Add(s);
            }
            Choice.SelectedIndex = defaultIndex;
        }

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
                && !NoSliders;
        }
    }
}
