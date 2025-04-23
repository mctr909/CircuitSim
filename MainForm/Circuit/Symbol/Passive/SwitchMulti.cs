using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class SwitchMulti : Switch {
		const int OPEN_HS = 8;
		const int BODY_LEN = 24;

		PointF[] mSwPoles;
		PointF[] mSwPosts;

		public override bool HasConnection(int n1, int n2) {
			return ComparePair(n1, n2, 0, 1 + mElm.Position);
		}

		public SwitchMulti(Point pos) : base(pos, 0) {
			AllocateNodes();
			Post.NoDiagonal = true;
		}

		public SwitchMulti(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm.Position = st.nextTokenInt();
			Momentary = st.nextTokenBool(false);
			Group = st.nextTokenInt();
			mElm.ThrowCount = st.nextTokenInt();
			AllocateNodes();
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH_MULTI; } }

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		protected override void dump(List<object> optionList) {
			base.dump(optionList);
			optionList.Add(mElm.ThrowCount);
		}

		public override void Stamp() {
			StampVoltageSource(mElm.Nodes[0], mElm.Nodes[mElm.Position + 1], mElm.VoltSource, 0);
		}

		public override RectangleF GetSwitchRect() {
			var l1 = new RectangleF(mLead1.X, mLead1.Y, 0, 0);
			var s0 = new RectangleF(mSwPoles[0].X, mSwPoles[0].Y, 0, 0);
			var s1 = new RectangleF(mSwPoles[mElm.ThrowCount - 1].X, mSwPoles[mElm.ThrowCount - 1].Y, 0, 0);
			return RectangleF.Union(l1, RectangleF.Union(s0, s1));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
			mSwPosts = new PointF[mElm.ThrowCount];
			mSwPoles = new PointF[2 + mElm.ThrowCount];
			int i;
			for (i = 0; i != mElm.ThrowCount; i++) {
				int hs = -OPEN_HS * (i - (mElm.ThrowCount - 1) / 2);
				if (mElm.ThrowCount == 2 && i == 0) {
					hs = OPEN_HS;
				}
				InterpolationLead(ref mSwPoles[i], 1, hs);
				InterpolationPost(ref mSwPosts[i], 1, hs);
			}
			mSwPoles[i] = mLead2; /* for center off */
			PosCount = mElm.ThrowCount;
			SetNodePos(Post.A, mSwPosts);
		}

		public override void Draw(CustomGraphics g) {
			/* draw first lead */
			DrawLeadA();
			FillCircle(mLead1, 2.5f);

			/* draw other leads */
			for (int i = 0; i < mElm.ThrowCount; i++) {
				var pole = mSwPoles[i];
				DrawLine(pole, mSwPosts[i]);
				FillCircle(pole, 2.5f);
			}
			/* draw switch */
			DrawLine(mLead1, mSwPoles[mElm.Position]);

			UpdateDotCount();
			DrawCurrentA(mCurCount);
			if (mElm.Position != 2) {
				DrawCurrent(mSwPoles[mElm.Position], mSwPosts[mElm.Position], mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "スイッチ(" + (Group == 0 ? "S" : "D")
				+ "P" + ((mElm.ThrowCount > 2) ? mElm.ThrowCount + "T)" : "DT)");
			arr[1] = "電位：" + TextUtils.Voltage(mElm.V[0]);
			arr[2] = "電流：" + TextUtils.CurrentAbs(mElm.I[0]);
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
				AllocateNodes();
				SetPoints();
			} else {
				base.SetElementValue(n, c, ei);
			}
		}
	}
}
