using Circuit.Elements.Passive;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class Transformer : BaseSymbol {
		public const int FLAG_REVERSE = 4;

		const int BODY_LEN = 24;

		PointF mTermPri1;
		PointF mTermPri2;
		PointF mTermSec1;
		PointF mTermSec2;
		PointF mCoilPri1;
		PointF mCoilSec1;
		PointF mCoilPri2;
		PointF mCoilSec2;

		PointF[] mCore;
		PointF[] mDots;
		PointF[] mCoilPri;
		PointF[] mCoilSec;
		float mCoilWidth;
		float mCoilAngle;

		double mCurCounts1 = 0;
		double mCurCounts2 = 0;

		public double PInductance = 0.01;
		public double Ratio = 1.0;
		public double CouplingCoef = 0.999;
		public int Polarity = 1;

		public override bool HasConnection(int n1, int n2) {
			if (ComparePair(n1, n2, 0, 2)) {
				return true;
			}
			if (ComparePair(n1, n2, 1, 3)) {
				return true;
			}
			return false;
		}

		public Transformer(Point pos) : base(pos) {
			AllocateNodes();
			Post.NoDiagonal = true;
			ReferenceName = "T";
		}

		public Transformer(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			PInductance = st.nextTokenDouble(1e3);
			Ratio = st.nextTokenDouble(1);
			Element.I[ElmTransformer.CUR_PRI] = st.nextTokenDouble(0);
			Element.I[ElmTransformer.CUR_SEC] = st.nextTokenDouble(0);
			CouplingCoef = st.nextTokenDouble(0.999);
			AllocateNodes();
			Post.NoDiagonal = true;
		}

		protected override BaseElement Create() {
			return new ElmTransformer();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.TRANSFORMER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(PInductance.ToString("g3"));
			optionList.Add(Ratio);
			optionList.Add(Element.I[ElmTransformer.CUR_PRI].ToString("g3"));
			optionList.Add(Element.I[ElmTransformer.CUR_SEC].ToString("g3"));
			optionList.Add(CouplingCoef);
		}

		public override void Reset() {
			/* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
			Element.I[ElmTransformer.CUR_PRI] = Element.I[ElmTransformer.CUR_SEC] = 0;
			Element.I[ElmTransformer.CS_PRI] = Element.I[ElmTransformer.CS_SEC] = 0;
			Element.V[ElmTransformer.PRI_T] = Element.V[ElmTransformer.PRI_B] = 0;
			Element.V[ElmTransformer.SEC_T] = Element.V[ElmTransformer.SEC_B] = 0;
		}

		public override void Stamp() {
			/* equations for transformer:
             *   v1 = L1 di1/dt + M  di2/dt
             *   v2 = M  di1/dt + L2 di2/dt
             * we invert that to get:
             *   di1/dt = a1 v1 + a2 v2
             *   di2/dt = a3 v1 + a4 v2
             * integrate di1/dt using trapezoidal approx and we get:
             *   i1(t2) = i1(t1) + dt/2 (i1(t1) + i1(t2))
             *          = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1) +
             *                     a1 dt/2 v1(t2) + a2 dt/2 v2(t2)
             * the norton equivalent of this for i1 is:
             *  a. current source, I = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1)
             *  b. resistor, G = a1 dt/2
             *  c. current source controlled by voltage v2, G = a2 dt/2
             * and for i2:
             *  a. current source, I = i2(t1) + a3 dt/2 v1(t1) + a4 dt/2 v2(t1)
             *  b. resistor, G = a3 dt/2
             *  c. current source controlled by voltage v2, G = a4 dt/2
             *
             * For backward euler,
             *
             *   i1(t2) = i1(t1) + a1 dt v1(t2) + a2 dt v2(t2)
             *
             * So the current source value is just i1(t1) and we use
             * dt instead of dt/2 for the resistor and VCCS.
             *
             * first winding goes from node 0 to 2, second is from 1 to 3 */
			var l1 = PInductance;
			var l2 = PInductance * Ratio * Ratio;
			var m = CouplingCoef * Math.Sqrt(l1 * l2);
			// build inverted matrix
			var deti = 1 / (l1 * l2 - m * m);
			var hdt = CircuitState.DeltaTime / 2; // we multiply dt/2 into a[0~3] here
			Element.Para[ElmTransformer.PRI_T] = l2 * deti * hdt;
			Element.Para[ElmTransformer.PRI_B] = -m * deti * hdt;
			Element.Para[ElmTransformer.SEC_T] = -m * deti * hdt;
			Element.Para[ElmTransformer.SEC_B] = l1 * deti * hdt;
			StampConductance(Element.Nodes[ElmTransformer.PRI_T], Element.Nodes[ElmTransformer.PRI_B], Element.Para[ElmTransformer.PRI_T]);
			StampConductance(Element.Nodes[ElmTransformer.SEC_T], Element.Nodes[ElmTransformer.SEC_B], Element.Para[ElmTransformer.SEC_B]);
			StampVCCurrentSource(
				Element.Nodes[ElmTransformer.SEC_T], Element.Nodes[ElmTransformer.SEC_B],
				Element.Nodes[ElmTransformer.PRI_T], Element.Nodes[ElmTransformer.PRI_B], Element.Para[ElmTransformer.PRI_B]);
			StampVCCurrentSource(
				Element.Nodes[ElmTransformer.PRI_T], Element.Nodes[ElmTransformer.PRI_B],
				Element.Nodes[ElmTransformer.SEC_T], Element.Nodes[ElmTransformer.SEC_B], Element.Para[ElmTransformer.SEC_T]);
			StampRightSide(Element.Nodes[ElmTransformer.PRI_T]);
			StampRightSide(Element.Nodes[ElmTransformer.PRI_B]);
			StampRightSide(Element.Nodes[ElmTransformer.SEC_T]);
			StampRightSide(Element.Nodes[ElmTransformer.SEC_B]);
		}

		public override void Drag(Point pos) {
			pos = SnapGrid(pos);
			Post.B = pos;
			SetPoints();
		}

		public override void SetPoints() {
			if (Post.B.Y < Post.A.Y + BODY_LEN) {
				Post.B.Y = Post.A.Y + BODY_LEN;
			}
			if (Post.B.X < Post.A.X + BODY_LEN) {
				Post.B.X = Post.A.X + BODY_LEN;
			}
			var width = Math.Max(BODY_LEN, Math.Abs(Post.B.X - Post.A.X));
			var height = Math.Max(BODY_LEN, Math.Abs(Post.B.Y - Post.A.Y));
			base.SetPoints();

			mTermPri1 = Post.A;
			mTermSec1 = Post.B;
			mTermSec1.Y = mTermPri1.Y;

			InterpolationPoint(mTermPri1, mTermSec1, out mTermPri2, 0, -Post.Dsign * height);
			InterpolationPoint(mTermPri1, mTermSec1, out mTermSec2, 1, -Post.Dsign * height);

			var pce = 0.5 - 10.0 / width;
			InterpolationPoint(mTermPri1, mTermSec1, out mCoilPri1, pce);
			InterpolationPoint(mTermPri1, mTermSec1, out mCoilSec1, 1 - pce);
			InterpolationPoint(mTermPri2, mTermSec2, out mCoilPri2, pce);
			InterpolationPoint(mTermPri2, mTermSec2, out mCoilSec2, 1 - pce);

			var pcd = 0.5 - 1.0 / width;
			mCore = new PointF[4];
			InterpolationPoint(mTermPri1, mTermSec1, out mCore[0], pcd);
			InterpolationPoint(mTermPri1, mTermSec1, out mCore[1], 1 - pcd);
			InterpolationPoint(mTermPri2, mTermSec2, out mCore[2], pcd);
			InterpolationPoint(mTermPri2, mTermSec2, out mCore[3], 1 - pcd);

			if (-1 == Polarity) {
				mDots = new PointF[2];
				var dotp = Math.Abs(7.0 / height);
				InterpolationPoint(mCoilPri1, mCoilPri2, out mDots[0], dotp, -7 * Post.Dsign);
				InterpolationPoint(mCoilSec2, mCoilSec1, out mDots[1], dotp, -7 * Post.Dsign);
				var x = mTermSec1;
				mTermSec1 = mTermSec2;
				mTermSec2 = x;
				var t = mCoilSec1;
				mCoilSec1 = mCoilSec2;
				mCoilSec2 = t;
			} else {
				mDots = null;
			}

			SetCoilPos(mCoilPri1, mCoilPri2, 90 * Post.Dsign, out mCoilPri);
			SetCoilPos(mCoilSec1, mCoilSec2, -90 * Post.Dsign * Polarity, out mCoilSec);
			SetNamePos();

			SetNodePos(mTermPri1, mTermSec1, mTermPri2, mTermSec2);
		}

		public override double Distance(Point p) {
			if (Polarity < 0) {
				return Math.Min(
					Post.DistanceOnLine(mTermPri1, mCoilPri1, p), Math.Min(
					Post.DistanceOnLine(mTermSec2, mCoilSec2, p), Math.Min(
					Post.DistanceOnLine(mTermPri2, mCoilPri2, p), Math.Min(
					Post.DistanceOnLine(mTermSec1, mCoilSec1, p), Math.Min(
					Post.DistanceOnLine(mCoilPri1, mCoilSec1, p),
					Post.DistanceOnLine(mCoilSec2, mCoilPri2, p)
				)))));
			} else {
				return Math.Min(
					Post.DistanceOnLine(mTermPri1, mCoilPri1, p), Math.Min(
					Post.DistanceOnLine(mTermSec1, mCoilSec1, p), Math.Min(
					Post.DistanceOnLine(mTermPri2, mCoilPri2, p), Math.Min(
					Post.DistanceOnLine(mTermSec2, mCoilSec2, p), Math.Min(
					Post.DistanceOnLine(mCoilPri1, mCoilSec2, p),
					Post.DistanceOnLine(mCoilSec1, mCoilPri2, p)
				)))));
			}
		}

		void SetCoilPos(PointF a, PointF b, float dir, out PointF[] pos) {
			var coilLen = (float)Distance(a, b);
			var loopCt = (int)Math.Ceiling(coilLen / 9);
			mCoilWidth = coilLen / loopCt;
			if (Angle(a, b) < 0) {
				mCoilAngle = -dir;
			} else {
				mCoilAngle = dir;
			}
			var arr = new List<PointF>();
			for (int loop = 0; loop != loopCt; loop++) {
				InterpolationPoint(a, b, out PointF p, (loop + 0.5) / loopCt, 0);
				arr.Add(p);
			}
			pos = arr.ToArray();
		}

		void SetNamePos() {
			mNamePos = new Point((int)mCore[0].X + 1, (int)mCore[0].Y - 8);
		}

		public override void Draw(CustomGraphics g) {
			DrawLine(mTermPri1, mCoilPri1);
			DrawLine(mTermSec1, mCoilSec1);
			DrawLine(mTermPri2, mCoilPri2);
			DrawLine(mTermSec2, mCoilSec2);

			foreach (var p in mCoilPri) {
				DrawArc(p, mCoilWidth, mCoilAngle, 180);
			}
			foreach (var p in mCoilSec) {
				DrawArc(p, mCoilWidth, mCoilAngle, -180);
			}

			DrawLine(mCore[0], mCore[2]);
			DrawLine(mCore[1], mCore[3]);

			if (mDots != null) {
				DrawCircle(mDots[0], 2.5f);
				DrawCircle(mDots[1], 2.5f);
			}

			UpdateDotCount(Element.I[ElmTransformer.CUR_PRI], ref mCurCounts1);
			UpdateDotCount(Element.I[ElmTransformer.CUR_SEC], ref mCurCounts2);
			DrawCurrent(mTermPri1, mCoilPri1, mCurCounts1);
			DrawCurrent(mCoilPri1, mCoilPri2, mCurCounts1);
			DrawCurrent(mCoilPri2, mTermPri2, mCurCounts1);
			DrawCurrent(mTermSec1, mCoilSec1, mCurCounts2);
			DrawCurrent(mCoilSec1, mCoilSec2, mCurCounts2);
			DrawCurrent(mCoilSec2, mTermSec2, mCurCounts2);

			if (ControlPanel.ChkShowName.Checked) {
				DrawCenteredText(ReferenceName, mNamePos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "トランス：" + TextUtils.Unit(PInductance, "H");
			arr[1] = "2次側巻数比：" + Ratio;
			arr[2] = "電位差(1次)：" + TextUtils.Voltage(Element.V[ElmTransformer.PRI_T] - Element.V[ElmTransformer.PRI_B]);
			arr[3] = "電位差(2次)：" + TextUtils.Voltage(Element.V[ElmTransformer.SEC_T] - Element.V[ElmTransformer.SEC_B]);
			arr[4] = "電流(1次)：" + TextUtils.Current(Element.I[ElmTransformer.CUR_PRI]);
			arr[5] = "電流(2次)：" + TextUtils.Current(Element.I[ElmTransformer.CUR_SEC]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("一次側インダクタンス(H)", PInductance);
			}
			if (r == 1) {
				return new ElementInfo("二次側巻数比", Ratio);
			}
			if (r == 2) {
				return new ElementInfo("結合係数(0～1)", CouplingCoef);
			}
			if (r == 3) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 4) {
				return new ElementInfo("極性反転", Polarity == -1);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				PInductance = ei.Value;
			}
			if (n == 1 && ei.Value > 0) {
				Ratio = ei.Value;
			}
			if (n == 2 && ei.Value > 0 && ei.Value < 1) {
				CouplingCoef = ei.Value;
			}
			if (n == 3) {
				ReferenceName = ei.Text;
				SetNamePos();
			}
			if (n == 4) {
				Polarity = ei.CheckBox.Checked ? -1 : 1;
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_REVERSE;
				} else {
					mFlags &= ~FLAG_REVERSE;
				}
				SetPoints();
			}
		}
	}
}
