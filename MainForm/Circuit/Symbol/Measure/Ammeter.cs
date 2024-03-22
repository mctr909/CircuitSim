using Circuit.Forms;
using Circuit.Elements.Measure;

namespace Circuit.Symbol.Measure {
	class Ammeter : BaseSymbol {
		const int FLAG_SHOWCURRENT = 1;
		const int TP_AMP = 0;
		const int TP_RMS = 1;
		const int TP_MAX = 2;
		const int TP_MIN = 3;

		ElmAmmeter mElm;
		PointF mMid;
		PointF[] mArrowPoly;
		PointF mTextPos;
		EScale mScale;
		int mMeter = TP_AMP;

		public override BaseElement Element { get { return mElm; } }

		public Ammeter(Point pos) : base(pos) {
			mElm = new ElmAmmeter();
			mFlags = FLAG_SHOWCURRENT;
			mScale = EScale.AUTO;
		}

		public Ammeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmAmmeter();
			mMeter = st.nextTokenInt();
			mScale = st.nextTokenEnum(EScale.AUTO);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.AMMETER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mMeter);
			optionList.Add(mScale);
		}

		public override void SetPoints() {
			base.SetPoints();
			InterpolationPost(ref mMid, 0.5 + 4 / Post.Len);
			CreateArrow(Post.A, mMid, out mArrowPoly, 9, 5);
			if (Post.Vertical) {
				InterpolationPost(ref mTextPos, 0.5, -21 * Post.Dsign);
			} else {
				InterpolationPost(ref mTextPos, 0.5, 12 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			base.Draw(g); /* BC required for highlighting */

			DrawLine(Post.A, Post.B);
			FillPolygon(mArrowPoly);
			DoDots();

			var s = DrawValues();
			if (Post.Vertical) {
				DrawCenteredText(s, mTextPos, -Math.PI / 2);
			} else {
				DrawCenteredText(s, mTextPos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "電流計";
			arr[1] = "電流：" + DrawValues();
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("表示", mMeter, new string[] { "瞬時値", "実効値", "最大値", "最小値" });
			}
			if (r == 1) {
				return new ElementInfo("スケール", (int)mScale, new string[] { "自動", "A", "mA", "uA" });
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

		string DrawValues() {
			switch (mMeter) {
			case TP_AMP:
				return TextUtils.UnitWithScale(mElm.Current, "A", mScale);
			case TP_RMS:
				return TextUtils.UnitWithScale(mElm.Rms, "A rms", mScale);
			case TP_MAX:
				return TextUtils.UnitWithScale(mElm.LastMax, "A pk", mScale);
			case TP_MIN:
				return TextUtils.UnitWithScale(mElm.LastMin, "A min", mScale);
			}
			return "";
		}
	}
}
