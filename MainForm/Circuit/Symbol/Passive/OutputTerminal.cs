using Circuit.Elements.Passive;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class OutputTerminal : BaseSymbol {
		const int FLAG_INTERNAL = 1;

		protected static Dictionary<string, int> mNodeList = [];

		protected ElmLabeledNode mElm;
		protected PointF[] mTextPoly;
		protected RectangleF mTextRect;
		protected int mNodeId;
		protected bool mIsOutput = true;
		protected string mName = "Node";

		public static void ResetNodeList() {
			mNodeList.Clear();
		}

		public override int ConnectionNodeCount { get { return 2; } }
		// this is basically a wire, since it just connects two nodes together
		public override bool IsWire { get { return true; } }
		public override int InternalNodeCount {
			get {
				// this can happen at startup
				if (mNodeList == null) {
					return 0;
				}
				var elm = (ElmLabeledNode)Element;
				// node assigned already?
				if (null != mName && mNodeList.ContainsKey(mName)) {
					var nn = mNodeList[mName];
					mNodeId = nn;
					return 0;
				}
				// allocate a new one
				return 1;
			}
		}
		public override int VoltageSourceCount { get { return 1; } }
		// get connection node (which is the same as regular nodes for all elements but this one).
		// nodeIndex 0 is the terminal, nodeIndex 1 is the internal node shared by all nodes with same name
		public override int GetConnection(int nodeIndex) {
			if (nodeIndex == 0) {
				return mElm.Nodes[0];
			}
			return mNodeId;
		}

		public OutputTerminal(Point pos) : base(pos) {
			mElm = (ElmLabeledNode)Element;
			mIsOutput = true;
		}

		public OutputTerminal(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmLabeledNode)Element;
			st.nextToken(out mName);
			mName = TextUtils.UnEscape(mName);
			mIsOutput = true;
		}

		protected override BaseElement Create() {
			return new ElmLabeledNode();
		}

		public bool IsInternal { get { return (mFlags & FLAG_INTERNAL) != 0; } }

		public override DUMP_ID DumpId { get { return DUMP_ID.OUTPUT_TERMINAL; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mName);
		}

		public override void Stamp() {
			StampVoltageSource(mNodeId, mElm.Nodes[0], mElm.VoltSource, 0);
		}

		public override void SetNode(int index, int id) {
			base.SetNode(index, id);
			if (index == 1) {
				// assign new node
				mNodeList.Add(mName, id);
				mNodeId = id;
			}
		}

		public override double Distance(Point p) {
			return Math.Min(
				Post.DistanceOnLine(Post.A, Post.B, p),
				mTextRect.Contains(p) ? 0 : double.MaxValue
			);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetPolygon();
		}

		protected virtual void SetPolygon() {
			var txtSize = CustomGraphics.Instance.GetTextSize(mName);
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
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
			} else {
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			DrawCenteredText(mName, mNamePos, mTextRot);
			DrawPolyline(mTextPoly);
			UpdateDotCount(mElm.I[0], ref mCurCount);
			DrawCurrentA(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = mName;
			arr[1] = "電流：" + TextUtils.Current(mElm.I[0]);
			arr[2] = "電位：" + TextUtils.Voltage(mElm.V[0]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", mName);
			}
			if (r == 1) {
				return new ElementInfo("内部端子", IsInternal);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mName = ei.Text;
			}
			if (n == 1) {
				mFlags = ei.ChangeFlag(mFlags, FLAG_INTERNAL);
			}
			SetPolygon();
		}
	}
}
