using System.Drawing;

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
			interpPost(ref mNamePos, 1 + 12 / Post.Len);
			interpPost(ref mC, 1);
			interpPost(ref mLa, 1, -6);
			interpPost(ref mLb, 1, 6);

			switch (mElm.WaveForm) {
			case ElmVoltage.WAVEFORM.DC:
			case ElmVoltage.WAVEFORM.NOISE:
				setLead1(1);
				break;
			default:
				if ((mFlags & FLAG_CLOCK) != 0) {
					setLead1(1);
				} else {
					if (Post.Len * 0.6 < BODY_LEN * 0.5) {
						setLead1(0);
					} else {
						setLead1(1 - BODY_LEN * 0.5 / Post.Len);
					}
				}
				break;
			}
		}

		public override void Draw(CustomGraphics g) {
			drawLeadA();
			drawRail();
			updateDotCount(-mElm.Current, ref mCurCount);
			if (CirSimForm.ConstructElm != this) {
				drawCurrentA(mCurCount);
			}
		}

		void drawRail() {
			if (mElm.WaveForm == ElmVoltage.WAVEFORM.DC) {
				drawLine(mLa, mLb);
				drawCircle(mC, 4);
				var v = mElm.GetVoltage();
				var s = Utils.UnitText(v, "V");
				drawCenteredText(s, mNamePos);
			} else if (mElm.WaveForm == ElmVoltage.WAVEFORM.SQUARE && (mFlags & FLAG_CLOCK) != 0) {
				drawCenteredText("Clock", mNamePos);
			} else if (mElm.WaveForm == ElmVoltage.WAVEFORM.NOISE) {
				drawCenteredText("Noise", mNamePos);
			} else {
				DrawWaveform(Post.B);
				if (ControlPanel.ChkShowValues.Checked) {
					var s = Utils.UnitText(mElm.MaxVoltage, "V\r\n");
					s += Utils.FrequencyText(mElm.Frequency, true) + "\r\n";
					s += Utils.PhaseText(mElm.Phase + mElm.PhaseOffset);
					drawValues(s, 23, 5);
				}
			}
		}
	}
}
