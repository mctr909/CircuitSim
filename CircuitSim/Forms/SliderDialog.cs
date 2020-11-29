using System;
using System.Collections.Generic;
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
            vp.AutoScroll = true;

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
            Width = vp.Width + 24;
            Height = vp.Height + 64;
            buildDialog();
            StartPosition = FormStartPosition.CenterParent;
        }

        void buildDialog() {
            int i;
            int idx;
            vp.SuspendLayout();
            for (i = 0; ; i++) {
                einfos[i] = elm.getEditInfo(i);
                if (einfos[i] == null) {
                    break;
                }
                var ei = einfos[i];
                if (!ei.canCreateAdjustable()) {
                    continue;
                }
                var adj = findAdjustable(i);
                string name = ei.name;
                idx = vp.Controls.IndexOf(hp);

                /* remove HTML */
                var rg = new Regex("<[^>]*>");
                name = rg.Replace(name, "");
                ei.checkbox = new CheckBox() {
                    AutoSize = true,
                    Text = name,
                    Checked = adj != null
                };
                ctrlInsert(vp, ei.checkbox, idx++);
                ei.checkbox.CheckedChanged += new EventHandler((sender, e) => { itemStateChanged(sender); });
                if (adj != null) {
                    ctrlInsert(vp, new Label() { Text = "Min Value" }, idx++);
                    ei.minBox = new TextBox() {
                        Text = EditDialog.unitString(ei, adj.minValue)
                    };
                    ctrlInsert(vp, ei.minBox, idx++);
                    ctrlInsert(vp, new Label() { Text = "Max Value" }, idx++);
                    ei.maxBox = new TextBox() {
                        Text = EditDialog.unitString(ei, adj.maxValue)
                    };
                    ctrlInsert(vp, ei.maxBox, idx++);
                    ctrlInsert(vp, new Label() { Text = "Label" }, idx++);
                    ei.labelBox = new TextBox() {
                        Text = adj.sliderText
                    };
                    ctrlInsert(vp, ei.labelBox, idx++);
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
                    adj.sliderText = ei.labelBox.Text;
                    adj.label.Text = adj.sliderText;
                    double d = EditDialog.parseUnits(ei.minBox.Text);
                    adj.minValue = d;
                    d = EditDialog.parseUnits(ei.maxBox.Text);
                    adj.maxValue = d;
                    adj.setSliderValue(ei.value);
                } catch { }
            }
        }

        public void itemStateChanged(object sender) {
            int i;
            bool changed = false;
            for (i = 0; i != einfocount; i++) {
                var ei = einfos[i];
                if (ei.checkbox == sender) {
                    apply();
                    if (ei.checkbox.Checked) {
                        var adj = new Adjustable(elm, i);
                        var rg = new Regex(" \\(.*\\)$");
                        adj.sliderText = rg.Replace(ei.name, "");
                        adj.createSlider(sim, ei.value);
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
            for (int i = 0; i < p.Controls.Count; i++) {
                ofsY += p.Controls[i].Height + 4;
            }
            ctrl.Left = 4;
            ctrl.Top = ofsY;
            p.Controls.Add(ctrl);
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
            for(int i=0; i<tmp.Count; i++) {
                tmp[i].Left = 4;
                tmp[i].Top = ofsY;
                p.Controls.Add(tmp[i]);
                ofsY += tmp[i].Height + 4;
            }
            tmp.Clear();
        }
    }
}
