using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class Ammeter : BaseSymbol {
		const int FLAG_SHOWCURRENT = 1;

		ElmAmmeter mElm;
		PointF mMid;
		PointF[] mArrowPoly;
		PointF mTextPos;

		public override BaseElement Element { get { return mElm; } }

		public Ammeter(Point pos) : base(pos) {
			mElm = new ElmAmmeter();
			mFlags = FLAG_SHOWCURRENT;
		}

		public Ammeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmAmmeter(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.AMMETER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Meter);
			optionList.Add(mElm.Scale);
		}

		public override void SetPoints() {
			base.SetPoints();
			interpPost(ref mMid, 0.5 + 4 / Post.Len);
			Utils.CreateArrow(Post.A, mMid, out mArrowPoly, 9, 5);
			if (Post.Vertical) {
				interpPost(ref mTextPos, 0.5, -21 * Post.Dsign);
			} else {
				interpPost(ref mTextPos, 0.5, 12 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			base.Draw(g); /* BC required for highlighting */

			drawLine(Post.A, Post.B);
			fillPolygon(mArrowPoly);
			doDots();

			string s = "A";
			switch (mElm.Meter) {
			case ElmAmmeter.AM_VOL:
				s = Utils.UnitTextWithScale(mElm.Current, "A", mElm.Scale);
				break;
			case ElmAmmeter.AM_RMS:
				s = Utils.UnitTextWithScale(mElm.RmsI, "A(rms)", mElm.Scale);
				break;
			}
			if (Post.Vertical) {
				drawCenteredText(s, mTextPos, -Math.PI / 2);
			} else {
				drawCenteredText(s, mTextPos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "電流計";
			switch (mElm.Meter) {
			case ElmAmmeter.AM_VOL:
				arr[1] = "電流：" + Utils.CurrentText(mElm.Current);
				break;
			case ElmAmmeter.AM_RMS:
				arr[1] = "電流(rms)：" + Utils.CurrentText(mElm.RmsI);
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
				return new ElementInfo("スケール", (int)mElm.Scale, new string[] { "自動", "A", "mA", "uA" });
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Meter = ei.Choice.SelectedIndex;
			}
			if (n == 1) {
				mElm.Scale = (E_SCALE)ei.Choice.SelectedIndex;
			}
		}
	}
}
