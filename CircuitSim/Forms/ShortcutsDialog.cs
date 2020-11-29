using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Circuit {
    class ShortcutsDialog : Form {
        CirSim mSim;
        TextBox textArea;
        List<TextBox> textBoxes;
        Button okButton;

        public ShortcutsDialog(CirSim asim) : base() {
            mSim = asim;
            Button cancelButton;

            Text = "Edit Shortcuts";

            var sp = new Panel();
            {
                string bkGroup = "";
                sp.AutoScroll = true;
                sp.SuspendLayout();
                /* */
                var ofsY = 4;
                textBoxes = new List<TextBox>();
                for (int i = 0; i != mSim.Menu.mainMenuItems.Count; i++) {
                    var item = mSim.Menu.mainMenuItems[i];
                    /* group */
                    string group = item.OwnerItem.Text;
                    if (group != bkGroup) {
                        ofsY += 8;
                        var grp = new Label() {
                            Text = group,
                            AutoSize = true,
                            TextAlign = ContentAlignment.BottomLeft,
                            Left = 4,
                            Top = ofsY
                        };
                        sp.Controls.Add(grp);
                        ofsY += grp.Height;
                        bkGroup = group;
                    }
                    /* label */
                    var lbl = new Label() {
                        Text = item.Text,
                        AutoSize = true,
                        TextAlign = ContentAlignment.BottomLeft,
                        Left = 16,
                        Top = ofsY
                    };
                    sp.Controls.Add(lbl);
                    ofsY += lbl.Height;
                    /* text box */
                    var text = new TextBox() {
                        Text = item.ShortcutKeys == Keys.None ? "" : item.ShortcutKeys.ToString(),
                        MaxLength = 1,
                        Left = 16,
                        Top = ofsY
                    };
                    text.TextChanged += new EventHandler((s, e) => {
                        checkForDuplicates();
                    });
                    textBoxes.Add(text);
                    sp.Controls.Add(text);
                    ofsY += text.Height + 8;
                }
                /* */
                sp.Left = 4;
                sp.Top = 4;
                sp.Height = 320;
                sp.BorderStyle = BorderStyle.Fixed3D;
                sp.ResumeLayout(false);
                Controls.Add(sp);
            }

            var hp = new Panel();
            {
                hp.AutoSize = true;
                /* OK */
                okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((sender, e) => {
                    int i;
                    if (checkForDuplicates()) {
                        return;
                    }
                    /* clear existing shortcuts */
                    for (i = 0; i != mSim.Menu.shortcuts.Length; i++) {
                        mSim.Menu.shortcuts[i] = MENU_ITEM.INVALID;
                    }
                    /* load new ones */
                    for (i = 0; i != textBoxes.Count; i++) {
                        string str = textBoxes[i].Text;
                        var item = mSim.Menu.mainMenuItems[i];
                        // TODO: ShortcutsDialog
                        //item.ShortcutKeys = str;
                        if (str.Length > 0) {
                            mSim.Menu.shortcuts[str.ElementAt(0)] = mSim.Menu.mainMenuItemNames[i];
                        }
                    }
                    /* save to local storage */
                    mSim.Menu.saveShortcuts();
                    closeDialog();
                });
                hp.Controls.Add(okButton);
                /* Cancel */
                cancelButton = new Button() { Text = "Cancel" };
                cancelButton.Click += new EventHandler((sender, e) => { closeDialog(); });
                hp.Controls.Add(cancelButton);
                /* */
                hp.Left = 4;
                hp.Top = sp.Bottom + 4;
                hp.Height = cancelButton.Bottom + 4;
                Controls.Add(hp);
            }

            Width = hp.Right + 24;
            Height = hp.Bottom + 38;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterParent;
        }

        bool checkForDuplicates() {
            var boxForShortcut = new TextBox[127];
            bool result = false;
            int i;
            for (i = 0; i != textBoxes.Count; i++) {
                var box = textBoxes[i];
                string str = box.Text;
                if (str.Length == 0) {
                    continue;
                }
                char c = str.ElementAt(0);

                /* check if character if out of range */
                if (c > boxForShortcut.Length) {
                    box.BackColor = Color.Red;
                    result = true;
                    continue;
                }

                /* check for duplicates and mark them */
                if (boxForShortcut[c] != null) {
                    box.BackColor = Color.Red;
                    boxForShortcut[c].BackColor = Color.Red;
                    result = true;
                }
                boxForShortcut[c] = box;
            }
            okButton.Enabled = !result;
            return result;
        }

        void closeDialog() {
            Close();
        }
    }
}
