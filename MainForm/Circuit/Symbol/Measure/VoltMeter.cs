using Circuit.Forms;
using Circuit.Elements.Measure;

namespace Circuit.Symbol.Measure {
	class VoltMeter : BaseSymbol {
		protected const int TP_VOL = 0;
		protected const int TP_RMS = 1;
		protected const int TP_MAX = 2;
		protected const int TP_MIN = 3;
		protected const int TP_P2P = 4;

		protected const int FLAG_SHOWVOLTAGE = 1;

		protected PointF mCenter;

		PointF mPlusPoint;
		ElmVoltMeter mElm;
		EScale mScale;
		int mMeter = TP_VOL;

		public override BaseElement Element { get { return mElm; } }

		public VoltMeter(Point pos) : base(pos) {
			mElm = new ElmVoltMeter();
			/* default for new elements */
			mFlags = FLAG_SHOWVOLTAGE;
			mScale = EScale.AUTO;
		}

		public VoltMeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmVoltMeter();
			mMeter = st.nextTokenInt(mMeter);
			mScale = st.nextTokenEnum(EScale.AUTO);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.VOLTMETER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mMeter);
			optionList.Add(mScale);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(16);
			InterpolationPost(ref mCenter, 0.5);
			InterpolationPost(ref mPlusPoint, 8.0 / Post.Len, 6 * Post.Dsign);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			if (MustShowVoltage()) {
				DrawCenteredText(DrawValues(), mCenter);
			}
			DrawCenteredLText("+", mPlusPoint);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "電圧計";
			if (this is VoltMeter1Term) {
				arr[1] = "電位：";
			} else {
				arr[1] = "電位差：";
			}
			arr[1] += DrawValues();
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("表示", mMeter,
					new string[] { "瞬時値", "実効値", "最大値", "最小値", "P-P" }
				);
			}
			if (r == 1) {
				return new ElementInfo("スケール", (int)mScale, new string[] { "自動", "V", "mV", "uV" });
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mMeter = ei.Choice.SelectedIndex;
			}
			if (n == 1) {
				mScale = (EScale)ei.Choice.SelectedIndex;
			}
		}

		protected string DrawValues() {
			switch (mMeter) {
			case TP_VOL:
				return TextUtils.UnitWithScale(mElm.VoltageDiff, "V", mScale);
			case TP_RMS:
				return TextUtils.UnitWithScale(mElm.Rms, "V rms", mScale);
			case TP_MAX:
				return TextUtils.UnitWithScale(mElm.LastMax, "V pk", mScale);
			case TP_MIN:
				return TextUtils.UnitWithScale(mElm.LastMin, "V min", mScale);
			case TP_P2P:
				return TextUtils.UnitWithScale(mElm.LastMax - mElm.LastMin, "V p-p", mScale);
			}
			return "";
		}

		protected bool MustShowVoltage() {
			return (mFlags & FLAG_SHOWVOLTAGE) != 0;
		}
	}
}
