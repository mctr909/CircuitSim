﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Input;

namespace Circuit {
    public interface Editable {
        ElementInfo GetElementInfo(int r, int c);
        void SetElementValue(int n, ElementInfo ei);
    }

    public class ElementInfoDialog : Form {
        const double ROOT2 = 1.41421356237309504880;

        Editable mElm;
        Button mBtnApply;
        Button mBtnCancel;
        ElementInfo[] mEInfos;

        int mEInfoCount;
        int mMaxX = 0;
        int mOfsX = 0;
        int mOfsY = 0;

        Panel mPnlCustomCtrl;
        Panel mPnlCommonButtons;
        bool mCloseOnEnter = true;

        public ElementInfoDialog(Editable ce) : base() {
            Text = "Edit Component";
            mElm = ce;

            mEInfos = new ElementInfo[10];

            SuspendLayout();
            Visible = false;

            mPnlCommonButtons = new Panel();
            {
                /* Apply */
                mPnlCommonButtons.Controls.Add(mBtnApply = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Text = "Apply"
                });
                mBtnApply.Click += new EventHandler((s, e) => {
                    apply();
                    Close();
                });
                /* Cancel */
                mPnlCommonButtons.Controls.Add(mBtnCancel = new Button() {
                    AutoSize = true,
                    Width = 50,
                    Left = mBtnApply.Right + 4,
                    Text = "Cancel"
                });
                mBtnCancel.Click += new EventHandler((s, e) => {
                    Close();
                });
                /* */
                mPnlCommonButtons.Width = mBtnCancel.Right;
                mPnlCommonButtons.Height = mBtnCancel.Height;
                Controls.Add(mPnlCommonButtons);
            }

            mPnlCustomCtrl = new Panel();
            mPnlCustomCtrl.Width = 0;
            mPnlCustomCtrl.Height = 0;
            Controls.Add(mPnlCustomCtrl);
            buildDialog();

            ResumeLayout(false);
        }

        public void Show(int x, int y) {
            Visible = false;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Show();

            x -= Width / 2;
            if (x < 0) {
                x = 0;
            }
            if (y < 0) {
                y = 0;
            }
            Location = new Point(x, y);
            
            if (null == mEInfos[0]) {
                Close();
                Visible = false;
            } else {
                Visible = true;
            }
        }

        public new void Close() {
            base.Close();
            CirSimForm.EditDialog = null;
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
                return v.ToString();
            }
            return Utils.UnitText(v);
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
                if (mElm is BaseUI) {
                    var adj = CirSimForm.Sim.FindAdjustable((BaseUI)mElm, i);
                    if (adj != null) {
                        adj.Value = ei.Value;
                    }
                }
            }
            CirSimForm.Sim.NeedAnalyze();
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
                    CirSimForm.Sim.NeedAnalyze();
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
                Visible = false;
                buildDialog();
                Visible = true;
                ResumeLayout(false);
            }
        }

        void clear() {
            while (0 < mPnlCustomCtrl.Controls.Count && mPnlCustomCtrl.Controls[0] != mPnlCommonButtons) {
                mPnlCustomCtrl.Controls.RemoveAt(0);
            }
        }

        void buildDialog() {
            int infoIdx = -1;
            int iRow = 0;
            int iCol = 0;

            mOfsX = 0;
            mOfsY = 0;
            mMaxX = 0;
            mPnlCustomCtrl.Controls.Clear();
            mPnlCustomCtrl.Width = 0;
            mPnlCustomCtrl.Height = 0;

            for (; ; iRow++) {
                var ei = mElm.GetElementInfo(iRow, iCol);
                if (ei == null) {
                    if (0 == iRow) {
                        break;
                    }
                    mOfsX = mMaxX + 4;
                    mOfsY = 0;
                    mMaxX = 0;
                    iRow = -1;
                    iCol++;
                    continue;
                }
                if (ei.Choice != null) {
                    ei.Choice.AutoSize = true;
                    ei.Choice.SelectedValueChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlCustomCtrl, ei.Name, ei.Choice);
                } else if (ei.CheckBox != null) {
                    ei.CheckBox.CheckedChanged += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlCustomCtrl, ei.Name, ei.CheckBox);
                } else if (ei.Button != null) {
                    ei.Button.Click += new EventHandler((s, e) => {
                        itemStateChanged(s);
                    });
                    insertCtrl(mPnlCustomCtrl, ei.Name, ei.Button);
                } else if (ei.TextArea != null) {
                    insertCtrl(mPnlCustomCtrl, ei.Name, ei.TextArea);
                    mCloseOnEnter = false;
                } else if (ei.Textf != null) {
                    insertCtrl(mPnlCustomCtrl, ei.Name, ei.Textf);
                    if (ei.Text == null) {
                        ei.Textf.Text = unitString(ei);
                    }
                } else {
                    mOfsY += 36;
                    continue;
                }
                mEInfos[++infoIdx] = ei;
                mEInfoCount = infoIdx + 1;
            }

            mPnlCommonButtons.Left = 4;
            mPnlCommonButtons.Top = mPnlCustomCtrl.Bottom + 4;
            Width = mPnlCustomCtrl.Width + 21;
            Height = mPnlCommonButtons.Bottom + 42;
        }

        void insertCtrl(Control parent, string name, Control ctrl) {
            if (ctrl is CheckBox) {
                ctrl.Top = mOfsY + 9;
            } else {
                var lbl = new Label() {
                    Left = mOfsX + 4,
                    Top = mOfsY,
                    AutoSize = true,
                    Text = name,
                    TextAlign = ContentAlignment.BottomLeft
                };
                parent.Controls.Add(lbl);
                mMaxX = Math.Max(mMaxX, lbl.Right);
                ctrl.Top = mOfsY + 12;
            }
            ctrl.Left = mOfsX + 4;
            parent.Controls.Add(ctrl);

            mOfsY += ctrl.Height + 16;
            mMaxX = Math.Max(mMaxX, ctrl.Right);

            if (parent.Width < mMaxX) {
                parent.Width = mMaxX;
            }
            if (parent.Height < mOfsY) {
                parent.Height = mOfsY;
            }
        }

        double diffFromInteger(double x) {
            return Math.Abs(x - Math.Round(x));
        }

        string unitString(ElementInfo ei) {
            /* for voltage elements, express values in rms if that would be shorter */
            if (mElm != null && (mElm is VoltageUI)
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
