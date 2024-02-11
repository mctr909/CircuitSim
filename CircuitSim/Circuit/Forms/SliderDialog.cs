using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Circuit.Forms {
	public class SliderDialog : Form {
		BaseSymbol mElm;
		ElementInfo[] mEInfos;
		int mEInfoCount;
		Panel mPnlValues;
		Panel mPnlButtons;

		public SliderDialog(BaseSymbol ce) : base() {
			Text = "Add Sliders";
			mElm = ce;

			SuspendLayout();
			Visible = false;

			mPnlValues = new Panel();
			mEInfos = new ElementInfo[10];
			mPnlButtons = new Panel();
			{
				mPnlButtons.AutoSize = true;
				/* 反映 */
				var okButton = new Button()
				{
					Left = 0,
					Width = 50,
					Text = "反映"
				};
				okButton.Click += new EventHandler((sender, e) => {
					apply();
				});
				mPnlButtons.Controls.Add(okButton);
				/* 閉じる */
				var cancelButton = new Button()
				{
					Left = okButton.Right + 4,
					Width = 50,
					Text = "閉じる"
				};
				cancelButton.Click += new EventHandler((sender, e) => {
					closeDialog();
				});
				mPnlButtons.Controls.Add(cancelButton);

				mPnlValues.Controls.Add(mPnlButtons);
			}

			/* */
			mPnlValues.Left = 4;
			mPnlValues.Top = 4;
			Controls.Add(mPnlValues);

			/* */
			buildDialog();
			Width = mPnlValues.Right + 2;
			Height = mPnlValues.Bottom + 2;

			ControlBox = false;
			ResumeLayout(false);
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
			mPnlValues.SuspendLayout();
			for (i = 0; ; i++) {
				var ei = mElm.GetElementInfo(i, 0);
				if (ei == null) {
					break;
				}
				mEInfos[i] = ei;

				if (!ei.CanCreateAdjustable()) {
					continue;
				}
				var adj = findAdjustable(i);
				string name = ei.Name;
				idx = mPnlValues.Controls.IndexOf(mPnlButtons);

				var pnlProperty = new Panel();
				pnlProperty.BorderStyle = BorderStyle.FixedSingle;

				ei.CheckBox = new CheckBox()
				{
					Top = 0,
					Left = 2,
					Height = 19,
					Text = name,
					Checked = adj != null
				};
				ei.CheckBox.CheckedChanged += new EventHandler((sender, e) => {
					itemStateChanged(sender);
				});
				pnlProperty.Controls.Add(ei.CheckBox);

				if (adj == null) {
					pnlProperty.Width = ei.CheckBox.Right + 4;
					pnlProperty.Height = ei.CheckBox.Bottom + 4;
				} else {
					/* スライダー名称 */
					var lblTitle = new Label()
					{
						Top = ei.CheckBox.Bottom,
						Left = ei.CheckBox.Left,
						TextAlign = ContentAlignment.BottomLeft,
						Text = "スライダー名称",
						AutoSize = true
					};
					pnlProperty.Controls.Add(lblTitle);
					ei.LabelBox = new TextBox()
					{
						Top = lblTitle.Bottom,
						Left = lblTitle.Left,
						Text = adj.SliderText,
						Width = 180
					};
					pnlProperty.Controls.Add(ei.LabelBox);
					/* 最小値 */
					var lblMin = new Label()
					{
						Top = ei.LabelBox.Bottom + 4,
						Left = ei.LabelBox.Left,
						TextAlign = ContentAlignment.BottomLeft,
						Text = "最小値",
						AutoSize = true
					};
					pnlProperty.Controls.Add(lblMin);
					ei.MinBox = new TextBox()
					{
						Top = lblMin.Bottom,
						Left = lblMin.Left,
						Text = ElementInfoDialog.UnitString(ei, adj.MinValue),
						Width = 40
					};
					pnlProperty.Controls.Add(ei.MinBox);
					/* 最大値 */
					var lblMax = new Label()
					{
						Top = lblMin.Top,
						Left = ei.MinBox.Right + 4,
						TextAlign = ContentAlignment.BottomLeft,
						Text = "最大値",
						AutoSize = true
					};
					pnlProperty.Controls.Add(lblMax);
					ei.MaxBox = new TextBox()
					{
						Top = lblMax.Bottom,
						Left = lblMax.Left,
						Text = ElementInfoDialog.UnitString(ei, adj.MaxValue),
						Width = 40
					};
					pnlProperty.Controls.Add(ei.MaxBox);
					pnlProperty.Width = ei.LabelBox.Right + 4;
					pnlProperty.Height = ei.MaxBox.Bottom + 4;
				}
				ctrlInsert(mPnlValues, pnlProperty, idx++);
			}
			mPnlValues.ResumeLayout(false);
			mEInfoCount = i;
		}

		Adjustable findAdjustable(int item) {
			return CirSimForm.FindAdjustable(mElm, item);
		}

		void apply() {
			int i;
			for (i = 0; i != mEInfoCount; i++) {
				var adj = findAdjustable(i);
				if (adj == null) {
					continue;
				}
				var ei = mEInfos[i];
				if (ei.LabelBox == null) {  // haven't created UI yet?
					continue;
				}
				{
					adj.SliderText = ei.LabelBox.Text;
					adj.Label.Text = adj.SliderText;
					Utils.ParseUnits(ei.MinBox.Text, out adj.MinValue);
					Utils.ParseUnits(ei.MaxBox.Text, out adj.MaxValue);
					adj.Value = ei.Value;
				}
			}
		}

		public void itemStateChanged(object sender) {
			int i;
			bool changed = false;
			for (i = 0; i != mEInfoCount; i++) {
				var ei = mEInfos[i];
				if (ei.CheckBox == sender) {
					apply();
					if (ei.CheckBox.Checked) {
						var adj = new Adjustable(mElm, i);
						var rg = new Regex(" \\(.*\\)$");
						adj.SliderText = rg.Replace(ei.Name, "");
						adj.CreateSlider(ei);
						CirSimForm.Adjustables.Add(adj);
					} else {
						var adj = findAdjustable(i);
						adj.DeleteSlider();
						CirSimForm.Adjustables.Remove(adj);
					}
					changed = true;
				}
			}
			if (changed) {
				/* apply changes before we reset everything */
				apply();
				Visible = false;
				clearDialog();
				buildDialog();
				Width = mPnlValues.Right + 2;
				Height = mPnlValues.Bottom + 2;
				Visible = true;
			}
		}

		public void clearDialog() {
			while (mPnlValues.Controls[0] != mPnlButtons) {
				mPnlValues.Controls.RemoveAt(0);
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
			var ofsY = 0;
			var width = 0;
			for (int i = 0; i < tmp.Count; i++) {
				tmp[i].Left = 0;
				tmp[i].Top = ofsY;
				p.Controls.Add(tmp[i]);
				ofsY += tmp[i].Height + 8;
				if (width < tmp[i].Width) {
					width = tmp[i].Width;
				}
			}
			p.Width = width + 4;
			p.Height = ofsY;
			tmp.Clear();
		}
	}
}
