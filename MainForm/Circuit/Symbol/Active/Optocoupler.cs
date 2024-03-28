using Circuit.Elements.Active;
using Circuit.Symbol.Custom;

namespace Circuit.Symbol.Active {
	class Optocoupler : Composite {
		static readonly int[] EXTERNAL_NODES = { 6, 2, 4, 5 };
		static readonly string MODEL_STRING
			= DUMP_ID.DIODE + " 6 1\r"
			+ DUMP_ID.CCCS + " 1 2 3 4\r"
			+ DUMP_ID.TRANSISTOR_N + " 3 4 5";

		const int CSPC = 8 * 2;
		const int CSPC2 = CSPC * 2;
		ElmOptocoupler mElm;
		Diode mDiode;
		Transistor mTransistor;
		Point[] mStubs;
		Point[] mPosts;
		PointF[] mRectPoints;
		PointF[] mArrow1;
		PointF[] mArrow2;

		public override BaseElement Element { get { return mElm; } }

		public Optocoupler(Point pos) : base(pos) {
			mElm = new ElmOptocoupler();
			mElm.LoadComposite(getCompNode(MODEL_STRING), CompList, EXTERNAL_NODES);
			mDiode = (Diode)CompList[0];
			mTransistor = (Transistor)CompList[2];
			Post.NoDiagonal = true;
		}

		public Optocoupler(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmOptocoupler();
			mElm.LoadComposite(getCompNode(MODEL_STRING), CompList, EXTERNAL_NODES);
			mDiode = (Diode)CompList[0];
			mTransistor = (Transistor)CompList[2];
			Post.NoDiagonal = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.OPTO_COUPLER; } }

		Point GetPinPos(int n, int px, int py, double dx, double dy, double dax, double day, int sx, int sy) {
			int pos = n % 2;
			var xa = (int)(px + CSPC2 * dx * pos + sx);
			var ya = (int)(py + CSPC2 * dy * pos + sy);
			return new Point((int)(xa + dax * CSPC2), (int)(ya + day * CSPC2));
		}

		public override void SetPoints() {
			base.SetPoints();

			// adapted from ChipElm
			int x0 = Post.A.X + CSPC;
			int y0 = Post.A.Y;
			var r = new Point(x0 - CSPC, y0 - CSPC / 2);
			var sizeX = 1.5f;
			int sizeY = 2;
			int xs = (int)(sizeX * CSPC2);
			int ys = sizeY * CSPC2 - CSPC - 3;
			mRectPoints = new PointF[] {
				new Point(r.X, r.Y + 3),
				new Point(r.X + xs, r.Y + 3),
				new Point(r.X + xs, r.Y + ys),
				new Point(r.X, r.Y + ys)
			};

			mPosts = new Point[] {
				GetPinPos(0, x0, y0, 0, 1, -0.5, 0, 0, 0),
				GetPinPos(1, x0, y0, 0, 1, -0.5, 0, 0, 0),
				GetPinPos(2, x0, y0, 0, 1, 0.5, 0, xs - CSPC2, 0),
				GetPinPos(3, x0, y0, 0, 1, 0.5, 0, xs - CSPC2, 0)
			};
			mElm.set_node_pos(mPosts);
			Post.B = mPosts[2];

			/* diode */
			mDiode.SetPosition(mPosts[0].X + 10, mPosts[0].Y, mPosts[1].X + 10, mPosts[1].Y);
			mStubs = new Point[4];
			mStubs[0] = mDiode.Post.A;
			mStubs[1] = mDiode.Post.B;

			/* transistor */
			int midp = (mPosts[2].Y + mPosts[3].Y) / 2;
			mTransistor.SetPosition(mPosts[2].X - 18, midp, mPosts[2].X - 6, midp);
			mStubs[2] = mTransistor.Terminal[1];
			mStubs[3] = mTransistor.Terminal[2];

			/* create little arrows */
			int sx1 = mStubs[0].X;
			int sx2 = sx1 + 17;
			int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
			int y = sy - 5;
			var p1 = new Point(sx1, y);
			var p2 = new Point(sx2, y);
			CreateArrow(p1, p2, out mArrow1, 5, 3);
			y = sy + 5;
			p1 = new Point(sx1, y);
			p2 = new Point(sx2, y);
			CreateArrow(p1, p2, out mArrow2, 5, 3);
		}

		public override void Draw(CustomGraphics g) {
			DrawPolygon(mRectPoints);

			/* draw stubs */
			for (int i = 0; i != 4; i++) {
				var a = mPosts[i];
				var b = mStubs[i];
				DrawLine(a, b);
			}

			mDiode.Draw(g);
			mTransistor.Draw(g);

			/* draw little arrows */
			var sx1 = mArrow1[0].X - 10;
			var sx2 = sx1 + 5;
			DrawLine(sx1, mArrow1[0].Y, sx2, mArrow1[0].Y);
			DrawLine(sx1, mArrow2[0].Y, sx2, mArrow2[0].Y);
			FillPolygon(mArrow1);
			FillPolygon(mArrow2);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "optocoupler";
		}
	}
}
