using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class VoltageLink : BaseLink {
		public const int VOLTAGE = 0;
		public const int BIAS = 1;
		public const int FREQUENCY = 2;
		public const int PHASE_OFFSET = 3;
		public int Voltage = 0;
		public int Bias = 0;
		public int Frequency = 0;
		public int PhaseOffset = 0;
		public override int GetGroup(int id) {
			switch (id) {
			case VOLTAGE:
				return Voltage;
			case BIAS:
				return Bias;
			case FREQUENCY:
				return Frequency;
			case PHASE_OFFSET:
				return PhaseOffset;
			default:
				return 0;
			}
		}
		public override void SetValue(BaseElement element, int linkID, double value) {
			var elm = (ElmVoltage)element;
			switch (linkID) {
			case VOLTAGE:
				elm.MaxVoltage = value;
				break;
			case BIAS:
				elm.Bias = value;
				break;
			case FREQUENCY:
				elm.Frequency = value;
				break;
			case PHASE_OFFSET:
				elm.PhaseOffset = value * Math.PI / 180;
				break;
			}
		}
		public override void Load(StringTokenizer st) {
			Voltage = st.nextTokenInt();
			Bias = st.nextTokenInt();
			Frequency = st.nextTokenInt();
			PhaseOffset = st.nextTokenInt();
		}
		public override void Dump(List<object> optionList) {
			optionList.Add(Voltage);
			optionList.Add(Bias);
			optionList.Add(Frequency);
			optionList.Add(PhaseOffset);
		}
	}

	class Voltage : BaseSymbol {
		const int FLAG_PULSE_DUTY = 4;
		const double DEFAULT_PULSE_DUTY = 0.5;

		protected const int BODY_LEN = 28;
		const int BODY_LEN_DC = 6;
		const int WAVE_HEIGHT = 5;
		const int DX = 10;
		const int DX_H = 5;

		public const string VALUE_NAME_V = "電圧";
		public const string VALUE_NAME_AMP = "振幅";
		public const string VALUE_NAME_BIAS = "バイアス電圧";
		public const string VALUE_NAME_HZ = "周波数";
		public const string VALUE_NAME_PHASE = "位相";
		public const string VALUE_NAME_PHASE_OFS = "オフセット位相";
		public const string VALUE_NAME_DUTY = "デューティ比";

		protected override BaseLink mLink { get; set; } = new VoltageLink();
		protected VoltageLink Link { get { return (VoltageLink)mLink; } }

		protected ElmVoltage mElm;

		public override BaseElement Element { get { return mElm; } }

		PointF mPs1;
		PointF mPs2;
		PointF mPs3;
		PointF mPs4;
		PointF[] mWaveFormPos;
		PointF mTextPos;
		double mTextRot;
		PointF mSignPos;

		public Voltage(Point pos, ElmVoltage.WAVEFORM wf) : base(pos) {
			mElm = new ElmVoltage(wf);
			ReferenceName = "";
		}

		public Voltage(Point p1, Point p2, int f) : base(p1, p2, f) { }

		public Voltage(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmVoltage(st);
			Link.Load(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.VOLTAGE; } }

		protected override void dump(List<object> optionList) {
			/* set flag so we know if duty cycle is correct for pulse waveforms */
			if (mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE ||
				mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE) {
				mFlags |= FLAG_PULSE_DUTY;
			} else {
				mFlags &= ~FLAG_PULSE_DUTY;
			}
			optionList.Add(mElm.WaveForm);
			optionList.Add(mElm.Frequency);
			optionList.Add(mElm.MaxVoltage);
			optionList.Add(mElm.Bias);
			optionList.Add((mElm.Phase * 180 / Math.PI).ToString("0"));
			optionList.Add((mElm.PhaseOffset * 180 / Math.PI).ToString("0"));
			optionList.Add(mElm.DutyCycle.ToString("0.00"));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads((mElm.WaveForm == ElmVoltage.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);
			SetTextPos();
			SetWaveform();
		}

		protected void SetTextPos() {
			int sign;
			if (Post.Horizontal) {
				sign = -Post.Dsign;
			} else {
				sign = Post.Dsign;
			}
			InterpolationPost(ref mSignPos, (Post.Len / 2 + 0.6 * BODY_LEN) / Post.Len, 7 * sign);
			if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC) {
				int hs = 10;
				InterpolationLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
				InterpolationLeadAB(ref mPs3, ref mPs4, 1, hs);
				var s = Utils.UnitText(mElm.MaxVoltage, "V");
				var w = CustomGraphics.Instance.GetTextSize(s).Width;
				InterpolationPost(ref mTextPos, 0.5, w * 0.5 + 10);
				mTextRot = Angle(Post.A, Post.B) + Math.PI / 2;
			} else {
				InterpolationLead(ref mPs1, 0.5);
			}
		}

		void SetWaveform() {
			float x, y;
			if (this is Rail) {
				x = Post.B.X;
				y = Post.B.Y;
			} else {
				x = mPs1.X;
				y = mPs1.Y;
			}

			var duty = Math.Min(1, Math.Max(0, mElm.DutyCycle));
			var w = (float)(2 * DX * duty);
			var w2 = (float)(DX * duty);
			var w3 = (float)(DX * duty / 3.0);
			var wh = 0 < duty ? WAVE_HEIGHT : 0;
			var pp = 0 == duty ? 0 : 1;
			var pm = 1 == duty ? 0 : 1;

			switch (mElm.WaveForm) {
			case ElmVoltage.WAVEFORM.SIN: {
				mWaveFormPos = new PointF[DX * 2 + 1];
				for (int t = -DX, c = 0; t <= DX; t++, c++) {
					var yy = y + (int)(.95 * Math.Sin(t * Math.PI / DX) * WAVE_HEIGHT);
					mWaveFormPos[c].X = x + t;
					mWaveFormPos[c].Y = yy;
				}
				break;
			}
			case ElmVoltage.WAVEFORM.SQUARE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX, y - WAVE_HEIGHT * pp),
					new PointF(x - DX + w, y - WAVE_HEIGHT * pp),
					new PointF(x - DX + w, y + WAVE_HEIGHT * pm),
					new PointF(x + DX, y + WAVE_HEIGHT * pm),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.TRIANGLE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX_H, y - WAVE_HEIGHT),
					new PointF(x, y),
					new PointF(x + DX_H, y + WAVE_HEIGHT),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.SAWTOOTH:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x, y - WAVE_HEIGHT),
					new PointF(x, y + WAVE_HEIGHT),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.PULSE_MONOPOLE:
				if (mElm.MaxVoltage < 0) {
					mWaveFormPos = [
						new PointF(x - DX, y),
						new PointF(x - DX, y + wh),
						new PointF(x - DX + w, y + wh),
						new PointF(x - DX + w, y),
						new PointF(x + DX, y)
					];
				} else {
					mWaveFormPos = [
						new PointF(x - DX, y),
						new PointF(x - DX, y - wh),
						new PointF(x - DX + w, y - wh),
						new PointF(x - DX + w, y),
						new PointF(x + DX, y)
					];
				}
				break;
			case ElmVoltage.WAVEFORM.PULSE_DIPOLE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX, y - wh),
					new PointF(x - DX + w2, y - wh),
					new PointF(x - DX + w2, y),
					new PointF(x, y),
					new PointF(x, y + wh),
					new PointF(x + w2, y + wh),
					new PointF(x + w2, y),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.PWM_MONOPOLE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX, y - WAVE_HEIGHT),
					new PointF(x - DX, y),
					new PointF(x - DX_H - w3, y),
					new PointF(x - DX_H - w3, y - WAVE_HEIGHT),
					new PointF(x - DX_H + w3, y - WAVE_HEIGHT),
					new PointF(x - DX_H + w3, y),
					new PointF(x, y),
					new PointF(x, y - WAVE_HEIGHT),
					new PointF(x, y),
					new PointF(x + DX_H - w3, y),
					new PointF(x + DX_H - w3, y - WAVE_HEIGHT),
					new PointF(x + DX_H + w3, y - WAVE_HEIGHT),
					new PointF(x + DX_H + w3, y),
					new PointF(x + DX, y),
					new PointF(x + DX, y - WAVE_HEIGHT),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.PWM_DIPOLE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX + 1, y),
					new PointF(x - DX + 1, y - wh),
					new PointF(x - DX + 1, y),
					new PointF(x - DX_H - w3, y),
					new PointF(x - DX_H - w3, y - wh),
					new PointF(x - DX_H + w3, y - wh),
					new PointF(x - DX_H + w3, y),
					new PointF(x - 1, y),
					new PointF(x - 1, y - wh),
					new PointF(x - 1, y),
					new PointF(x + 1, y),
					new PointF(x + 1, y + wh),
					new PointF(x + 1, y),
					new PointF(x + DX_H - w3, y),
					new PointF(x + DX_H - w3, y + wh),
					new PointF(x + DX_H + w3, y + wh),
					new PointF(x + DX_H + w3, y),
					new PointF(x + DX - 1, y),
					new PointF(x + DX - 1, y + wh),
					new PointF(x + DX - 1, y),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.PWM_POSITIVE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x - DX + 1, y),
					new PointF(x - DX + 1, y - wh),
					new PointF(x - DX + 1, y),
					new PointF(x - DX_H - w3, y),
					new PointF(x - DX_H - w3, y - wh),
					new PointF(x - DX_H + w3, y - wh),
					new PointF(x - DX_H + w3, y),
					new PointF(x - 1, y),
					new PointF(x - 1, y - wh),
					new PointF(x - 1, y),
					new PointF(x + DX, y)
				];
				break;
			case ElmVoltage.WAVEFORM.PWM_NEGATIVE:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x + 1, y),
					new PointF(x + 1, y - wh),
					new PointF(x + 1, y),
					new PointF(x + DX_H - w3, y),
					new PointF(x + DX_H - w3, y - wh),
					new PointF(x + DX_H + w3, y - wh),
					new PointF(x + DX_H + w3, y),
					new PointF(x + DX - 1, y),
					new PointF(x + DX - 1, y - wh),
					new PointF(x + DX - 1, y),
					new PointF(x + DX, y)
				];
				break;
			default:
				mWaveFormPos = [
					new PointF(x - DX, y),
					new PointF(x + DX, y)
				];
				break;
			}
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC) {
				DrawLine(mPs1, mPs2);
				DrawLine(mPs3, mPs4);
				var s = Utils.UnitText(mElm.MaxVoltage, "V");
				DrawCenteredText(s, mTextPos, mTextRot);
			} else {
				DrawWaveform(mPs1);
				if (ControlPanel.ChkShowValues.Checked) {
					var s = Utils.UnitText(mElm.MaxVoltage, "V\r\n");
					s += Utils.FrequencyText(mElm.Frequency, true) + "\r\n";
					s += Utils.PhaseText(mElm.Phase + mElm.PhaseOffset);
					var w = g.GetTextSize(s).Width;
					InterpolationPost(ref mTextPos, 0.5, w - 4);
					DrawCenteredText(s, mTextPos);
				}
				if (0 < mElm.Bias || (0 == mElm.Bias &&
					(ElmVoltage.WAVEFORM.PULSE_MONOPOLE == mElm.WaveForm || ElmVoltage.WAVEFORM.PULSE_DIPOLE == mElm.WaveForm))) {
					DrawCenteredLText("+", mSignPos);
				} else {
					DrawCenteredLText("*", mSignPos);
				}
			}

			UpdateDotCount();

			if (ConstructItem != this) {
				if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC) {
					DrawCurrent(Post.A, Post.B, mCurCount);
				} else {
					DrawCurrentA(mCurCount);
					DrawCurrentB(mCurCount);
				}
			}
		}

		protected void DrawWaveform(PointF p) {
			DrawCircle(p, BODY_LEN / 2);
			DrawPolyline(mWaveFormPos);
		}

		public override void GetInfo(string[] arr) {
			switch (mElm.WaveForm) {
			case ElmVoltage.WAVEFORM.DC:
			case ElmVoltage.WAVEFORM.SIN:
			case ElmVoltage.WAVEFORM.SQUARE:
			case ElmVoltage.WAVEFORM.PULSE_MONOPOLE:
			case ElmVoltage.WAVEFORM.PULSE_DIPOLE:
			case ElmVoltage.WAVEFORM.SAWTOOTH:
			case ElmVoltage.WAVEFORM.TRIANGLE:
			case ElmVoltage.WAVEFORM.NOISE:
			case ElmVoltage.WAVEFORM.PWM_DIPOLE:
			case ElmVoltage.WAVEFORM.PWM_POSITIVE:
			case ElmVoltage.WAVEFORM.PWM_NEGATIVE:
				arr[0] = mElm.WaveForm.ToString();
				break;
			}
			arr[1] = "電圧：" + Utils.VoltageText(mElm.VoltageDiff);
			int i = 2;
			if (mElm.WaveForm != ElmVoltage.WAVEFORM.DC && mElm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
				arr[i++] = "振幅：" + Utils.VoltageText(mElm.MaxVoltage);
				arr[i++] = "周波数：" + Utils.FrequencyText(mElm.Frequency);
				var phase = mElm.Phase + mElm.PhaseOffset;
				phase %= 2 * Math.PI;
				arr[i++] = "位相：" + Utils.UnitText3digit(phase * 180 / Math.PI, "deg");
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c == 0) {
				if (r == 0) {
					var ei = new ElementInfo("波形") {
						Choice = new ComboBox()
					};
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.DC);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SIN);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SQUARE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.TRIANGLE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SAWTOOTH);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE_MONOPOLE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE_DIPOLE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_MONOPOLE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_DIPOLE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_POSITIVE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_NEGATIVE);
					ei.Choice.Items.Add(ElmVoltage.WAVEFORM.NOISE);
					ei.Choice.SelectedIndex = (int)mElm.WaveForm;
					return ei;
				}
				if (r == 1) {
					return new ElementInfo(mElm.WaveForm == ElmVoltage.WAVEFORM.DC ? VALUE_NAME_V : VALUE_NAME_AMP, mElm.MaxVoltage);
				}
				if (r == 2) {
					return new ElementInfo(VALUE_NAME_BIAS, mElm.Bias);
				}
				if (r == 3) {
					if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC || mElm.WaveForm == ElmVoltage.WAVEFORM.NOISE) {
						return null;
					} else {
						return new ElementInfo(VALUE_NAME_HZ, mElm.Frequency);
					}
				}
				if (r == 4) {
					return new ElementInfo(VALUE_NAME_PHASE, double.Parse((mElm.Phase * 180 / Math.PI).ToString("0.00")));
				}
				if (r == 5) {
					return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((mElm.PhaseOffset * 180 / Math.PI).ToString("0.00")));
				}
				if (r == 6 && (mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_MONOPOLE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_DIPOLE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_POSITIVE
					|| mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_NEGATIVE)) {
					return new ElementInfo(VALUE_NAME_DUTY, mElm.DutyCycle * 100);
				}
			}
			if (c == 1) {
				if (r == 1) {
					return new ElementInfo("連動グループ", Link.Voltage);
				}
				if (r == 2) {
					return new ElementInfo("連動グループ", Link.Bias);
				}
				if (r == 3) {
					return new ElementInfo("連動グループ", Link.Frequency);
				}
				if (r == 5) {
					return new ElementInfo("連動グループ", Link.PhaseOffset);
				}
				if (r < 5) {
					return new ElementInfo();
				}
			}
			return null;
		}

		public override void SetElementValue(int r, int c, ElementInfo ei) {
			if (c == 0) {
				if (r == 0) {
					var ow = mElm.WaveForm;
					mElm.WaveForm = (ElmVoltage.WAVEFORM)ei.Choice.SelectedIndex;
					if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC && ow != ElmVoltage.WAVEFORM.DC) {
						ei.NewDialog = true;
						mElm.Bias = 0;
					} else if (mElm.WaveForm != ow) {
						ei.NewDialog = true;
					}

					/* change duty cycle if we're changing to or from pulse */
					if (mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE && ow != ElmVoltage.WAVEFORM.PULSE_MONOPOLE ||
						mElm.WaveForm != ElmVoltage.WAVEFORM.PULSE_MONOPOLE && ow == ElmVoltage.WAVEFORM.PULSE_MONOPOLE) {
						mElm.DutyCycle = DEFAULT_PULSE_DUTY;
					}
					SetPoints();
				}
				if (r == 1) {
					mElm.MaxVoltage = ei.Value;
				}
				if (r == 2) {
					mElm.Bias = ei.Value;
				}
				if (r == 3) {
					/* adjust time zero to maintain continuity ind the waveform
                     * even though the frequency has changed. */
					var oldfreq = mElm.Frequency;
					mElm.Frequency = ei.Value;
					var maxfreq = 1 / (8 * ControlPanel.TimeStep);
					if (maxfreq < mElm.Frequency) {
						if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
							ControlPanel.TimeStep = 1 / (32 * mElm.Frequency);
						} else {
							mElm.Frequency = maxfreq;
						}
					}
				}
				if (r == 4) {
					mElm.Phase = ei.Value * Math.PI / 180;
				}
				if (r == 5) {
					mElm.PhaseOffset = ei.Value * Math.PI / 180;
				}
				if (mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_MONOPOLE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_DIPOLE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_POSITIVE
				   || mElm.WaveForm == ElmVoltage.WAVEFORM.PWM_NEGATIVE) {
					if (r == 6) {
						mElm.DutyCycle = ei.Value * .01;
					}
				}
			}
			if (c == 1) {
				if (r == 1) {
					Link.Voltage = (int)ei.Value;
				}
				if (r == 2) {
					Link.Bias = (int)ei.Value;
				}
				if (r == 3) {
					Link.Frequency = (int)ei.Value;
				}
				if (r == 5) {
					Link.PhaseOffset = (int)ei.Value;
				}
			}
			SetWaveform();
			SetTextPos();
		}

		public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
			var trb = adj.Slider;
			switch (ei.Name) {
			case VALUE_NAME_V:
			case VALUE_NAME_AMP:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				trb.Minimum = 0;
				trb.Maximum = 100;
				break;
			case VALUE_NAME_BIAS:
				adj.MinValue = 0;
				adj.MaxValue = 5;
				trb.Minimum = 0;
				trb.Maximum = 100;
				break;
			case VALUE_NAME_HZ:
				adj.MinValue = 0;
				adj.MaxValue = 1000;
				trb.Minimum = 0;
				trb.Maximum = 100;
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
			case VALUE_NAME_DUTY:
				adj.MinValue = 0;
				adj.MaxValue = 100;
				trb.Minimum = 0;
				trb.Maximum = 100;
				break;
			}
			return new EventHandler((s, e) => {
				var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				switch (ei.Name) {
				case VALUE_NAME_V:
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
				case VALUE_NAME_DUTY:
					mElm.DutyCycle = val * 0.01;
					break;
				}
				SetWaveform();
				SetTextPos();
			});
		}
	}
}
