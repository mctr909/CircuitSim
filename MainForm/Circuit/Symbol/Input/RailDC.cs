using Circuit.Elements.Input;
using MainForm.Forms;

namespace Circuit.Symbol.Input {
	class RailDC : Rail {
		public RailDC(Point pos) : base(pos, ElmVoltage.WAVEFORM.DC) { }

		public RailDC(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

		public override DUMP_ID DumpId { get { return DUMP_ID.RAIL_DC; } }

		public override void GetInfo(string[] arr) {
			arr[0] = "直流電源";
			arr[1] = "電圧：" + TextUtils.Voltage(mElm.VoltageDiff + mElm.Bias);
			arr[2] = "電流：" + TextUtils.Current(mElm.I[0]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c == 0) {
				if (r == 0) {
					return new ElementInfo(VALUE_NAME_V, mElm.MaxVoltage);
				}
				if (r == 1) {
					return new ElementInfo(VALUE_NAME_BIAS, mElm.Bias);
				}
			}
			return null;
		}

		public override void SetElementValue(int r, int c, ElementInfo ei) {
			if (c == 0) {
				if (r == 0) {
					mElm.MaxVoltage = ei.Value;
				}
				if (r == 1) {
					mElm.Bias = ei.Value;
				}
			}
		}

		public override EventHandler CreateSlider(ElementInfo ei, Slider adj) {
			var trb = adj.Trackbar;
			switch (ei.Name) {
			case VALUE_NAME_V:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				break;
			case VALUE_NAME_BIAS:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				break;
			}
			return new EventHandler((s, e) => {
				var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				switch (ei.Name) {
				case VALUE_NAME_V:
					mElm.MaxVoltage = val;
					break;
				case VALUE_NAME_BIAS:
					mElm.Bias = val;
					break;
				}
			});
		}
	}
}
