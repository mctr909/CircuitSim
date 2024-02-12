namespace Circuit {
	public abstract class BaseSymbol {
		public const int GRID_SIZE = 8;
		public const int GRID_MASK = ~(GRID_SIZE - 1);
		public const int GRID_ROUND = GRID_SIZE / 2 - 1;
		public const int CURRENT_DOT_SIZE = GRID_SIZE;

		static BaseSymbol mSelected = null;

		public static double CurrentMult { get; set; } = 0;
		public static List<Adjustable> Adjustables { get; private set; } = new List<Adjustable>();
		public static BaseSymbol ConstructItem { get; set; }

		protected BaseSymbol(Point pos) {
			Post = new Post(pos);
			mFlags = 0;
		}

		protected BaseSymbol(Point p1, Point p2, int f) {
			Post = new Post(p1, p2);
			mFlags = f;
		}

		#region [property]
		public abstract DUMP_ID DumpId { get; }

		public abstract BaseElement Element { get; }

		public string ReferenceName { get; set; }

		public Post Post { get; set; }

		public bool IsSelected { get; set; }

		public bool IsMouseElm {
			get {
				if (null == mSelected) {
					return false;
				}
				return mSelected.Equals(this);
			}
		}

		public bool NeedsHighlight {
			get {
				if (null == mSelected) {
					return IsSelected;
				}
				return mSelected.Equals(this) || IsSelected;
			}
		}

		public virtual bool IsCreationFailed { get { return Post.IsCreationFailed; } }

		public virtual bool CanViewInScope { get { return Element.TermCount <= 2; } }
		#endregion

		#region [protected variable]
		protected PointF mLead1;
		protected PointF mLead2;
		protected PointF mNamePos;
		protected PointF mValuePos;
		protected int mFlags;
		protected double mCurCount;
		protected double mTextRot;
		protected virtual BaseLink mLink { get; set; } = new BaseLink();
		#endregion

		#region [public method]
		public double DistancePostA(Point p) {
			return Distance(Post.A, p);
		}
		public double DistancePostB(Point p) {
			return Distance(Post.B, p);
		}
		public string Dump() {
			var valueList = new List<object>();
			valueList.Add(DumpId);
			Post.Dump(valueList);
			valueList.Add(mFlags);
			dump(valueList);
			mLink.Dump(valueList);
			if (!string.IsNullOrWhiteSpace(ReferenceName)) {
				valueList.Add(Utils.Escape(ReferenceName));
			}
			return string.Join(" ", valueList.ToArray());
		}
		public void SetPosition(int ax, int ay, int bx, int by) {
			Post.SetPosition(ax, ay, bx, by);
			SetPoints();
		}
		public void Move(int dx, int dy) {
			Post.Move(dx, dy);
			SetPoints();
		}
		public void Move(int dx, int dy, EPOST n) {
			Post.Move(dx, dy, n);
			SetPoints();
		}
		public bool AllowMove(int dx, int dy) {
			int nx = Post.A.X + dx;
			int ny = Post.A.Y + dy;
			int nx2 = Post.B.X + dx;
			int ny2 = Post.B.Y + dy;
			for (int i = 0; i != CircuitSymbol.Count; i++) {
				var ce = CircuitSymbol.List[i];
				var ceP1 = ce.Post.A;
				var ceP2 = ce.Post.B;
				if (ceP1.X == nx && ceP1.Y == ny && ceP2.X == nx2 && ceP2.Y == ny2) {
					return false;
				}
				if (ceP1.X == nx2 && ceP1.Y == ny2 && ceP2.X == nx && ceP2.Y == ny) {
					return false;
				}
			}
			return true;
		}
		public void FlipPosts() {
			Post.FlipPosts();
			SetPoints();
		}
		public void Select(bool v) {
			if (v) {
				mSelected = this;
			} else if (mSelected == this) {
				mSelected = null;
			}
		}
		#endregion

		#region [public virtual method]
		public virtual double Distance(Point p) {
			return Utils.DistanceOnLine(Post.A, Post.B, p);
		}
		public virtual void Delete() {
			if (mSelected == this) {
				mSelected = null;
			}
			if (Adjustables == null) {
				return;
			}
			for (int i = Adjustables.Count - 1; i >= 0; i--) {
				var adj = Adjustables[i];
				if (adj.UI == this) {
					adj.DeleteSlider();
					Adjustables.RemoveAt(i);
				}
			}
		}
		public virtual void Draw(CustomGraphics g) { }
		public virtual void Drag(Point pos) {
			Post.Drag(SnapGrid(pos));
			SetPoints();
		}
		public virtual void SelectRect(RectangleF r) {
			IsSelected = r.IntersectsWith(Post.GetRect());
		}
		public virtual void SetPoints() {
			Post.SetValue();
			Element.SetNodePos(Post.A, Post.B);
		}
		public virtual void GetInfo(string[] arr) { }
		public virtual ElementInfo GetElementInfo(int r, int c) { return null; }
		public virtual void SetElementValue(int r, int c, ElementInfo ei) { }
		public virtual EventHandler CreateSlider(ElementInfo ei, Adjustable adj) { return null; }
		#endregion

		#region [protected method]
		protected virtual void dump(List<object> optionList) { }

		/// <summary>
		/// update and draw current for simple two-terminal element
		/// </summary>
		protected void DoDots() {
			UpdateDotCount();
			if (ConstructItem != this) {
				DrawCurrent(Post.A, Post.B, mCurCount);
			}
		}

		/// <summary>
		///  update dot positions (curcount) for drawing current (general case for multiple currents)
		/// </summary>
		/// <param name="current"></param>
		/// <param name="count"></param>
		protected void UpdateDotCount(double current, ref double count) {
			if (!CircuitSymbol.IsRunning) {
				return;
			}
			var speed = current * CurrentMult;
			speed %= CURRENT_DOT_SIZE;
			count += speed;
		}

		/// <summary>
		/// update dot positions (curcount) for drawing current (simple case for single current)
		/// </summary>
		protected void UpdateDotCount() {
			UpdateDotCount(Element.Current, ref mCurCount);
		}

		protected void GetBasicInfo(int begin, params string[] arr) {
			arr[begin] = "電流：" + Utils.CurrentAbsText(Element.Current);
			arr[begin + 1] = "電位差：" + Utils.VoltageAbsText(Element.VoltageDiff);
		}

		/// <summary>
		/// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
		/// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
		/// </summary>
		/// <param name="len"></param>
		protected void SetLeads(int bodyLength) {
			if (Post.Len < bodyLength || bodyLength == 0) {
				mLead1 = Post.A;
				mLead2 = Post.B;
				return;
			}
			SetLead1((Post.Len - bodyLength) / (2 * Post.Len));
			SetLead2((Post.Len + bodyLength) / (2 * Post.Len));
		}

		protected void SetLead1(double w) {
			InterpolationPost(ref mLead1, w);
		}

		protected void SetLead2(double w) {
			InterpolationPost(ref mLead2, w);
		}

		protected void InterpolationPost(ref PointF p, double f) {
			p.X = (float)(Post.A.X * (1 - f) + Post.B.X * f);
			p.Y = (float)(Post.A.Y * (1 - f) + Post.B.Y * f);
		}

		protected void InterpolationPost(ref PointF p, double f, double g) {
			var gx = Post.B.Y - Post.A.Y;
			var gy = Post.A.X - Post.B.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				p.X = Post.A.X;
				p.Y = Post.A.Y;
			} else {
				g /= r;
				p.X = (float)(Post.A.X * (1 - f) + Post.B.X * f + g * gx);
				p.Y = (float)(Post.A.Y * (1 - f) + Post.B.Y * f + g * gy);
			}
		}

		protected void InterpolationPostAB(ref PointF a, ref PointF b, double f, double g) {
			var gx = Post.B.Y - Post.A.Y;
			var gy = Post.A.X - Post.B.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				a = Post.A;
				b = Post.B;
			} else {
				g /= r;
				a.X = (float)(Post.A.X * (1 - f) + Post.B.X * f + g * gx);
				a.Y = (float)(Post.A.Y * (1 - f) + Post.B.Y * f + g * gy);
				b.X = (float)(Post.A.X * (1 - f) + Post.B.X * f - g * gx);
				b.Y = (float)(Post.A.Y * (1 - f) + Post.B.Y * f - g * gy);
			}
		}

		protected void InterpolationLead(ref PointF p, double f) {
			p.X = (float)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f);
			p.Y = (float)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f);
		}

		protected void InterpolationLead(ref PointF p, double f, double g) {
			var gx = mLead2.Y - mLead1.Y;
			var gy = mLead1.X - mLead2.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				p.X = mLead1.X;
				p.Y = mLead1.Y;
			} else {
				g /= r;
				p.X = (float)(mLead1.X * (1 - f) + mLead2.X * f + g * gx);
				p.Y = (float)(mLead1.Y * (1 - f) + mLead2.Y * f + g * gy);
			}
		}

		protected void InterpolationLeadAB(ref PointF a, ref PointF b, double f, double g) {
			var gx = mLead2.Y - mLead1.Y;
			var gy = mLead1.X - mLead2.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				a.X = mLead1.X;
				a.Y = mLead1.Y;
				b.X = mLead2.X;
				b.Y = mLead2.Y;
			} else {
				g /= r;
				a.X = (float)(mLead1.X * (1 - f) + mLead2.X * f + g * gx);
				a.Y = (float)(mLead1.Y * (1 - f) + mLead2.Y * f + g * gy);
				b.X = (float)(mLead1.X * (1 - f) + mLead2.X * f - g * gx);
				b.Y = (float)(mLead1.Y * (1 - f) + mLead2.Y * f - g * gy);
			}
		}

		protected void SetLinkedValues<T>(int linkID, double value) {
			mLink.SetValue(Element, linkID, value);
			if (mLink.GetGroup(linkID) == 0) {
				return;
			}
			for (int i = 0; i != CircuitSymbol.Count; i++) {
				var u2 = CircuitSymbol.List[i];
				if (u2 is T) {
					if (u2.mLink.GetGroup(linkID) == mLink.GetGroup(linkID)) {
						u2.mLink.SetValue(u2.Element, linkID, value);
					}
				}
			}
		}
		#endregion

		#region [draw method]
		protected void DrawLine(PointF a, PointF b) {
			CustomGraphics.Instance.DrawLine(a.X, a.Y, b.X, b.Y);
		}
		protected void DrawLine(float ax, float ay, float bx, float by) {
			CustomGraphics.Instance.DrawLine(ax, ay, bx, by);
		}
		protected void DrawDashRectangle(float x, float y, float w, float h) {
			CustomGraphics.Instance.DrawDashRectangle(x, y, w, h);
		}
		protected void DrawCircle(PointF p, float radius) {
			CustomGraphics.Instance.DrawCircle(p, radius);
		}
		protected void DrawArc(PointF p, float diameter, float start, float sweep) {
			CustomGraphics.Instance.DrawArc(p, diameter, start, sweep);
		}
		protected void DrawPolygon(PointF[] p) {
			CustomGraphics.Instance.DrawPolygon(p);
		}
		protected void DrawPolyline(PointF[] p) {
			CustomGraphics.Instance.DrawPolyline(p);
		}
		protected void FillCircle(PointF p, float radius) {
			CustomGraphics.Instance.FillCircle(p.X, p.Y, radius);
		}
		protected void FillPolygon(PointF[] p) {
			CustomGraphics.Instance.FillPolygon(p);
		}
		protected void DrawLeadA() {
			CustomGraphics.Instance.DrawLine(Post.A, mLead1);
		}
		protected void DrawLeadB() {
			CustomGraphics.Instance.DrawLine(mLead2, Post.B);
		}
		protected void Draw2Leads() {
			var g = CustomGraphics.Instance;
			g.DrawLine(Post.A, mLead1);
			g.DrawLine(mLead2, Post.B);
		}
		protected void DrawCurrent(PointF a, PointF b, double pos) {
			DrawCurrent(a.X, a.Y, b.X, b.Y, pos);
		}
		protected void DrawCurrent(float ax, float ay, float bx, float by, double pos) {
			if ((!CircuitSymbol.IsRunning) || ControlPanel.ChkPrintable.Checked || !ControlPanel.ChkShowCurrent.Checked) {
				return;
			}
			pos %= CURRENT_DOT_SIZE;
			if (pos < 0) {
				pos += CURRENT_DOT_SIZE;
			}
			var nx = bx - ax;
			var ny = by - ay;
			var r = (float)Math.Sqrt(nx * nx + ny * ny);
			nx /= r;
			ny /= r;
			for (var di = pos; di < r; di += CURRENT_DOT_SIZE) {
				var x0 = (int)(ax + di * nx);
				var y0 = (int)(ay + di * ny);
				CustomGraphics.Instance.DrawCurrent(x0, y0, 0.5f);
			}
		}
		protected void DrawCurrentA(double pos) {
			DrawCurrent(Post.A, mLead1, pos);
		}
		protected void DrawCurrentB(double pos) {
			DrawCurrent(mLead2, Post.B, pos);
		}
		protected void DrawLeftText(string text, float x, float y) {
			CustomGraphics.Instance.DrawLeftText(text, x, y);
		}
		protected void DrawCenteredText(string text, PointF centerPos, double rotateAngle = 0) {
			CustomGraphics.Instance.DrawCenteredText(text, centerPos, rotateAngle);
		}
		protected void DrawCenteredLText(string s, PointF p) {
			CustomGraphics.Instance.DrawCenteredLText(s, p);
		}
		protected void DrawValues(string s, int offsetX, int offsetY) {
			if (s == null) {
				return;
			}
			var g = CustomGraphics.Instance;
			var textSize = g.GetTextSize(s);
			var xc = Post.B.X;
			var yc = Post.B.Y;
			g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
		}
		protected void DrawValue(string s) {
			if (ControlPanel.ChkShowValues.Checked) {
				DrawCenteredText(s, mValuePos, mTextRot);
			}
		}
		protected void DrawName() {
			if (ControlPanel.ChkShowName.Checked) {
				DrawCenteredText(ReferenceName, mNamePos, mTextRot);
			}
		}
		#endregion

		#region [util method]
		/// <summary>
		/// calculate point fraction f between a and b, linearly interpolated, return it in c
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="ret"></param>
		/// <param name="f"></param>
		protected static void InterpolationPoint(PointF a, PointF b, out PointF ret, double f) {
			ret = new PointF(
				(float)(a.X * (1 - f) + b.X * f),
				(float)(a.Y * (1 - f) + b.Y * f)
			);
		}

		/// <summary>
		/// Returns a point fraction f along the line between a and b and offset perpendicular by g
		/// </summary>
		/// <param name="a">1st Point</param>
		/// <param name="b">2nd Point</param>
		/// <param name="ret">Returns interpolated point</param>
		/// <param name="f">Fraction along line</param>
		/// <param name="g">Fraction perpendicular to line</param>
		protected static void InterpolationPoint(PointF a, PointF b, out PointF ret, double f, double g) {
			var gx = b.Y - a.Y;
			var gy = a.X - b.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				ret = new PointF(a.X, a.Y);
			} else {
				g /= r;
				ret = new PointF(
					(float)(a.X * (1 - f) + b.X * f + g * gx),
					(float)(a.Y * (1 - f) + b.Y * f + g * gy)
				);
			}
		}

		/// <summary>
		/// Calculates two points fraction f along the line between a and b and offest perpendicular by +/-g
		/// </summary>
		/// <param name="a">1st point (In)</param>
		/// <param name="b">2nd point (In)</param>
		/// <param name="ret1">1st point (Out)</param>
		/// <param name="ret2">2nd point (Out)</param>
		/// <param name="f">Fraction along line</param>
		/// <param name="g">Fraction perpendicular to line</param>
		protected static void InterpolationPoint(PointF a, PointF b, out PointF ret1, out PointF ret2, double f, double g) {
			var gx = b.Y - a.Y;
			var gy = a.X - b.X;
			var r = Math.Sqrt(gx * gx + gy * gy);
			if (0.0 == r) {
				ret1 = new PointF(a.X, a.Y);
				ret2 = new PointF(b.X, b.Y);
			} else {
				g /= r;
				ret1 = new PointF(
					(float)(a.X * (1 - f) + b.X * f + g * gx),
					(float)(a.Y * (1 - f) + b.Y * f + g * gy)
				);
				ret2 = new PointF(
					(float)(a.X * (1 - f) + b.X * f - g * gx),
					(float)(a.Y * (1 - f) + b.Y * f - g * gy)
				);
			}
		}

		protected static void CreateArrow(PointF a, PointF b, out PointF[] ret, double al, double aw) {
			var adx = b.X - a.X;
			var ady = b.Y - a.Y;
			var l = Math.Sqrt(adx * adx + ady * ady);
			ret = new PointF[3];
			ret[0] = new PointF(b.X, b.Y);
			InterpolationPoint(a, b, out ret[1], out ret[2], 1.0 - al / l, aw);
		}

		protected static void CreateSchmitt(PointF a, PointF b, out PointF[] ret, double size, double ctr) {
			ret = new PointF[6];
			var hs = 3 * size;
			var h1 = 3 * size;
			var h2 = h1 * 2;
			var len = Distance(a, b);
			InterpolationPoint(a, b, out ret[0], ctr - h2 / len, hs);
			InterpolationPoint(a, b, out ret[1], ctr + h1 / len, hs);
			InterpolationPoint(a, b, out ret[2], ctr + h1 / len, -hs);
			InterpolationPoint(a, b, out ret[3], ctr + h2 / len, -hs);
			InterpolationPoint(a, b, out ret[4], ctr - h1 / len, -hs);
			InterpolationPoint(a, b, out ret[5], ctr - h1 / len, hs);
		}

		protected static double Angle(PointF o, PointF p) {
			var x = p.X - o.X;
			var y = p.Y - o.Y;
			return Math.Atan2(y, x);
		}

		protected static double Distance(PointF a, PointF b) {
			var x = b.X - a.X;
			var y = b.Y - a.Y;
			return Math.Sqrt(x * x + y * y);
		}

		public static int SnapGrid(int x) {
			return (x + GRID_ROUND) & GRID_MASK;
		}
		public static Point SnapGrid(int x, int y) {
			return new Point(
				(x + GRID_ROUND) & GRID_MASK,
				(y + GRID_ROUND) & GRID_MASK);
		}
		public static Point SnapGrid(Point pos) {
			return new Point(
				(pos.X + GRID_ROUND) & GRID_MASK,
				(pos.Y + GRID_ROUND) & GRID_MASK);
		}
		public static Adjustable FindAdjustable(BaseSymbol elm, int item) {
			for (int i = 0; i != Adjustables.Count; i++) {
				var a = Adjustables[i];
				if (a.UI == elm && a.EditItemR == item) {
					return a;
				}
			}
			return null;
		}
		#endregion
	}
}
