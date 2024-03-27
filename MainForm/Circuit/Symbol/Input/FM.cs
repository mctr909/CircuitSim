using Circuit.Forms;
using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class FM : BaseSymbol {
		const int FLAG_COS = 2;
		const int SIZE = 28;

		ElmFM mElm;

		public override BaseElement Element { get { return mElm; } }

		public FM(Point pos) : base(pos) {
			mElm = new ElmFM();
		}

		public FM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmFM();
			mElm.CarrierFreq = st.nextTokenDouble();
			mElm.Signalfreq = st.nextTokenDouble();
			mElm.MaxVoltage = st.nextTokenDouble();
			mElm.Deviation = st.nextTokenDouble();
			if ((mFlags & FLAG_COS) != 0) {
				mFlags &= ~FLAG_COS;
			}
			mElm.Reset();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.FM; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.CarrierFreq);
			optionList.Add(mElm.Signalfreq);
			optionList.Add(mElm.MaxVoltage);
			optionList.Add(mElm.Deviation);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLead1(1 - 0.5 * SIZE / Post.Len);
			InterpolationPost(ref mNamePos, 1);
			ReferenceName = "FM";
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			DrawCircle(Post.B, SIZE / 2);
			DrawCenteredText(ReferenceName, mNamePos);
			UpdateDotCount(-mElm.Current, ref mCurCount);
			if (ConstructItem != this) {
				DrawCurrentA(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "FM Source";
			arr[1] = "I = " + TextUtils.Current(mElm.Current);
			arr[2] = "V = " + TextUtils.Voltage(mElm.VoltageDiff());
			arr[3] = "cf = " + TextUtils.Frequency(mElm.CarrierFreq);
			arr[4] = "sf = " + TextUtils.Frequency(mElm.Signalfreq);
			arr[5] = "dev =" + TextUtils.Frequency(mElm.Deviation);
			arr[6] = "Vmax = " + TextUtils.Voltage(mElm.MaxVoltage);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("振幅", mElm.MaxVoltage);
			}
			if (r == 1) {
				return new ElementInfo("搬送波周波数", mElm.CarrierFreq);
			}
			if (r == 2) {
				return new ElementInfo("信号周波数", mElm.Signalfreq);
			}
			if (r == 3) {
				return new ElementInfo("周波数偏移(Hz)", mElm.Deviation);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.MaxVoltage = ei.Value;
			}
			if (n == 1) {
				mElm.CarrierFreq = ei.Value;
			}
			if (n == 2) {
				mElm.Signalfreq = ei.Value;
			}
			if (n == 3) {
				mElm.Deviation = ei.Value;
			}
		}
	}
}
