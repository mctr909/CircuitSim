using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class VoltageAC : Voltage {
		public VoltageAC(Point pos) : base(pos, ElmVoltage.WAVEFORM.SIN) { }

		public VoltageAC(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

		public override DUMP_ID DumpId { get { return DUMP_ID.AC; } }

		public override void GetInfo(string[] arr) {
			arr[0] = "交流電源";
			arr[1] = "電流：" + Utils.CurrentText(mElm.Current);
			arr[2] = "振幅：" + Utils.VoltageText(mElm.MaxVoltage);
			arr[3] = "周波数：" + Utils.FrequencyText(mElm.Frequency);
			var phase = mElm.Phase + mElm.PhaseOffset;
			phase %= 2 * Math.PI;
			arr[4] = "位相：" + Utils.UnitText3digit(phase * 180 / Math.PI, "deg");
			if (mElm.Bias != 0) {
				arr[5] = "バイアス：" + Utils.VoltageText(mElm.Bias);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c == 0) {
				if (r == 0) {
					return new ElementInfo(VALUE_NAME_AMP, mElm.MaxVoltage);
				}
				if (r == 1) {
					return new ElementInfo(VALUE_NAME_BIAS, mElm.Bias);
				}
				if (r == 2) {
					return new ElementInfo(VALUE_NAME_HZ, mElm.Frequency);
				}
				if (r == 3) {
					return new ElementInfo(VALUE_NAME_PHASE, double.Parse((mElm.Phase * 180 / Math.PI).ToString("0.00")));
				}
				if (r == 4) {
					return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((mElm.PhaseOffset * 180 / Math.PI).ToString("0.00")));
				}
			}
			if (c == 1) {
				if (r == 0) {
					return new ElementInfo("連動グループ", Link.Voltage);
				}
				if (r == 1) {
					return new ElementInfo("連動グループ", Link.Bias);
				}
				if (r == 2) {
					return new ElementInfo("連動グループ", Link.Frequency);
				}
				if (r == 4) {
					return new ElementInfo("連動グループ", Link.PhaseOffset);
				}
				if (r < 4) {
					return new ElementInfo();
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
				if (r == 2) {
					mElm.Frequency = ei.Value;
					var maxfreq = 1 / (8 * ControlPanel.TimeStep);
					if (maxfreq < mElm.Frequency) {
						mElm.Frequency = maxfreq;
					}
				}
				if (r == 3) {
					mElm.Phase = ei.Value * Math.PI / 180;
				}
				if (r == 4) {
					mElm.PhaseOffset = ei.Value * Math.PI / 180;
				}
			}
			if (c == 1) {
				if (r == 0) {
					Link.Voltage = (int)ei.Value;
				}
				if (r == 1) {
					Link.Bias = (int)ei.Value;
				}
				if (r == 2) {
					Link.Frequency = (int)ei.Value;
				}
				if (r == 4) {
					Link.PhaseOffset = (int)ei.Value;
				}
			}
			SetTextPos();
		}

		public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
			var trb = adj.Slider;
			switch (ei.Name) {
			case VALUE_NAME_AMP:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				break;
			case VALUE_NAME_BIAS:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				break;
			case VALUE_NAME_HZ:
				adj.MinValue = 0;
				adj.MaxValue = 1000;
				break;
			case VALUE_NAME_PHASE:
				adj.MinValue = -180;
				adj.MaxValue = 180;
				trb.Maximum = 360;
				trb.TickFrequency = 30;
				break;
			case VALUE_NAME_PHASE_OFS:
				adj.MinValue = -180;
				adj.MaxValue = 180;
				trb.Maximum = 360;
				trb.TickFrequency = 30;
				break;
			}
			return new EventHandler((s, e) => {
				var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				switch (ei.Name) {
				case VALUE_NAME_AMP:
					SetLinkedValues<Voltage>(VoltageLink.VOLTAGE, val);
					break;
				case VALUE_NAME_BIAS:
					SetLinkedValues<Voltage>(VoltageLink.BIAS, val);
					break;
				case VALUE_NAME_HZ:
					SetLinkedValues<Voltage>(VoltageLink.FREQUENCY, val);
					break;
				case VALUE_NAME_PHASE:
					mElm.Phase = val * Math.PI / 180;
					break;
				case VALUE_NAME_PHASE_OFS:
					SetLinkedValues<Voltage>(VoltageLink.PHASE_OFFSET, val);
					break;
				}
			});
		}
	}
}
