namespace Circuit.Forms {
	public class Slider {
		public TrackBar Trackbar;
		public Label Label;
		public BaseSymbol Symbol;
		public double MinValue;
		public double MaxValue;
		public string SliderText;

		public int EditItemR { get; private set; }
		public int EditItemC { get; private set; }
		public double Value {
			get { return MinValue + (MaxValue - MinValue) * Trackbar.Value / 100; }
			set {
				int intValue = (int)((value - MinValue) * 100 / (MaxValue - MinValue));
				Trackbar.Value = (intValue < Trackbar.Minimum) ? Trackbar.Minimum :
					(Trackbar.Maximum < intValue) ? Trackbar.Maximum : intValue;
			}
		}

		public Slider(BaseSymbol ce, int itemR) {
			MinValue = 1;
			MaxValue = 1000;
			Symbol = ce;
			EditItemR = itemR;
			EditItemC = 0;
		}

		public Slider(StringTokenizer st) {
			var e = st.nextTokenInt();
			if (e == -1) {
				return;
			}
			Symbol = MainForm.MainForm.SymbolList[e];
			EditItemR = st.nextTokenInt();
			EditItemC = 0;
			MinValue = st.nextTokenDouble();
			MaxValue = st.nextTokenDouble();
			st.nextToken(out SliderText);
			SliderText = TextUtils.UnEscape(SliderText);
		}

		public void CreateSlider() {
			var ei = Symbol.GetElementInfo(EditItemR, EditItemC);
			CreateSlider(ei);
		}

		public void CreateSlider(ElementInfo ei) {
			if (null == ei) {
				return;
			}
			int intValue = (int)((ei.Value - MinValue) * 100 / (MaxValue - MinValue));
			ControlPanel.AddSlider(Label = new Label()
			{
				Text = SliderText
			});
			ControlPanel.AddSlider(Trackbar = new TrackBar()
			{
				SmallChange = 1,
				LargeChange = 10,
				TickFrequency = 10,
				TickStyle = TickStyle.TopLeft,
				Minimum = 0,
				Maximum = 100,
				Value = (intValue < 0) ? 0 : (100 < intValue) ? 100 : intValue,
				Width = 175,
				Height = 23
			});
			Trackbar.ValueChanged += Symbol.CreateSlider(ei, this);
		}

		public void DeleteSlider() {
			ControlPanel.RemoveSlider(Label);
			ControlPanel.RemoveSlider(Trackbar);
		}

		public string Dump() {
			return string.Join(" ",
				'&',
				MainForm.MainForm.SymbolList.IndexOf(Symbol),
				EditItemR,
				MinValue,
				MaxValue,
				TextUtils.Escape(SliderText)
			);
		}
	}
}
