using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class Sweep : BaseSymbol {
		const int FLAG_LOG = 1;
		const int FLAG_BIDIR = 2;
		const int SIZE = 28;

		ElmSweep mElm;

		public override BaseElement Element { get { return mElm; } }

		public Sweep(Point pos) : base(pos) {
			mElm = new ElmSweep();
			mFlags = FLAG_BIDIR;
			mElm.BothSides = 0 != (mFlags & FLAG_BIDIR);
		}

		public Sweep(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmSweep(st);
			mElm.IsLog = 0 != (mFlags & FLAG_LOG);
			mElm.BothSides = 0 != (mFlags & FLAG_BIDIR);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWEEP; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.MinF.ToString("g3"));
			optionList.Add(mElm.MaxF.ToString("g3"));
			optionList.Add(mElm.MaxV.ToString("g3"));
			optionList.Add(mElm.SweepTime.ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLead1(1 - 0.5 * SIZE / Post.Len);
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();

			int xc = Post.B.X;
			int yc = Post.B.Y;
			DrawCircle(Post.B, SIZE / 2);

			int wl = 11;
			int xl = 10;
			long tm = DateTime.Now.ToFileTimeUtc();
			tm %= 2000;
			if (tm > 1000) {
				tm = 2000 - tm;
			}
			double w = 1 + tm * 0.002;
			if (CircuitSymbol.IsRunning) {
				w = 1.01 + (mElm.Frequency - mElm.MinF) / (mElm.MaxF - mElm.MinF);
			}

			int x0 = 0;
			var y0 = 0.0f;
			for (int i = -xl; i <= xl; i++) {
				var yy = yc + (float)(0.95 * Math.Sin(i * Math.PI * w / xl) * wl);
				if (i == -xl) {
					x0 = xc + i;
					y0 = yy;
				} else {
					DrawLine(x0, y0, xc + i, yy);
					x0 = xc + i;
					y0 = yy;
				}
			}

			if (ControlPanel.ChkShowValues.Checked) {
				string s = Utils.UnitText(mElm.MaxV, "V\r\n")
					+ Utils.FrequencyText(mElm.Frequency);
				DrawValues(s, 25, 0);
			}

			UpdateDotCount(-mElm.Current, ref mCurCount);
			if (ConstructItem != this) {
				DrawCurrentA(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
			arr[1] = "I = " + Utils.CurrentAbsText(mElm.Current);
			arr[2] = "V = " + Utils.VoltageText(mElm.Volts[0]);
			arr[3] = "f = " + Utils.FrequencyText(mElm.Frequency);
			arr[4] = "range = " + Utils.FrequencyText(mElm.MinF) + " .. " + Utils.FrequencyText(mElm.MaxF);
			arr[5] = "time = " + Utils.UnitText(mElm.SweepTime, "s");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("振幅", mElm.MaxV);
			}
			if (r == 1) {
				return new ElementInfo("最小周波数", mElm.MinF);
			}
			if (r == 2) {
				return new ElementInfo("最大周波数", mElm.MaxF);
			}
			if (r == 3) {
				return new ElementInfo("スウィープ時間(sec)", mElm.SweepTime);
			}
			if (r == 4) {
				return new ElementInfo("周波数対数変化", (mFlags & FLAG_LOG) != 0);
			}
			if (r == 5) {
				return new ElementInfo("双方向周波数遷移", (mFlags & FLAG_BIDIR) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			var maxfreq = 1 / (8 * ControlPanel.TimeStep);
			if (n == 0) {
				mElm.MaxV = ei.Value;
			}
			if (n == 1) {
				mElm.MinF = ei.Value;
				if (mElm.MinF > maxfreq) {
					mElm.MinF = maxfreq;
				}
			}
			if (n == 2) {
				mElm.MaxF = ei.Value;
				if (mElm.MaxF > maxfreq) {
					mElm.MaxF = maxfreq;
				}
			}
			if (n == 3) {
				mElm.SweepTime = ei.Value;
			}
			if (n == 4) {
				mFlags &= ~FLAG_LOG;
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_LOG;
				}
				mElm.IsLog = 0 != (mFlags & FLAG_LOG);
			}
			if (n == 5) {
				mFlags &= ~FLAG_BIDIR;
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_BIDIR;
				}
				mElm.BothSides = 0 != (mFlags & FLAG_BIDIR);
			}
			mElm.setParams();
		}
	}
}
