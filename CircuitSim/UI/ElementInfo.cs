using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.UI {
    public class ElementInfo {
        public string Name { get; private set; }
        public string Text { get; set; }
        public double Value { get; set; }

        public bool NewDialog { get; set; }

        public ComboBox Choice;
        public CheckBox CheckBox;

        internal TextBox TextString;
        internal TextBox TextDouble;
        internal TextBox TextInt;
        internal Button Button;

        internal TextBox MinBox;
        internal TextBox MaxBox;
        internal TextBox LabelBox;

        readonly Font mTextFont = new Font("Arial", 11);
        bool mEnableSliders = false;

        public ElementInfo(string name = "") {
            Name = name;
        }

        public ElementInfo(string textTitle, string text, bool multiLine = false) {
            Name = textTitle;
            Value = 0;
            Text = text;
            if (multiLine) {
                TextString = new TextBox() {
                    Multiline = true,
                    Height = 100,
                    Width = 300,
                    ScrollBars = ScrollBars.Vertical
                };
            } else {
                TextString = new TextBox();
            }
            TextString.Font = mTextFont;
            TextString.Text = text;
        }

        public ElementInfo(string valueName, double val, bool enableSliders = true) {
            Name = valueName;
            Value = val;
            TextDouble = new TextBox() {
                Text = val.ToString(),
                Width = 50,
                TextAlign = HorizontalAlignment.Right
            };
            TextDouble.Font = mTextFont;
            mEnableSliders = enableSliders;
        }

        public ElementInfo(string valueName, int val) {
            Name = valueName;
            Value = val;
            TextInt = new TextBox()
            {
                Text = val.ToString(),
                Width = 60
            };
            TextInt.Font = mTextFont;
        }

        public ElementInfo(string buttonName, EventHandler e) {
            Button = new Button()
            {
                AutoSize = true,
                Text = buttonName
            };
            Button.Click += e;
        }

        public ElementInfo(string checkboxName, bool val) {
            Name = checkboxName;
            Value = 0;
            CheckBox = new CheckBox() {
                AutoSize = true,
                Text = checkboxName,
                Checked = val
            };
        }

        public ElementInfo(string listName, int defaultIndex, string[] val) {
            Name = listName;
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
            return mEnableSliders;
        }
    }
}
