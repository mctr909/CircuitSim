using Circuit.Forms;
using Circuit.Elements.Gate;

namespace Circuit.Symbol.Gate {
	class InvertingSchmitt : BaseSymbol {
		protected ElmInvertingSchmitt mElm;
		protected PointF[] gatePoly;
		protected PointF[] symbolPoly;

		PointF pcircle;
		double dlt;
		double dut;

		public override BaseElement Element { get { return mElm; } }

		public InvertingSchmitt(Point pos, int dummy) : base(pos) {
			Post.NoDiagonal = true;
		}

		public InvertingSchmitt(Point pos) : base(pos) {
			mElm = new ElmInvertingSchmitt();
			Post.NoDiagonal = true;
		}

		public InvertingSchmitt(Point p1, Point p2, int f) : base(p1, p2, f) {
			Post.NoDiagonal = true;
		}

		public InvertingSchmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmInvertingSchmitt(st);
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVERT_SCHMITT; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.SlewRate.ToString("g3"));
			optionList.Add(mElm.LowerTrigger);
			optionList.Add(mElm.UpperTrigger);
			optionList.Add(mElm.LogicOnLevel);
			optionList.Add(mElm.LogicOffLevel);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawPolygon(gatePoly);
			DrawPolygon(symbolPoly);
			DrawCircle(pcircle, 3);
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
			SetLead2(0.5 + (ww + 2) / Post.Len);
			InterpolationPost(ref pcircle, 0.5 + (ww - 2) / Post.Len);

			gatePoly = new PointF[3];
			InterpolationLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
			InterpolationPost(ref gatePoly[2], 0.5 + (ww - 5) / Post.Len);

			CreateSchmitt(Post.A, Post.B, out symbolPoly, 0.8, .5 - (ww - 7) / Post.Len);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "inverting Schmitt trigger";
			arr[1] = "Vin：" + TextUtils.Voltage(mElm.Volts[0]);
			arr[2] = "Vout：" + TextUtils.Voltage(mElm.Volts[1]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				dlt = mElm.LowerTrigger;
				return new ElementInfo("Lower threshold (V)", mElm.LowerTrigger);
			}
			if (r == 1) {
				dut = mElm.UpperTrigger;
				return new ElementInfo("Upper threshold (V)", mElm.UpperTrigger);
			}
			if (r == 2) {
				return new ElementInfo("Slew Rate (V/ns)", mElm.SlewRate);
			}
			if (r == 3) {
				return new ElementInfo("High Voltage (V)", mElm.LogicOnLevel);
			}
			if (r == 4) {
				return new ElementInfo("Low Voltage (V)", mElm.LogicOffLevel);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				dlt = ei.Value;
			}
			if (n == 1) {
				dut = ei.Value;
			}
			if (n == 2) {
				mElm.SlewRate = ei.Value;
			}
			if (n == 3) {
				mElm.LogicOnLevel = ei.Value;
			}
			if (n == 4) {
				mElm.LogicOffLevel = ei.Value;
			}
			if (dlt > dut) {
				mElm.UpperTrigger = dlt;
				mElm.LowerTrigger = dut;
			} else {
				mElm.UpperTrigger = dut;
				mElm.LowerTrigger = dlt;
			}
		}
	}
}
