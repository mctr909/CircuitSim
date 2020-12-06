using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    interface Editable {
        EditInfo GetEditInfo(int n);
        void SetEditValue(int n, EditInfo ei);
    }

    class EditDialog : Form {
        const int barmax = 1000;
        const double ROOT2 = 1.41421356237309504880;

        Editable elm;
        CirSim cframe;
        Button applyButton;
        Button cancelButton;
        EditInfo[] einfos;

        int einfocount;

        Panel vp;
        Panel hp;
        bool closeOnEnter = true;

        public EditDialog(Editable ce, CirSim f) : base() {
            Text = "Edit Component";
            cframe = f;
            elm = ce;

            einfos = new EditInfo[10];

            SuspendLayout();

            vp = new Panel();
            Controls.Add(vp);

            hp = new Panel();
            {
                /* Apply */
                hp.Controls.Add(applyButton = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Text = "Apply"
                });
                applyButton.Click += new EventHandler((s, e) => {
                    apply();
                    closeDialog();
                });
                /* Cancel */
                hp.Controls.Add(cancelButton = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Left = applyButton.Right + 4,
                    Text = "Cancel"
                });
                cancelButton.Click += new EventHandler((s, e) => {
                    closeDialog();
                });
                /* */
                hp.Width = cancelButton.Right;
                hp.Height = cancelButton.Height;
                vp.Controls.Add(hp);
            }

            buildDialog();

            ResumeLayout(false);
        }

        public void Show(int x, int y) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Visible = false;
            Show();
            Location = new Point(x - Width / 2, y + Height / 2);
            Visible = true;
        }

        void buildDialog() {
            int i;
            int idx;
            for (i = 0; ; i++) {
                einfos[i] = elm.GetEditInfo(i);
                if (einfos[i] == null) {
                    break;
                }
                var ei = einfos[i];
                idx = vp.Controls.IndexOf(hp);
                insertCtrl(vp, new Label() {
                    Text = ei.Name,
                    AutoSize = true,
                    TextAlign = ContentAlignment.BottomLeft
                }, idx);
                idx = vp.Controls.IndexOf(hp);
                if (ei.Choice != null) {
                    ei.Choice.AutoSize = true;
                    ei.Choice.SelectedValueChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(vp, ei.Choice, idx);
                } else if (ei.CheckBox != null) {
                    ei.CheckBox.AutoSize = true;
                    ei.CheckBox.CheckedChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(vp, ei.CheckBox, idx);
                } else if (ei.Button != null) {
                    ei.Button.AutoSize = true;
                    ei.Button.Click += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(vp, ei.Button, idx);
                } else if (ei.TextArea != null) {
                    insertCtrl(vp, ei.TextArea, idx);
                    closeOnEnter = false;
                } else if (ei.widget != null) {
                    insertCtrl(vp, ei.widget, idx);
                } else {
                    insertCtrl(vp, ei.Textf = new TextBox(), idx);
                    if (ei.Text != null) {
                        ei.Textf.Text = ei.Text;
                    }
                    if (ei.Text == null) {
                        ei.Textf.Text = unitString(ei);
                    }
                }
            }
            einfocount = i;
            Width = vp.Width + 20;
            Height = vp.Height + 35;
        }

        void insertCtrl(Control parent, Control ctrl, int idx) {
            var tmp = new List<Control>();
            for (int i = 0; i < idx; i++) {
                tmp.Add(parent.Controls[i]);
            }
            tmp.Add(ctrl);
            for (int i = idx; i < parent.Controls.Count; i++) {
                tmp.Add(parent.Controls[i]);
            }
            /* */
            int ofsY = 0;
            int maxX = 0;
            parent.Controls.Clear();
            foreach (var c in tmp) {
                c.Left = 4;
                c.Top = ofsY;
                parent.Controls.Add(c);
                if (c is Label) {
                    ofsY += c.Height;
                } else {
                    ofsY += c.Height + 8;
                }
                maxX = Math.Max(maxX, c.Right);
            }
            parent.Width = maxX;
            parent.Height = ofsY;
            tmp.Clear();
        }

        double diffFromInteger(double x) {
            return Math.Abs(x - Math.Round(x));
        }

        public string unitString(EditInfo ei) {
            /* for voltage elements, express values in rms if that would be shorter */
            if (elm != null && (elm is VoltageElm)
                && Math.Abs(ei.Value) > 1e-4
                && diffFromInteger(ei.Value * 1e4) > diffFromInteger(ei.Value * 1e4 / ROOT2)) {
                return unitString(ei, ei.Value / ROOT2) + "rms";
            }
            return unitString(ei, ei.Value);
        }

        public static string unitString(EditInfo ei, double v) {
            double va = Math.Abs(v);
            if (ei != null && ei.Dimensionless) {
                return (v).ToString();
            }
            if (v == 0) {
                return "0";
            }
            if (va < 1e-9) {
                return (v * 1e12).ToString("0") + "p";
            }
            if (va < 1e-6) {
                return (v * 1e9).ToString("0") + "n";
            }
            if (va < 1e-3) {
                return (v * 1e6).ToString("0") + "u";
            }
            if (va < 1) { /*&& !ei.forceLargeM*/
                return (v * 1e3).ToString("0") + "m";
            }
            if (va < 1e3) {
                return (v).ToString("0");
            }
            if (va < 1e6) {
                return (v * 1e-3).ToString("0") + "k";
            }
            if (va < 1e9) {
                return (v * 1e-6).ToString("0") + "M";
            }
            return (v * 1e-9).ToString("0") + "G";
        }

        double parseUnits(EditInfo ei) {
            string s = ei.Textf.Text;
            return parseUnits(s);
        }

        public static double parseUnits(string s) {
            s = s.Trim();
            double rmsMult = 1;
            if (s.EndsWith("rms")) {
                s = s.Substring(0, s.Length - 3).Trim();
                rmsMult = ROOT2;
            }
            /* rewrite shorthand (eg "2k2") in to normal format (eg 2.2k) using regex */
            var rg = new Regex("([0-9]+)([pPnNuUmMkKgG])([0-9]+)");
            s = rg.Replace(s, "$1.$3$2");
            int len = s.Length;
            char uc = s.ElementAt(len - 1);
            double mult = 1;
            switch (uc) {
            case 'p': case 'P': mult = 1e-12; break;
            case 'n': case 'N': mult = 1e-9; break;
            case 'u': case 'U': mult = 1e-6; break;
            /* for ohm values, we used to assume mega for lowercase m, otherwise milli */
            case 'm': mult = 1e-3; break; /*(ei.forceLargeM) ? 1e6 : */
            case 'k': case 'K': mult = 1e3; break;
            case 'M': mult = 1e6; break;
            case 'G': case 'g': mult = 1e9; break;
            }
            if (mult != 1) {
                s = s.Substring(0, len - 1).Trim();
            }
            return double.Parse(s) * mult * rmsMult;
        }

        void apply() {
            int i;
            for (i = 0; i != einfocount; i++) {
                var ei = einfos[i];
                if (ei.Textf != null && ei.Text == null) {
                    try {
                        double d = parseUnits(ei);
                        ei.Value = d;
                    } catch (FormatException ex) {
                        MessageBox.Show(ex.Message);
                    } catch (Exception ex) {
                        throw ex;
                    }
                }
                if (ei.Button != null) {
                    continue;
                }
                elm.SetEditValue(i, ei);

                /* update slider if any */
                if (elm is CircuitElm) {
                    var adj = cframe.findAdjustable((CircuitElm)elm, i);
                    if (adj != null) {
                        adj.setSliderValue(ei.Value);
                    }
                }
            }
            cframe.needAnalyze();
        }

        void itemStateChanged(object sender) {
            int i;
            bool changed = false;
            bool applied = false;
            for (i = 0; i != einfocount; i++) {
                var ei = einfos[i];
                if (ei.Choice == sender || ei.CheckBox == sender || ei.Button == sender) {
                    /* if we're pressing a button, make sure to apply changes first */
                    if (ei.Button == sender && !ei.NewDialog) {
                        apply();
                        applied = true;
                    }
                    elm.SetEditValue(i, ei);
                    if (ei.NewDialog) {
                        changed = true;
                    }
                    cframe.needAnalyze();
                }
            }
            if (changed) {
                /* apply changes before we reset everything
                 * (need to check if we already applied changes; otherwise Diode create simple model button doesn't work) */
                if (!applied) {
                    apply();
                }
                SuspendLayout();
                clearDialog();
                buildDialog();
                ResumeLayout(false);
            }
        }

        void clearDialog() {
            while (vp.Controls[0] != hp) {
                vp.Controls.RemoveAt(0);
            }
        }

        public void closeDialog() {
            Close();
            CirSim.editDialog = null;
        }

        public void enterPressed() {
            if (closeOnEnter) {
                apply();
                closeDialog();
            }
        }
    }
}
