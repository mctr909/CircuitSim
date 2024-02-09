using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class SwitchMulti : Switch {
		const int OPEN_HS = 8;
		const int BODY_LEN = 24;

		protected ElmSwitchMulti mElm;
		PointF[] mSwPoles;
		PointF[] mSwPosts;

		public SwitchMulti(Point pos) : base(pos, 0) {
			mElm = new ElmSwitchMulti();
			mElm.AllocNodes();
			Post.NoDiagonal = true;
		}

		public SwitchMulti(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmSwitchMulti();
			mElm.Position = st.nextTokenInt();
			mElm.Momentary = st.nextTokenBool(false);
			mElm.Link = st.nextTokenInt();
			mElm.ThrowCount = st.nextTokenInt();
			mElm.AllocNodes();
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH_MULTI; } }

		protected override void dump(List<object> optionList) {
			base.dump(optionList);
			optionList.Add(mElm.ThrowCount);
		}

		public override RectangleF GetSwitchRect() {
			var l1 = new RectangleF(mLead1.X, mLead1.Y, 0, 0);
			var s0 = new RectangleF(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
			var s1 = new RectangleF(mSwPoles[mElm.ThrowCount - 1].X, mSwPoles[mElm.ThrowCount - 1].Y, 0, 0);
			return RectangleF.Union(l1, RectangleF.Union(s0, s1));
		}

		public override void SetPoints() {
			base.SetPoints();
			setLeads(BODY_LEN);
			mSwPosts = new PointF[mElm.ThrowCount];
			mSwPoles = new PointF[2 + mElm.ThrowCount];
			int i;
			for (i = 0; i != mElm.ThrowCount; i++) {
				int hs = -OPEN_HS * (i - (mElm.ThrowCount - 1) / 2);
				if (mElm.ThrowCount == 2 && i == 0) {
					hs = OPEN_HS;
				}
				interpLead(ref mSwPoles[i], 1, hs);
				interpPost(ref mSwPosts[i], 1, hs);
			}
			mSwPoles[i] = mLead2; /* for center off */
			mElm.PosCount = mElm.ThrowCount;
			mElm.SetNodePos(Post.A, mSwPosts);
		}

		public override void Draw(CustomGraphics g) {
			/* draw first lead */
			drawLeadA();
			fillCircle(mLead1, 2.5f);

			/* draw other leads */
			for (int i = 0; i < mElm.ThrowCount; i++) {
				var pole = mSwPoles[i];
				drawLine(pole, mSwPosts[i]);
				fillCircle(pole, 2.5f);
			}
			/* draw switch */
			drawLine(mLead1, mSwPoles[mElm.Position]);

			updateDotCount();
			drawCurrentA(mCurCount);
			if (mElm.Position != 2) {
				drawCurrent(mSwPoles[mElm.Position], mSwPosts[mElm.Position], mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "スイッチ(" + (mElm.Link == 0 ? "S" : "D")
				+ "P" + ((mElm.ThrowCount > 2) ? mElm.ThrowCount + "T)" : "DT)");
			arr[1] = "電位：" + Utils.VoltageText(mElm.Volts[0]);
			arr[2] = "電流：" + Utils.CurrentAbsText(mElm.Current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 1) {
				return new ElementInfo("分岐数", mElm.ThrowCount);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 1) {
				if (ei.Value < 2) {
					ei.Value = 2;
				}
				mElm.ThrowCount = (int)ei.Value;
				mElm.AllocNodes();
				SetPoints();
			} else {
				base.SetElementValue(n, c, ei);
			}
		}
	}
}
