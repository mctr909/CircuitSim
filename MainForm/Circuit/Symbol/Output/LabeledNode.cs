using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class LabeledNode : BaseSymbol {
		const int FLAG_INTERNAL = 1;
		const int LabelSize = 17;

		ElmLabeledNode mElm;
		PointF[] mTextPoly;
		RectangleF mTextRect;

		public override BaseElement Element { get { return mElm; } }

		public LabeledNode(Point pos) : base(pos) {
			mElm = new ElmLabeledNode();
		}

		public LabeledNode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmLabeledNode(st);
		}

		public bool IsInternal { get { return (mFlags & FLAG_INTERNAL) != 0; } }

		public override DUMP_ID DumpId { get { return DUMP_ID.LABELED_NODE; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Text);
		}

		public override double Distance(Point p) {
			return Math.Min(
				Utils.DistanceOnLine(Post.A, Post.B, p),
				mTextRect.Contains(p) ? 0 : double.MaxValue
			);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetTextPos();
		}

		void SetTextPos() {
			var txtSize = CustomGraphics.Instance.GetTextSize(mElm.Text);
			var txtW = txtSize.Width;
			var txtH = txtSize.Height;
			var pw = txtW / Post.Len;
			var ph = 0.5 * (txtH - 1);
			SetLead1(1);
			var p1 = new PointF();
			var p2 = new PointF();
			var p3 = new PointF();
			var p4 = new PointF();
			var p5 = new PointF();
			InterpolationPost(ref p1, 1, -ph);
			InterpolationPost(ref p2, 1, ph);
			InterpolationPost(ref p3, 1 + pw, ph);
			InterpolationPost(ref p4, 1 + pw + ph / Post.Len, 0);
			InterpolationPost(ref p5, 1 + pw, -ph);
			mTextPoly = new PointF[] {
				p1, p2, p3, p4, p5, p1
			};
			var ax = p1.X;
			var ay = p1.Y;
			var bx = p4.X;
			var by = p3.Y;
			if (bx < ax) {
				var t = ax;
				ax = bx;
				bx = t;
			}
			if (by < ay) {
				var t = ay;
				ay = by;
				by = t;
			}
			mTextRect = new RectangleF(ax, ay, bx - ax + 1, by - ay + 1);
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
			} else {
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			DrawCenteredText(mElm.Text, mNamePos, mTextRot);
			DrawPolyline(mTextPoly);
			UpdateDotCount(mElm.Current, ref mCurCount);
			DrawCurrentA(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = mElm.Text;
			arr[1] = "電流：" + Utils.CurrentText(mElm.Current);
			arr[2] = "電位：" + Utils.VoltageText(mElm.Volts[0]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", mElm.Text);
			}
			if (r == 1) {
				return new ElementInfo("内部端子", IsInternal);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Text = ei.Text;
			}
			if (n == 1) {
				mFlags = ei.ChangeFlag(mFlags, FLAG_INTERNAL);
			}
			SetTextPos();
		}
	}
}
