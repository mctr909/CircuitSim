using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class Rail : Voltage {
		protected const int FLAG_CLOCK = 1;

		PointF mC;
		PointF mLa;
		PointF mLb;

		public Rail(Point pos, ElmVoltage.WAVEFORM wf) : base(pos, wf) {
			mElm = new ElmRail(wf);
		}

		public Rail(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmRail(st);
			Link.Load(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.RAIL; } }

		public override void SetPoints() {
			base.SetPoints();
			InterpolationPost(ref mNamePos, 1 + 12 / Post.Len);
			InterpolationPost(ref mC, 1);
			InterpolationPost(ref mLa, 1, -6);
			InterpolationPost(ref mLb, 1, 6);

			switch (mElm.WaveForm) {
			case ElmVoltage.WAVEFORM.DC:
			case ElmVoltage.WAVEFORM.NOISE:
				SetLead1(1);
				break;
			default:
				if ((mFlags & FLAG_CLOCK) != 0) {
					SetLead1(1);
				} else {
					if (Post.Len * 0.6 < BODY_LEN * 0.5) {
						SetLead1(0);
					} else {
						SetLead1(1 - BODY_LEN * 0.5 / Post.Len);
					}
				}
				break;
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			drawRail();
			UpdateDotCount(-mElm.Current, ref mCurCount);
			if (ConstructItem != this) {
				DrawCurrentA(mCurCount);
			}
		}

		void drawRail() {
			if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC) {
				DrawLine(mLa, mLb);
				DrawCircle(mC, 4);
				var v = mElm.GetVoltage();
				var s = TextUtils.Unit(v, "V");
				DrawCenteredText(s, mNamePos);
			} else if (mElm.WaveForm == ElmVoltage.WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
				DrawCenteredText("Clock", mNamePos);
			} else if (mElm.WaveForm == ElmVoltage.WAVEFORM.NOISE) {
				DrawCenteredText("Noise", mNamePos);
			} else {
				DrawWaveform(Post.B);
				if (ControlPanel.ChkShowValues.Checked) {
					var s = TextUtils.Unit(mElm.MaxVoltage, "V\r\n");
					s += TextUtils.Frequency(mElm.Frequency, true) + "\r\n";
					s += TextUtils.Phase(mElm.Phase + mElm.PhaseOffset);
					DrawValues(s, 23, 5);
				}
			}
		}
	}
}
