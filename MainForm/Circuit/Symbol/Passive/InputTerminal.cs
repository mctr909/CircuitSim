namespace Circuit.Symbol.Passive {
	class InputTerminal : OutputTerminal {
		public InputTerminal(Point pos) : base(pos) {
			mElm.IsOutput = false;
		}

		public InputTerminal(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			mElm.IsOutput = false;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INPUT_TERMINAL; } }

		protected override void SetPolygon() {
			var txtSize = CustomGraphics.Instance.GetTextSize(mElm.Name);
			var txtW = txtSize.Width;
			var txtH = txtSize.Height;
			var txtOfsX = (txtW * 0.5 + 5.0) / Post.Len;
			var pw = (txtW + 5) / Post.Len;
			var ph = 0.5 * (txtH - 1);
			SetLead1(1);
			var p1 = new PointF();
			var p2 = new PointF();
			var p3 = new PointF();
			var p4 = new PointF();
			var p5 = new PointF();
			InterpolationPost(ref p1, 1 + pw, -ph);
			InterpolationPost(ref p2, 1 + pw, ph);
			InterpolationPost(ref p3, 1 + ph / Post.Len, ph);
			InterpolationPost(ref p4, 1, 0);
			InterpolationPost(ref p5, 1 + ph / Post.Len, -ph);
			mTextPoly = [
				p1, p2, p3, p4, p5, p1
			];
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
				InterpolationPost(ref mNamePos, 1 + txtOfsX, txtH / Post.Len);
			} else {
				InterpolationPost(ref mNamePos, 1 + txtOfsX, -txtH / Post.Len);
			}
		}
	}
}