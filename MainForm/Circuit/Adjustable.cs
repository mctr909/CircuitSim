namespace Circuit {
	public class Adjustable {
		public TrackBar Slider;
		public Label Label;
		public BaseSymbol UI;
		public double MinValue;
		public double MaxValue;
		public string SliderText;

		public int EditItemR { get; private set; }
		public int EditItemC { get; private set; }
		public double Value {
			get { return MinValue + (MaxValue - MinValue) * Slider.Value / 100; }
			set {
				int intValue = (int)((value - MinValue) * 100 / (MaxValue - MinValue));
				Slider.Value = (intValue < Slider.Minimum) ? Slider.Minimum :
					(Slider.Maximum < intValue) ? Slider.Maximum : intValue;
			}
		}

		public Adjustable(BaseSymbol ce, int itemR) {
			MinValue = 1;
			MaxValue = 1000;
			UI = ce;
			EditItemR = itemR;
			EditItemC = 0;
		}

		public Adjustable(StringTokenizer st) {
			var e = st.nextTokenInt();
			if (e == -1) {
				return;
			}
			UI = CircuitSymbol.List[e];
			EditItemR = st.nextTokenInt();
			EditItemC = 0;
			MinValue = st.nextTokenDouble();
			MaxValue = st.nextTokenDouble();
			st.nextToken(out SliderText);
			SliderText = Utils.UnEscape(SliderText);
		}

		public void CreateSlider() {
			var ei = UI.GetElementInfo(EditItemR, EditItemC);
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
			ControlPanel.AddSlider(Slider = new TrackBar()
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
			Slider.ValueChanged += UI.CreateSlider(ei, this);
		}

		public void DeleteSlider() {
			ControlPanel.RemoveSlider(Label);
			ControlPanel.RemoveSlider(Slider);
		}

		public string Dump() {
			return string.Join(" ",
				'&',
				CircuitSymbol.List.IndexOf(UI),
				EditItemR,
				MinValue,
				MaxValue,
				Utils.Escape(SliderText)
			);
		}
	}
}
