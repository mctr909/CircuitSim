using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class CapacitorPolar : Capacitor {
		PointF mPlusPoint;

		ElmPolarCapacitor mElm;

		public CapacitorPolar(Point pos) : base(pos) {
			mElm = new ElmPolarCapacitor();
			mElm.Capacitance = mLastValue;
			mElm.MaxNegativeVoltage = 5;
			ReferenceName = mLastReferenceName;
		}

		public CapacitorPolar(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmPolarCapacitor();
			mElm.Capacitance = st.nextTokenDouble();
			mElm.VoltDiff = st.nextTokenDouble();
			mElm.MaxNegativeVoltage = st.nextTokenDouble();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.CAPACITOR_POLAR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Capacitance.ToString("g3"));
			optionList.Add(mElm.VoltDiff.ToString("g3"));
			optionList.Add(mElm.MaxNegativeVoltage);
		}

		public override void SetPoints() {
			base.SetPoints();
			var f = (Post.Len / 2 - 4) / Post.Len;
			if (Post.A.Y == Post.B.Y) {
				InterpolationPost(ref mPlusPoint, f - 5 / Post.Len, 5 * Post.Dsign);
			} else {
				InterpolationPost(ref mPlusPoint, f - 5 / Post.Len, -5 * Post.Dsign);
			}
			if (Post.B.Y > Post.A.Y) {
				mPlusPoint.Y += 1;
			}
			if (Post.A.Y > Post.B.Y) {
				mPlusPoint.Y += 3;
			}
		}

		public override void Draw(CustomGraphics g) {
			base.Draw(g);
			DrawCenteredText("+", mPlusPoint);
		}

		public override void GetInfo(string[] arr) {
			base.GetInfo(arr);
			arr[1] = "有極性コンデンサ：" + Utils.UnitText(mElm.Capacitance, "F");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 2) {
				return new ElementInfo("耐逆電圧", mElm.MaxNegativeVoltage);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 2 && ei.Value >= 0) {
				mElm.MaxNegativeVoltage = ei.Value;
			}
			base.SetElementValue(n, c, ei);
		}
	}
}
