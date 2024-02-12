using System;
using System.Windows.Forms;

namespace Circuit {
	static class ControlPanel {
		public static Panel VerticalPanel { get; private set; }
		public static Button BtnReset { get; private set; }
		public static Button BtnRunStop { get; private set; }
		public static Button BtnReload { get; private set; }
		public static TrackBar TrbSpeed { get; private set; }
		public static TrackBar TrbCurrent { get; private set; }
		public static Label LblSelectInfo { get; private set; }
		public static CheckBox ChkContinuousArrangement { get; private set; }
		public static CheckBox ChkShowCurrent { get; private set; }
		public static CheckBox ChkShowValues { get; private set; }
		public static CheckBox ChkShowName { get; private set; }
		public static CheckBox ChkUseAnsiSymbols { get; private set; }
		public static CheckBox ChkPrintable { get; private set; }
		public static CheckBox ChkCrossHair { get; private set; }

		public static double StepRate {
			get {
				if (TrbSpeed.Value == 0) {
					return 0;
				}
				return 1.0 * TrbSpeed.Value / TrbSpeed.Maximum;
			}
		}

		public static double TimeStep {
			get { return Circuit.TimeStep; }
			set {
				Circuit.TimeStep = value;
				if (null != mTxtTimeStep) {
					mTxtTimeStep.Text = Utils.UnitText(Circuit.TimeStep, "");
				}
			}
		}

		static TextBox mTxtTimeStep;
		static Panel mSliderPanel;

		public static void Init() {
			int ofsY = 0;
			VerticalPanel = new Panel();

			/* 実行 */
			BtnRunStop = new Button() { AutoSize = true, Text = "実行" };
			BtnRunStop.Click += new EventHandler((s, e) => { CirSimForm.SetSimRunning(!CirSimForm.IsRunning); });
			BtnRunStop.Width = 50;
			BtnRunStop.Left = 4;
			BtnRunStop.Top = ofsY;
			VerticalPanel.Controls.Add(BtnRunStop);

			/* リセット */
			BtnReset = new Button() { AutoSize = true, Text = "リセット" };
			BtnReset.Click += new EventHandler((s, e) => { CirSimForm.ResetButton_onClick(); });
			BtnReset.Width = 50;
			BtnReset.Left = BtnRunStop.Right + 4;
			BtnReset.Top = ofsY;
			VerticalPanel.Controls.Add(BtnReset);

			/* 再読み込み */
			BtnReload = new Button() { AutoSize = true, Text = "再読み込み" };
			BtnReload.Click += new EventHandler((s, e) => { CirSimForm.Instance.Reload(); });
			BtnReload.Left = BtnReset.Right + 4;
			BtnReload.Top = ofsY;
			VerticalPanel.Controls.Add(BtnReload);
			ofsY += BtnReload.Height + 4;

			/* 単位時間 */
			ofsY += 8;
			var lblTimeStep = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "単位時間(sec)" };
			VerticalPanel.Controls.Add(lblTimeStep);
			ofsY += lblTimeStep.Height + 4;
			mTxtTimeStep = new TextBox()
			{
				Left = 4,
				Top = ofsY,
				Width = 50,
				TextAlign = HorizontalAlignment.Right,
				Font = new System.Drawing.Font("MS Gothic", 11)
			};
			mTxtTimeStep.TextChanged += new EventHandler((s, e) => {
				var tmp = 0.0;
				if (Utils.ParseUnits(mTxtTimeStep.Text, out tmp)) {
					Circuit.TimeStep = tmp;
				}
				mTxtTimeStep.Text = Utils.UnitText(Circuit.TimeStep, "");
			});
			VerticalPanel.Controls.Add(mTxtTimeStep);
			ofsY += mTxtTimeStep.Height + 4;

			/* 実行速度 */
			var lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "実行速度" };
			VerticalPanel.Controls.Add(lbl);
			ofsY += lbl.Height;
			TrbSpeed = new TrackBar()
			{
				Left = 4,
				Top = ofsY,
				Minimum = 0,
				Maximum = 1000,
				SmallChange = 1,
				LargeChange = 10,
				TickFrequency = 100,
				TickStyle = TickStyle.TopLeft,
				Value = 10,
				Width = 200
			};
			VerticalPanel.Controls.Add(TrbSpeed);
			ofsY += TrbSpeed.Height;

			/* 電流表示速度 */
			lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "電流表示速度" };
			VerticalPanel.Controls.Add(lbl);
			ofsY += lbl.Height;
			lbl = new Label() { Left = 9, Top = ofsY, AutoSize = true, Text = "10" };
			VerticalPanel.Controls.Add(lbl);
			lbl = new Label() { Left = 37, Top = ofsY, AutoSize = true, Text = "1" };
			VerticalPanel.Controls.Add(lbl);

			lbl = new Label() { Left = 50, Top = ofsY, AutoSize = true, Text = "100m" };
			VerticalPanel.Controls.Add(lbl);
			lbl = new Label() { Left = 80, Top = ofsY, AutoSize = true, Text = "10m" };
			VerticalPanel.Controls.Add(lbl);
			lbl = new Label() { Left = 106, Top = ofsY, AutoSize = true, Text = "1m" };
			VerticalPanel.Controls.Add(lbl);

			lbl = new Label() { Left = 128, Top = ofsY, AutoSize = true, Text = "100u" };
			VerticalPanel.Controls.Add(lbl);
			lbl = new Label() { Left = 155, Top = ofsY, AutoSize = true, Text = "10u" };
			VerticalPanel.Controls.Add(lbl);
			lbl = new Label() { Left = 181, Top = ofsY, AutoSize = true, Text = "1u" };
			VerticalPanel.Controls.Add(lbl);

			ofsY += lbl.Height;
			TrbCurrent = new TrackBar()
			{
				Left = 4,
				Top = ofsY,
				Minimum = -2,
				Maximum = 12,
				SmallChange = 1,
				LargeChange = 1,
				TickFrequency = 2,
				TickStyle = TickStyle.TopLeft,
				Value = 6,
				Width = 200
			};
			VerticalPanel.Controls.Add(TrbCurrent);
			ofsY += TrbCurrent.Height;

			/* 連続で配置 */
			ChkContinuousArrangement = new CheckBox()
			{
				Left = 4,
				Top = ofsY,
				Checked = true,
				AutoSize = true,
				Text = "連続で配置"
			};
			VerticalPanel.Controls.Add(ChkContinuousArrangement);
			ofsY += ChkContinuousArrangement.Height + 4;

			/* 電流を表示 */
			ChkShowCurrent = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "電流を表示" };
			VerticalPanel.Controls.Add(ChkShowCurrent);
			ofsY += ChkShowCurrent.Height + 4;

			/* 値を表示 */
			ChkShowValues = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "値を表示" };
			ChkShowValues.CheckedChanged += new EventHandler((s, e) => {
				for (int i = 0; i != Circuit.SymbolCount; i++) {
					var ce = Circuit.SymbolList[i];
					ce.SetPoints();
				}
			});
			VerticalPanel.Controls.Add(ChkShowValues);
			ofsY += ChkShowValues.Height + 4;

			/* 名前を表示 */
			ChkShowName = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "名前を表示" };
			ChkShowName.CheckedChanged += new EventHandler((s, e) => {
				for (int i = 0; i != Circuit.SymbolCount; i++) {
					var ce = Circuit.SymbolList[i];
					ce.SetPoints();
				}
			});
			VerticalPanel.Controls.Add(ChkShowName);
			ofsY += ChkShowName.Height + 4;

			/* ANSI */
			ChkUseAnsiSymbols = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ANSI" };
			VerticalPanel.Controls.Add(ChkUseAnsiSymbols);
			ofsY += ChkUseAnsiSymbols.Height + 4;

			/* 白黒表示 */
			ChkPrintable = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "白黒表示" };
			ChkPrintable.CheckedChanged += new EventHandler((s, e) => {
				CustomGraphics.SetColor(ChkPrintable.Checked);
			});
			VerticalPanel.Controls.Add(ChkPrintable);
			ofsY += ChkPrintable.Height + 4;

			/* ポインターを表示 */
			ChkCrossHair = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ポインターを表示" };
			VerticalPanel.Controls.Add(ChkCrossHair);
			ofsY += ChkCrossHair.Height + 4;

			/* 選択情報 */
			lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "選択情報" };
			VerticalPanel.Controls.Add(lbl);
			ofsY += lbl.Height;
			LblSelectInfo = new Label()
			{
				Left = 4,
				Top = ofsY,
				Width = 200,
				Height = 100,
				BorderStyle = BorderStyle.FixedSingle
			};
			VerticalPanel.Controls.Add(LblSelectInfo);
			ofsY += LblSelectInfo.Height + 4;

			/* SliderPanel */
			mSliderPanel = new Panel()
			{
				Left = 4,
				Top = ofsY,
				BorderStyle = BorderStyle.FixedSingle,
				AutoScroll = true
			};
			VerticalPanel.Controls.Add(mSliderPanel);

			/* */
			VerticalPanel.Width = TrbSpeed.Width + 12;
			VerticalPanel.Height = ofsY;
		}

		public static void Reset() {
			TimeStep = 1e-6;
			ChkShowCurrent.Checked = false;
			ChkShowValues.Checked = true;
			ChkUseAnsiSymbols.Checked = true;
			TrbSpeed.Value = 57;
		}

		public static void SetSliderPanelHeight() {
			if (mSliderPanel == null) {
				return;
			}
			int height = 0;
			for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
				if (height < mSliderPanel.Controls[i].Bottom) {
					height = mSliderPanel.Controls[i].Bottom;
				}
			}
			mSliderPanel.Visible = 0 < mSliderPanel.Controls.Count;
			mSliderPanel.Height = Math.Min(220, height + 4);
			VerticalPanel.Height = mSliderPanel.Bottom + 4;
		}

		public static void AddSlider(Control ctrl) {
			if (mSliderPanel == null) {
				return;
			}
			var ofsY = 4;
			for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
				if (ofsY < mSliderPanel.Controls[i].Bottom) {
					ofsY = mSliderPanel.Controls[i].Bottom;
				}
			}
			ctrl.Top = ofsY;
			mSliderPanel.Controls.Add(ctrl);
			SetSliderPanelHeight();
		}

		public static void RemoveSlider(Control ctrl) {
			if (mSliderPanel == null) {
				return;
			}
			int ofsY = 4;
			mSliderPanel.SuspendLayout();
			mSliderPanel.Controls.Remove(ctrl);
			for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
				mSliderPanel.Controls[i].Top = ofsY;
				ofsY += mSliderPanel.Controls[i].Height + 4;
			}
			mSliderPanel.ResumeLayout(false);
			SetSliderPanelHeight();
		}
	}
}
