using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class Ammeter : BaseSymbol {
		const int FLAG_SHOWCURRENT = 1;

		ElmAmmeter mElm;
		PointF mMid;
		PointF[] mArrowPoly;
		PointF mTextPos;
		EScale mScale;

		public override BaseElement Element { get { return mElm; } }

		public Ammeter(Point pos) : base(pos) {
			mElm = new ElmAmmeter();
			mFlags = FLAG_SHOWCURRENT;
			mScale = EScale.AUTO;
		}

		public Ammeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmAmmeter(st);
			mScale = st.nextTokenEnum(EScale.AUTO);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.AMMETER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Meter);
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

			string s = "A";
			switch (mElm.Meter) {
			case ElmAmmeter.AM_VOL:
				s = TextUtils.UnitWithScale(mElm.Current, "A", mScale);
				break;
			case ElmAmmeter.AM_RMS:
				s = TextUtils.UnitWithScale(mElm.RmsI, "A(rms)", mScale);
				break;
			}
			if (Post.Vertical) {
				DrawCenteredText(s, mTextPos, -Math.PI / 2);
			} else {
				DrawCenteredText(s, mTextPos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "電流計";
			switch (mElm.Meter) {
			case ElmAmmeter.AM_VOL:
				arr[1] = "電流：" + TextUtils.Current(mElm.Current);
				break;
			case ElmAmmeter.AM_RMS:
				arr[1] = "電流(rms)：" + TextUtils.Current(mElm.RmsI);
				break;
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("表示", mElm.Meter, new string[] { "瞬時値", "実効値" });
			}
			if (r == 1) {
				return new ElementInfo("スケール", (int)mScale, new string[] { "自動", "A", "mA", "uA" });
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Meter = ei.Choice.SelectedIndex;
			}
			if (n == 1) {
				mScale = (EScale)ei.Choice.SelectedIndex;
			}
		}
	}
}
