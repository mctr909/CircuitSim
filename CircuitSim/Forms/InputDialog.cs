using System;
using System.Windows.Forms;

namespace Circuit {
    class InputDialog : Form {
        TextBox txb;
        public string Value;

        public InputDialog(string title, string defaultValue = "") {
            Text = title;
            txb = new TextBox() { Width = 200, Text = defaultValue };
            Controls.Add(txb);
            var btn = new Button() { Text = "確定" };
            btn.Click += new EventHandler((s, e) => {
                Value = txb.Text;
                Close();
            });
            Controls.Add(btn);
            StartPosition = FormStartPosition.CenterParent;
            Show();
        }
    }
}
