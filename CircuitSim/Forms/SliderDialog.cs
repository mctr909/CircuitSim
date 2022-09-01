using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    public class SliderDialog : Form {
        BaseUI elm;
        CirSimForm sim;
        Button okButton;
        Button cancelButton;
        ElementInfo[] einfos;
        int einfocount;
        Panel vp;
        Panel hp;

        public SliderDialog(BaseUI ce, CirSimForm f) : base() {
            Text = "Add Sliders";
            sim = f;
            elm = ce;

            vp = new Panel();

            einfos = new ElementInfo[10];
            hp = new Panel();
            {
                hp.AutoSize = true;
                /* Apply */
                okButton = new Button() {
                    Left = 0,
                    Width = 50,
                    Text = "Apply"
                };
                okButton.Click += new EventHandler((sender, e) => {
                    apply();
                    closeDialog();
                });
                hp.Controls.Add(okButton);
                /* Cancel */
                cancelButton = new Button() {
                    Left = okButton.Right + 4,
                    Width = 50,
                    Text = "Cancel"
                };
                cancelButton.Click += new EventHandler((sender, e) => {
                    closeDialog();
                });
                hp.Controls.Add(cancelButton);
            }

            /* */
            vp.Controls.Add(hp);

            /* */
            vp.Left = 4;
            vp.Top = 4;
            Controls.Add(vp);

            /* */
            buildDialog();
            Width = vp.Width + 24;
            Height = vp.Height + 64;
            Visible = false;
        }

        public void Show(int x, int y) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Show();
            Left = x - Width / 2;
            Top = y - Height / 2;
            Visible = true;
        }

        void buildDialog() {
            int i;
            int idx;
            vp.SuspendLayout();
            for (i = 0; ; i++) {
                var ei = elm.GetElementInfo(i, 0);
                if (ei == null) {
                    break;
                }
                einfos[i] = ei;

                if (!ei.CanCreateAdjustable()) {
                    continue;
                }
                var adj = findAdjustable(i);
                string name = ei.Name;
                idx = vp.Controls.IndexOf(hp);

                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = name,
                    Checked = adj != null
                };
                ctrlInsert(vp, ei.CheckBox, idx++);
                ei.CheckBox.CheckedChanged += new EventHandler((sender, e) => { itemStateChanged(sender); });

                if (adj != null) {
                    ctrlInsert(vp, new Label() {
                        TextAlign = ContentAlignment.BottomLeft,
                        Text = "名前"
                    }, idx++);
                    ei.LabelBox = new TextBox() {
                        Text = adj.SliderText,
                        Width = 120
                    };
                    ctrlInsert(vp, ei.LabelBox, idx++);
                    ctrlInsert(vp, new Label() {
                        TextAlign = ContentAlignment.BottomLeft,
                        Text = "最小値"
                    }, idx++);
                    ei.MinBox = new TextBox() {
                        Text = ElementInfoDialog.UnitString(ei, adj.MinValue),
                        Width = 50
                    };
                    ctrlInsert(vp, ei.MinBox, idx++);
                    ctrlInsert(vp, new Label() {
                        TextAlign = ContentAlignment.BottomLeft,
                        Text = "最大値"
                    }, idx++);
                    ei.MaxBox = new TextBox() {
                        Text = ElementInfoDialog.UnitString(ei, adj.MaxValue),
                        Width = 50
                    };
                    ctrlInsert(vp, ei.MaxBox, idx++);
                }
            }
            vp.ResumeLayout(false);
            einfocount = i;
        }

        Adjustable findAdjustable(int item) {
            return sim.FindAdjustable(elm, item);
        }

        void apply() {
            int i;
            for (i = 0; i != einfocount; i++) {
                var adj = findAdjustable(i);
                if (adj == null) {
                    continue;
                }
                var ei = einfos[i];
                if (ei.LabelBox == null) {  // haven't created UI yet?
                    continue;
                }
                try {
                    adj.SliderText = ei.LabelBox.Text;
                    adj.Label.Text = adj.SliderText;
                    double d = ElementInfoDialog.ParseUnits(ei.MinBox.Text);
                    adj.MinValue = d;
                    d = ElementInfoDialog.ParseUnits(ei.MaxBox.Text);
                    adj.MaxValue = d;
                    adj.Value = ei.Value;
                } catch { }
            }
        }

        public void itemStateChanged(object sender) {
            int i;
            bool changed = false;
            for (i = 0; i != einfocount; i++) {
                var ei = einfos[i];
                if (ei.CheckBox == sender) {
                    apply();
                    if (ei.CheckBox.Checked) {
                        var adj = new Adjustable(elm, i);
                        var rg = new Regex(" \\(.*\\)$");
                        adj.SliderText = rg.Replace(ei.Name, "");
                        adj.CreateSlider(ei);
                        sim.Adjustables.Add(adj);
                    } else {
                        var adj = findAdjustable(i);
                        adj.DeleteSlider();
                        sim.Adjustables.Remove(adj);
                    }
                    changed = true;
                }
            }
            if (changed) {
                /* apply changes before we reset everything */
                apply();
                clearDialog();
                buildDialog();
                Width = vp.Width + 24;
                Height = vp.Height + 64;
            }
        }

        public void clearDialog() {
            while (vp.Controls[0] != hp) {
                vp.Controls.RemoveAt(0);
            }
        }

        public void closeDialog() {
            Close();
            CirSimForm.SliderDialog = null;
        }

        void ctrlInsert(Panel p, Control ctrl, int idx) {
            var tmp = new List<Control>();
            for (int i = 0; i < idx; i++) {
                tmp.Add(p.Controls[i]);
            }
            tmp.Add(ctrl);
            for (int i = idx; i < p.Controls.Count; i++) {
                tmp.Add(p.Controls[i]);
            }
            p.Controls.Clear();
            var ofsY = 4;
            var width = 0;
            for (int i=0; i<tmp.Count; i++) {
                tmp[i].Left = 4;
                tmp[i].Top = ofsY;
                p.Controls.Add(tmp[i]);
                ofsY += tmp[i].Height;
                if (width < tmp[i].Width) {
                    width = tmp[i].Width;
                }
            }
            p.Width = width + 4;
            p.Height = ofsY + 4;
            tmp.Clear();
        }
    }
}
