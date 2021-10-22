using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Input;

namespace Circuit {
    interface Editable {
        ElementInfo GetElementInfo(int n);
        void SetElementValue(int n, ElementInfo ei);
    }

    class ElementInfoDialog : Form {
        const double ROOT2 = 1.41421356237309504880;

        Editable mElm;
        Button mBtnApply;
        Button mBtnCancel;
        ElementInfo[] mEInfos;

        int mEInfoCount;

        Panel mPnlV;
        Panel mPnlH;
        bool mCloseOnEnter = true;

        public ElementInfoDialog(Editable ce) : base() {
            Text = "Edit Component";
            mElm = ce;

            mEInfos = new ElementInfo[10];

            SuspendLayout();

            mPnlV = new Panel();
            Controls.Add(mPnlV);

            mPnlH = new Panel();
            {
                /* Apply */
                mPnlH.Controls.Add(mBtnApply = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Text = "Apply"
                });
                mBtnApply.Click += new EventHandler((s, e) => {
                    apply();
                    Close();
                });
                /* Cancel */
                mPnlH.Controls.Add(mBtnCancel = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Left = mBtnApply.Right + 4,
                    Text = "Cancel"
                });
                mBtnCancel.Click += new EventHandler((s, e) => {
                    Close();
                });
                /* */
                mPnlH.Width = mBtnCancel.Right;
                mPnlH.Height = mBtnCancel.Height;
                mPnlV.Controls.Add(mPnlH);
            }

            buildDialog();

            ResumeLayout(false);
        }

        public void Show(int x, int y) {
            Visible = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Show();

            x -= Width / 2;
            if (x < 0) {
                x = 0;
            }
            if (y < 0) {
                y = 0;
            }
            Location = new Point(x, y);
            Visible = true;
        }

        public new void Close() {
            base.Close();
            CirSim.EditDialog = null;
        }

        public void EnterPressed() {
            if (mCloseOnEnter) {
                apply();
                Close();
            }
        }

        public static string UnitString(ElementInfo ei, double v) {
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
                return (v).ToString("0.##");
            }
            if (va < 1e6) {
                return (v * 1e-3).ToString("0") + "k";
            }
            if (va < 1e9) {
                return (v * 1e-6).ToString("0") + "M";
            }
            return (v * 1e-9).ToString("0") + "G";
        }

        public static double ParseUnits(string s) {
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
            case 'p':
            case 'P':
                mult = 1e-12;
                break;
            case 'n':
            case 'N':
                mult = 1e-9;
                break;
            case 'u':
            case 'U':
                mult = 1e-6;
                break;
            /* for ohm values, we used to assume mega for lowercase m, otherwise milli */
            case 'm':
                mult = 1e-3;
                break; /*(ei.forceLargeM) ? 1e6 : */
            case 'k':
            case 'K':
                mult = 1e3;
                break;
            case 'M':
                mult = 1e6;
                break;
            case 'G':
            case 'g':
                mult = 1e9;
                break;
            }
            if (mult != 1) {
                s = s.Substring(0, len - 1).Trim();
            }
            return double.Parse(s) * mult * rmsMult;
        }

        void apply() {
            for (int i = 0; i != mEInfoCount; i++) {
                var ei = mEInfos[i];
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
                mElm.SetElementValue(i, ei);

                /* update slider if any */
                if (mElm is CircuitElm) {
                    var adj = CirSim.Sim.FindAdjustable((CircuitElm)mElm, i);
                    if (adj != null) {
                        adj.Value = ei.Value;
                    }
                }
            }
            CirSim.Sim.NeedAnalyze();
        }

        void itemStateChanged(object sender) {
            bool changed = false;
            bool applied = false;
            for (int i = 0; i != mEInfoCount; i++) {
                var ei = mEInfos[i];
                if (ei.Choice == sender || ei.CheckBox == sender || ei.Button == sender) {
                    /* if we're pressing a button, make sure to apply changes first */
                    if (ei.Button == sender && !ei.NewDialog) {
                        apply();
                        applied = true;
                    }
                    mElm.SetElementValue(i, ei);
                    if (ei.NewDialog) {
                        changed = true;
                    }
                    CirSim.Sim.NeedAnalyze();
                }
            }
            if (changed) {
                /* apply changes before we reset everything
                 * (need to check if we already applied changes; otherwise Diode create simple model button doesn't work) */
                if (!applied) {
                    apply();
                }
                SuspendLayout();
                clear();
                buildDialog();
                ResumeLayout(false);
            }
        }

        void clear() {
            while (mPnlV.Controls[0] != mPnlH) {
                mPnlV.Controls.RemoveAt(0);
            }
        }

        void buildDialog() {
            int idx;
            int i;
            for (i = 0; ; i++) {
                mEInfos[i] = mElm.GetElementInfo(i);
                if (mEInfos[i] == null) {
                    break;
                }
                var ei = mEInfos[i];
                idx = mPnlV.Controls.IndexOf(mPnlH);
                if (0 <= ei.Name.IndexOf("<a")) {
                    var name = ei.Name.Replace(" ", "");
                    string title = "";
                    var title0 = name.IndexOf(">") + ">".Length;
                    if (0 <= title0) {
                        var title1 = name.IndexOf("</a>", title0);
                        title = name.Substring(title0, title1 - title0);
                    }
                    var label = new LinkLabel() {
                        Text = title,
                        AutoSize = true,
                        TextAlign = ContentAlignment.BottomLeft
                    };
                    var href0 = name.IndexOf("href=\"") + "href=\"".Length;
                    if (0 <= href0) {
                        var href1 = name.IndexOf("\"", href0);
                        var href = name.Substring(href0, href1 - href0);
                        label.Click += new EventHandler((s, e) => {
                            Process.Start(href);
                        });
                    }
                    insertCtrl(mPnlV, label, idx);
                } else {
                    insertCtrl(mPnlV, new Label() {
                        Text = ei.Name,
                        AutoSize = true,
                        TextAlign = ContentAlignment.BottomLeft
                    }, idx);
                }
                idx = mPnlV.Controls.IndexOf(mPnlH);
                if (ei.Choice != null) {
                    ei.Choice.AutoSize = true;
                    ei.Choice.SelectedValueChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlV, ei.Choice, idx);
                } else if (ei.CheckBox != null) {
                    ei.CheckBox.AutoSize = true;
                    ei.CheckBox.CheckedChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlV, ei.CheckBox, idx);
                } else if (ei.Button != null) {
                    ei.Button.AutoSize = true;
                    ei.Button.Click += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlV, ei.Button, idx);
                } else if (ei.TextArea != null) {
                    insertCtrl(mPnlV, ei.TextArea, idx);
                    mCloseOnEnter = false;
                } else {
                    insertCtrl(mPnlV, ei.Textf = new TextBox(), idx);
                    if (ei.Text != null) {
                        ei.Textf.Text = ei.Text;
                    }
                    if (ei.Text == null) {
                        ei.Textf.Text = unitString(ei);
                    }
                }
            }
            mEInfoCount = i;
            Width = mPnlV.Width + 20;
            Height = mPnlV.Height + 35;
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

        string unitString(ElementInfo ei) {
            /* for voltage elements, express values in rms if that would be shorter */
            if (mElm != null && (mElm is VoltageElm)
                && Math.Abs(ei.Value) > 1e-4
                && diffFromInteger(ei.Value * 1e4) > diffFromInteger(ei.Value * 1e4 / ROOT2)) {
                return UnitString(ei, ei.Value / ROOT2) + "rms";
            }
            return UnitString(ei, ei.Value);
        }

        double parseUnits(ElementInfo ei) {
            string s = ei.Textf.Text;
            return ParseUnits(s);
        }
    }
}
