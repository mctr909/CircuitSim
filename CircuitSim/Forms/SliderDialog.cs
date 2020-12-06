using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class SliderDialog : Form {
        const int barmax = 1000;

        CircuitElm elm;
        CirSim sim;
        Button applyButton;
        Button okButton;
        Button cancelButton;
        EditInfo[] einfos;
        int einfocount;
        Panel vp;
        Panel hp;

        public SliderDialog(CircuitElm ce, CirSim f) : base() {
            Text = "Add Sliders";
            sim = f;
            elm = ce;

            vp = new Panel();

            einfos = new EditInfo[10];
            hp = new Panel();
            {
                hp.AutoSize = true;
                /* Apply */
                applyButton = new Button() { Text = "Apply" };
                applyButton.Click += new EventHandler((sender, e) => { apply(); });
                ctrlInsert(hp, applyButton);
                /* OK */
                okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((sender, e) => {
                    apply();
                    closeDialog();
                });
                ctrlInsert(hp, okButton);
                /* Cancel */
                cancelButton = new Button() { Text = "Cancel" };
                cancelButton.Click += new EventHandler((sender, e) => {
                    closeDialog();
                });
                ctrlInsert(hp, cancelButton);
            }
            /* */
            ctrlInsert(vp, hp);
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
                einfos[i] = elm.GetEditInfo(i);
                if (einfos[i] == null) {
                    break;
                }
                var ei = einfos[i];
                if (!ei.CanCreateAdjustable()) {
                    continue;
                }
                var adj = findAdjustable(i);
                string name = ei.Name;
                idx = vp.Controls.IndexOf(hp);

                /* remove HTML */
                var rg = new Regex("<[^>]*>");
                name = rg.Replace(name, "");
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = name,
                    Checked = adj != null
                };
                ctrlInsert(vp, ei.CheckBox, idx++);
                ei.CheckBox.CheckedChanged += new EventHandler((sender, e) => { itemStateChanged(sender); });
                if (adj != null) {
                    ctrlInsert(vp, new Label() { TextAlign = ContentAlignment.BottomLeft, Text = "Min Value" }, idx++);
                    ei.MinBox = new TextBox() {
                        Text = EditDialog.unitString(ei, adj.minValue)
                    };
                    ctrlInsert(vp, ei.MinBox, idx++);
                    ctrlInsert(vp, new Label() { TextAlign = ContentAlignment.BottomLeft, Text = "Max Value" }, idx++);
                    ei.MaxBox = new TextBox() {
                        Text = EditDialog.unitString(ei, adj.maxValue)
                    };
                    ctrlInsert(vp, ei.MaxBox, idx++);
                    ctrlInsert(vp, new Label() { TextAlign = ContentAlignment.BottomLeft, Text = "Label" }, idx++);
                    ei.LabelBox = new TextBox() {
                        Text = adj.sliderText
                    };
                    ctrlInsert(vp, ei.LabelBox, idx++);
                }
            }
            vp.ResumeLayout(false);
            einfocount = i;
        }

        Adjustable findAdjustable(int item) {
            return sim.findAdjustable(elm, item);
        }

        void apply() {
            int i;
            for (i = 0; i != einfocount; i++) {
                var adj = findAdjustable(i);
                if (adj == null) {
                    continue;
                }
                var ei = einfos[i];
                /*if (ei.labelBox == null) {  haven't created UI yet?
                    continue;
                }*/
                try {
                    adj.sliderText = ei.LabelBox.Text;
                    adj.label.Text = adj.sliderText;
                    double d = EditDialog.parseUnits(ei.MinBox.Text);
                    adj.minValue = d;
                    d = EditDialog.parseUnits(ei.MaxBox.Text);
                    adj.maxValue = d;
                    adj.setSliderValue(ei.Value);
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
                        adj.sliderText = rg.Replace(ei.Name, "");
                        adj.createSlider(sim, ei.Value);
                        sim.adjustables.Add(adj);
                    } else {
                        var adj = findAdjustable(i);
                        adj.deleteSlider(sim);
                        sim.adjustables.Remove(adj);
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
            CirSim.sliderDialog = null;
        }

        void ctrlInsert(Panel p, Control ctrl) {
            var ofsY = 4;
            var width = 0;
            for (int i = 0; i < p.Controls.Count; i++) {
                ofsY += p.Controls[i].Height;
                if (width < p.Controls[i].Width) {
                    width = p.Controls[i].Width;
                }
            }
            ctrl.Left = 4;
            ctrl.Top = ofsY;
            p.Controls.Add(ctrl);
            p.Width = width + 4;
            p.Height = ofsY + 4;
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
