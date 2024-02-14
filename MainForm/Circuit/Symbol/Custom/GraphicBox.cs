namespace Circuit.Symbol.Custom {
	class GraphicBox : Graphic {
		public GraphicBox(Point pos) : base(pos) {
			Post.B = pos;
		}

		public GraphicBox(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			Post.B = b;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.BOX; } }

		public override bool IsCreationFailed {
			get { return Post.BoxIsCreationFailed; }
		}

		public override void SelectRect(RectangleF r) {
			var x1 = Post.A.X;
			var y1 = Post.A.Y;
			var x2 = Post.B.X;
			var y2 = Post.B.Y;
			IsSelected =
				r.Contains(x1, y1) ||
				r.Contains(x2, y1) ||
				r.Contains(x2, y2) ||
				r.Contains(x1, y2);
		}

		public override double Distance(Point p) {
			var x1 = Post.A.X;
			var y1 = Post.A.Y;
			var x2 = Post.B.X;
			var y2 = Post.B.Y;
			return Math.Min(
				Post.DistanceOnLine(x1, y1, x2, y1, p.X, p.Y), Math.Min(
				Post.DistanceOnLine(x1, y1, x1, y2, p.X, p.Y), Math.Min(
				Post.DistanceOnLine(x2, y2, x2, y1, p.X, p.Y),
				Post.DistanceOnLine(x2, y2, x1, y2, p.X, p.Y)
			)));
		}

		public override void Drag(Point p) {
			Post.B = SnapGrid(p);
		}

		public override void Draw(CustomGraphics g) {
			var x1 = Post.A.X;
			var y1 = Post.A.Y;
			var x2 = Post.B.X;
			var y2 = Post.B.Y;
			if (x1 < x2 && y1 < y2) {
				DrawDashRectangle(x1, y1, x2 - x1, y2 - y1);
			} else if (x1 > x2 && y1 < y2) {
				DrawDashRectangle(x2, y1, x1 - x2, y2 - y1);
			} else if (x1 < x2 && y1 > y2) {
				DrawDashRectangle(x1, y2, x2 - x1, y1 - y2);
			} else {
				DrawDashRectangle(x2, y2, x1 - x2, y1 - y2);
			}
		}

		public override void GetInfo(string[] arr) { }

		public override ElementInfo GetElementInfo(int r, int c) { return null; }

		public override void SetElementValue(int n, int c, ElementInfo ei) { }
	}
}
