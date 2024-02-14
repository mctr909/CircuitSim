using Circuit.Elements.Passive;

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
		ElmTransformer mElm;

		public override BaseElement Element { get { return mElm; } }

		public Transformer(Point pos) : base(pos) {
			mElm = new ElmTransformer();
			mElm.AllocNodes();
			Post.NoDiagonal = true;
			ReferenceName = "T";
		}

		public Transformer(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmTransformer {
				PInductance = st.nextTokenDouble(1e3),
				Ratio = st.nextTokenDouble(1)
			};
			mElm.Currents[0] = st.nextTokenDouble(0);
			mElm.Currents[1] = st.nextTokenDouble(0);
			mElm.CouplingCoef = st.nextTokenDouble(0.999);
			mElm.AllocNodes();
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.TRANSFORMER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.PInductance.ToString("g3"));
			optionList.Add(mElm.Ratio);
			optionList.Add(mElm.Currents[0].ToString("g3"));
			optionList.Add(mElm.Currents[1].ToString("g3"));
			optionList.Add(mElm.CouplingCoef);
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

			if (-1 == mElm.Polarity) {
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
			SetCoilPos(mCoilSec1, mCoilSec2, -90 * Post.Dsign * mElm.Polarity, out mCoilSec);
			SetNamePos();

			mElm.SetNodePos(mTermPri1, mTermSec1, mTermPri2, mTermSec2);
		}

		public override double Distance(Point p) {
			if (mElm.Polarity < 0) {
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

			UpdateDotCount(mElm.Currents[0], ref mElm.CurCounts[0]);
			UpdateDotCount(mElm.Currents[1], ref mElm.CurCounts[1]);
			DrawCurrent(mTermPri1, mCoilPri1, mElm.CurCounts[0]);
			DrawCurrent(mCoilPri1, mCoilPri2, mElm.CurCounts[0]);
			DrawCurrent(mCoilPri2, mTermPri2, mElm.CurCounts[0]);
			DrawCurrent(mTermSec1, mCoilSec1, mElm.CurCounts[1]);
			DrawCurrent(mCoilSec1, mCoilSec2, mElm.CurCounts[1]);
			DrawCurrent(mCoilSec2, mTermSec2, mElm.CurCounts[1]);

			if (ControlPanel.ChkShowName.Checked) {
				DrawCenteredText(ReferenceName, mNamePos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "トランス：" + TextUtils.Unit(mElm.PInductance, "H");
			arr[1] = "2次側巻数比：" + mElm.Ratio;
			arr[2] = "電位差(1次)：" + TextUtils.Voltage(mElm.Volts[ElmTransformer.PRI_T] - mElm.Volts[ElmTransformer.PRI_B]);
			arr[3] = "電位差(2次)：" + TextUtils.Voltage(mElm.Volts[ElmTransformer.SEC_T] - mElm.Volts[ElmTransformer.SEC_B]);
			arr[4] = "電流(1次)：" + TextUtils.Current(mElm.Currents[0]);
			arr[5] = "電流(2次)：" + TextUtils.Current(mElm.Currents[1]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("一次側インダクタンス(H)", mElm.PInductance);
			}
			if (r == 1) {
				return new ElementInfo("二次側巻数比", mElm.Ratio);
			}
			if (r == 2) {
				return new ElementInfo("結合係数(0～1)", mElm.CouplingCoef);
			}
			if (r == 3) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 4) {
				return new ElementInfo("極性反転", mElm.Polarity == -1);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				mElm.PInductance = ei.Value;
			}
			if (n == 1 && ei.Value > 0) {
				mElm.Ratio = ei.Value;
			}
			if (n == 2 && ei.Value > 0 && ei.Value < 1) {
				mElm.CouplingCoef = ei.Value;
			}
			if (n == 3) {
				ReferenceName = ei.Text;
				SetNamePos();
			}
			if (n == 4) {
				mElm.Polarity = ei.CheckBox.Checked ? -1 : 1;
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
