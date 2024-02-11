using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Symbol.Input;

namespace Circuit.Forms {
	public class ElementInfoDialog : Form {
		const double ROOT2 = 1.41421356237309504880;

		BaseSymbol mElm;
		Button mBtnApply;
		Button mBtnCancel;
		ElementInfo[,] mEInfos;

		Panel mPnlCustomCtrl;
		Panel mPnlCommonButtons;
		bool mCloseOnEnter = true;

		public ElementInfoDialog(BaseSymbol ce) : base() {
			Text = "Edit Component";
			mElm = ce;

			mEInfos = new ElementInfo[16, 16];

			SuspendLayout();
			Visible = false;

			mPnlCommonButtons = new Panel();
			{
				/* 反映 */
				mPnlCommonButtons.Controls.Add(mBtnApply = new Button()
				{
					AutoSize = true,
					Width = 50,
					Text = "反映"
				});
				mBtnApply.Click += new EventHandler((s, e) => {
					apply();
					Close();
				});
				/* 閉じる */
				mPnlCommonButtons.Controls.Add(mBtnCancel = new Button()
				{
					AutoSize = true,
					Width = 50,
					Left = mBtnApply.Right + 4,
					Text = "閉じる"
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
			ControlBox = false;
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

			if (null == mEInfos[0, 0]) {
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
			return Utils.UnitText(v);
		}

		void apply() {
			for (int c = 0; c < 16; c++) {
				for (int r = 0; r != 16; r++) {
					var ei = mEInfos[r, c];
					if (ei == null) {
						continue;
					}
					if (ei.TextString != null) {
						try {
							ei.Text = ei.TextString.Text;
						} catch (Exception ex) {
							throw ex;
						}
					} else if (ei.TextInt != null) {
						try {
							ei.Value = int.Parse(ei.TextInt.Text);
						} catch (FormatException ex) {
							MessageBox.Show(ex.Message);
						} catch (Exception ex) {
							throw ex;
						}
					} else if (ei.TextDouble != null) {
						try {
							double tmp;
							Utils.ParseUnits(ei.TextDouble.Text, out tmp);
							ei.Value = tmp;
						} catch (FormatException ex) {
							MessageBox.Show(ex.Message);
						} catch (Exception ex) {
							throw ex;
						}
					} else if (ei.Button != null) {
						continue;
					}
					mElm.SetElementValue(r, c, ei);

					/* update slider if any */
					if (mElm is BaseSymbol) {
						var adj = CirSimForm.FindAdjustable(mElm, r);
						if (adj != null) {
							adj.Value = ei.Value;
						}
					}
				}
			}
			CirSimForm.NeedAnalyze();
		}

		void itemStateChanged(object sender) {
			bool changed = false;
			bool applied = false;
			for (int c = 0; c < 16; c++) {
				for (int r = 0; r < 16; r++) {
					var ei = mEInfos[r, c];
					if (ei == null) {
						continue;
					}
					if (ei.Choice == sender || ei.CheckBox == sender || ei.Button == sender) {
						/* if we're pressing a button, make sure to apply changes first */
						if (ei.Button == sender && !ei.NewDialog) {
							apply();
							applied = true;
						}
						mElm.SetElementValue(r, c, ei);
						if (ei.NewDialog) {
							changed = true;
						}
						CirSimForm.NeedAnalyze();
					}
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
			int iRow = 0;
			int iCol = 0;
			int maxX = 0;
			int ofsX = 0;
			int ofsY = 0;
			List<int> listOfsY = new List<int>();

			mPnlCustomCtrl.Controls.Clear();
			mPnlCustomCtrl.Width = 0;
			mPnlCustomCtrl.Height = 0;

			if (string.IsNullOrEmpty(mElm.ReferenceName)) {
				Text = string.Format("{0} 設定", mElm.GetType().Name);
			} else {
				Text = string.Format("{0} [{1}] 設定",
					mElm.GetType().Name,
					mElm.ReferenceName);
			}

			for (; ; iRow++) {
				var ei = mElm.GetElementInfo(iRow, iCol);
				if (ei == null) {
					if (0 == iRow) {
						break;
					}
					ofsX = maxX + 4;
					maxX = 0;
					iRow = -1;
					iCol++;
					continue;
				}
				if (0 == iCol) {
					listOfsY.Add(ofsY);
				} else {
					if (iRow < listOfsY.Count) {
						ofsY = listOfsY[iRow];
					} else {
						continue;
					}
				}
				if (ei.Choice != null) {
					ei.Choice.AutoSize = true;
					ei.Choice.SelectedValueChanged += new EventHandler((s, e) => {
						itemStateChanged(s);
					});
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.Choice, ofsX, ref ofsY, ref maxX);
				} else if (ei.CheckBox != null) {
					ei.CheckBox.CheckedChanged += new EventHandler((s, e) => {
						itemStateChanged(s);
					});
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.CheckBox, ofsX, ref ofsY, ref maxX);
				} else if (ei.Button != null) {
					ei.Button.Click += new EventHandler((s, e) => {
						itemStateChanged(s);
					});
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.Button, ofsX, ref ofsY, ref maxX);
				} else if (ei.TextString != null) {
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.TextString, ofsX, ref ofsY, ref maxX);
					if (null == ei.Text) {
						ei.TextString.Text = "";
					} else {
						ei.TextString.Text = ei.Text;
					}
					mCloseOnEnter = false;
				} else if (ei.TextInt != null) {
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.TextInt, ofsX, ref ofsY, ref maxX);
					ei.TextInt.Text = ((int)ei.Value).ToString();
					mCloseOnEnter = false;
				} else if (ei.TextDouble != null) {
					insertCtrl(mPnlCustomCtrl, ei.Name, ei.TextDouble, ofsX, ref ofsY, ref maxX);
					ei.TextDouble.Text = unitString(ei);
					mCloseOnEnter = false;
				} else {
					continue;
				}
				mEInfos[iRow, iCol] = ei;
			}

			mPnlCommonButtons.Left = 4;
			mPnlCommonButtons.Top = mPnlCustomCtrl.Bottom + 4;
			Width = mPnlCustomCtrl.Width + 21;
			Height = mPnlCommonButtons.Bottom + 42;
		}

		void insertCtrl(Control parent, string name, Control ctrl, int ofsX, ref int ofsY, ref int maxX) {
			if (ctrl is CheckBox) {
				ctrl.Top = ofsY + 9;
			} else {
				var lbl = new Label()
				{
					Left = ofsX + 4,
					Top = ofsY,
					AutoSize = true,
					Text = name,
					TextAlign = ContentAlignment.BottomLeft
				};
				parent.Controls.Add(lbl);
				maxX = Math.Max(maxX, lbl.Right);
				ctrl.Top = ofsY + 12;
			}
			ctrl.Left = ofsX + 4;
			parent.Controls.Add(ctrl);

			ofsY += Math.Max(38, ctrl.Height + 17);
			maxX = Math.Max(maxX, ctrl.Right);

			if (parent.Width < maxX) {
				parent.Width = maxX;
			}
			if (parent.Height < ofsY) {
				parent.Height = ofsY;
			}
		}

		double diffFromInteger(double x) {
			return Math.Abs(x - Math.Round(x));
		}

		string unitString(ElementInfo ei) {
			/* for voltage elements, express values in rms if that would be shorter */
			if (mElm != null && (mElm is Voltage)
				&& Math.Abs(ei.Value) > 1e-4
				&& diffFromInteger(ei.Value * 1e4) > diffFromInteger(ei.Value * 1e4 / ROOT2)) {
				return UnitString(ei, ei.Value / ROOT2) + "rms";
			}
			return UnitString(ei, ei.Value);
		}

		private void InitializeComponent() {
			this.SuspendLayout();
			// 
			// ElementInfoDialog
			// 
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Name = "ElementInfoDialog";
			this.ResumeLayout(false);

		}
	}
}
