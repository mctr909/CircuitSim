using Circuit.Forms;
using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class SwitchMulti : Switch {
		const int OPEN_HS = 8;
		const int BODY_LEN = 24;

		ElmSwitchMulti mElmMulti;
		PointF[] mSwPoles;
		PointF[] mSwPosts;

		public SwitchMulti(Point pos) : base(pos, 0) {
			mElmMulti = new ElmSwitchMulti();
			mElm = mElmMulti;
			mElmMulti.alloc_nodes();
			Post.NoDiagonal = true;
		}

		public SwitchMulti(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElmMulti = new ElmSwitchMulti();
			mElm = mElmMulti;
			mElmMulti.Position = st.nextTokenInt();
			mElmMulti.Momentary = st.nextTokenBool(false);
			mElmMulti.Link = st.nextTokenInt();
			mElmMulti.ThrowCount = st.nextTokenInt();
			mElmMulti.alloc_nodes();
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH_MULTI; } }

		protected override void dump(List<object> optionList) {
			base.dump(optionList);
			optionList.Add(mElmMulti.ThrowCount);
		}

		public override RectangleF GetSwitchRect() {
			var l1 = new RectangleF(mLead1.X, mLead1.Y, 0, 0);
			var s0 = new RectangleF(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
			var s1 = new RectangleF(mSwPoles[mElmMulti.ThrowCount - 1].X, mSwPoles[mElmMulti.ThrowCount - 1].Y, 0, 0);
			return RectangleF.Union(l1, RectangleF.Union(s0, s1));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
			mSwPosts = new PointF[mElmMulti.ThrowCount];
			mSwPoles = new PointF[2 + mElmMulti.ThrowCount];
			int i;
			for (i = 0; i != mElmMulti.ThrowCount; i++) {
				int hs = -OPEN_HS * (i - (mElmMulti.ThrowCount - 1) / 2);
				if (mElmMulti.ThrowCount == 2 && i == 0) {
					hs = OPEN_HS;
				}
				InterpolationLead(ref mSwPoles[i], 1, hs);
				InterpolationPost(ref mSwPosts[i], 1, hs);
			}
			mSwPoles[i] = mLead2; /* for center off */
			mElmMulti.PosCount = mElmMulti.ThrowCount;
			mElmMulti.set_node_pos(Post.A, mSwPosts);
		}

		public override void Draw(CustomGraphics g) {
			/* draw first lead */
			DrawLeadA();
			FillCircle(mLead1, 2.5f);

			/* draw other leads */
			for (int i = 0; i < mElmMulti.ThrowCount; i++) {
				var pole = mSwPoles[i];
				DrawLine(pole, mSwPosts[i]);
				FillCircle(pole, 2.5f);
			}
			/* draw switch */
			DrawLine(mLead1, mSwPoles[mElmMulti.Position]);

			UpdateDotCount();
			DrawCurrentA(mCurCount);
			if (mElmMulti.Position != 2) {
				DrawCurrent(mSwPoles[mElmMulti.Position], mSwPosts[mElmMulti.Position], mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "スイッチ(" + (mElmMulti.Link == 0 ? "S" : "D")
				+ "P" + ((mElmMulti.ThrowCount > 2) ? mElmMulti.ThrowCount + "T)" : "DT)");
			arr[1] = "電位：" + TextUtils.Voltage(mElmMulti.volts[0]);
			arr[2] = "電流：" + TextUtils.CurrentAbs(mElmMulti.current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 1) {
				return new ElementInfo("分岐数", mElmMulti.ThrowCount);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 1) {
				if (ei.Value < 2) {
					ei.Value = 2;
				}
				mElmMulti.ThrowCount = (int)ei.Value;
				mElmMulti.alloc_nodes();
				SetPoints();
			} else {
				base.SetElementValue(n, c, ei);
			}
		}
	}
}
