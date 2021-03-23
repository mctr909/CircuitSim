using System;
using System.Windows.Forms;

namespace Circuit {
    class InputDialog : Form {
        public string Value { get; private set; }

        TextBox mTxt;

        public InputDialog(string title = "", string defaultValue = "") {
            Text = title;
            mTxt = new TextBox() { Width = 200, Text = defaultValue };
            Controls.Add(mTxt);
            var btn = new Button() { Text = "確定" };
            btn.Click += new EventHandler((s, e) => {
                Value = mTxt.Text;
                Close();
            });
            Controls.Add(btn);
            StartPosition = FormStartPosition.CenterParent;
            Show();
        }
    }
}
