using Circuit.Elements.Logic;

namespace Circuit.Symbol.Logic {
	class Schmitt : InvertingSchmitt {
		public Schmitt(Point pos) : base(pos) {
			mElm = new ElmSchmitt();
		}

		public Schmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmSchmitt();
			mElm.SlewRate = st.nextTokenDouble(0.5);
			mElm.LowerTrigger = st.nextTokenDouble(1.66);
			mElm.UpperTrigger = st.nextTokenDouble(3.33);
			mElm.LogicOnLevel = st.nextTokenDouble(5);
			mElm.LogicOffLevel = st.nextTokenDouble(0);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SCHMITT; } }

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawPolygon(gatePoly);
			DrawPolygon(symbolPoly);
			UpdateDotCount(mElm.Current, ref mCurCount);
			DrawCurrentB(mCurCount);
		}

		public override void SetPoints() {
			base.SetPoints();
			int hs = 10;
			int ww = 12;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			SetLead1(0.5 - ww / Post.Len);
			SetLead2(0.5 + (ww - 4) / Post.Len);
			gatePoly = new PointF[3];
			InterpolationLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
			InterpolationPost(ref gatePoly[2], 0.5 + (ww - 2) / Post.Len);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "Schmitt Trigger~"; // ~ is for localization
		}
	}
}
