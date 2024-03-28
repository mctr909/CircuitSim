using System.Drawing.Drawing2D;

namespace MainForm;

public abstract class ElmBase : Editable {
	const double pi = 3.14159265358979323846;
	const int SCALE_AUTO = 0;
	const int SCALE_1 = 1;
	const int SCALE_M = 2;
	const int SCALE_MU = 3;

	public static readonly Point PInvalid = new(int.MinValue, int.MinValue);

	#region static variables
	static double voltageRange = 5;
	static int colorScaleCount = 32;
	static Color[] colorScale;
	static double currentMult, powerMult;

	// scratch points for convenience
	protected static Point ps1, ps2;

	protected static CirSim sim;
	public static Color whiteColor, selectColor, lightGrayColor;
	public static Font unitsFont;

	static string showFormat, shortFormat;
	static ElmBase? mouseElmRef = null;

	public static Color positiveColor = Color.Transparent;
	public static Color negativeColor = Color.Transparent;
	public static Color neutralColor = Color.Transparent;
	#endregion

	#region dinamic variables
	// initial point where user created element. For simple two-terminal elements,
	// this is the first node/post.
	int x, y;

	// point to which user dragged out element. For simple two-terminal elements,
	// this is the second node/post
	int x2, y2;

	public int flags, voltSource;
	public int[] nodes;

	// length along x and y axes, and sign of difference
	protected int dx, dy, dsign;

	int lastHandleGrabbed = -1;

	// length of element
	protected double dn;

	double dpx1, dpy1;

	// (x,y) and (x2,y2) as Point objects
	public Point point1, point2;

	// lead points (ends of wire stubs for simple two-terminal elements)
	public Point lead1, lead2;

	// voltages at each node
	public double[] volts;

	public double current, curcount;
	Rectangle boundingBox;

	// if subclasses set this to true, element will be horizontal or vertical only
	bool noDiagonal;

	public bool selected;
	#endregion

	// create new element with one post at xx,yy, to be dragged out by user
	protected ElmBase(int xx, int yy) {
		x = x2 = xx;
		y = y2 = yy;
		flags = getDefaultFlags();
		allocNodes();
		initBoundingBox();
	}

	// create element between xa,ya and xb,yb from undump
	protected ElmBase(int xa, int ya, int xb, int yb, int f) {
		x = xa;
		y = ya;
		x2 = xb;
		y2 = yb;
		flags = f;
		allocNodes();
		initBoundingBox();
	}

	void initBoundingBox() {
		boundingBox = new Rectangle()
		{
			X = Math.Min(x, x2),
			Y = Math.Min(y, y2),
			Width = Math.Abs(x2 - x) + 1,
			Height = Math.Abs(y2 - y) + 1
		};
	}

	#region protected virtual
	protected virtual int getPostCount() {
		return 2;
	}

	// number of internal nodes (nodes not visible in UI that are needed for
	// implementation)
	protected virtual int getInternalNodeCount() {
		return 0;
	}

	// number of voltage sources this element needs
	protected virtual int getVoltageSourceCount() {
		return 0;
	}

	protected virtual int getDumpType() {
		throw new NotImplementedException();
		// Seems necessary to work-around what appears to be a compiler
		// bug affecting OTAElm to make sure this method (which should really be
		// abstract) throws
		// an exception
	}

	protected virtual Type getDumpClass() {
		return GetType();
	}

	protected virtual int getDefaultFlags() {
		return 0;
	}

	// allocate nodes/volts arrays we need
	protected virtual void allocNodes() {
		var n = getPostCount() + getInternalNodeCount();
		// preserve voltages if possible
		if (nodes == null || nodes.Length != n) {
			nodes = new int[n];
			volts = new double[n];
		}
	}

	// notify this element that its pth node is n. This value n can be passed to
	// stampMatrix()
	protected virtual void setNode(int p, int n) {
		nodes[p] = n;
	}

	// notify this element that its nth voltage source is v. This value v can be
	// passed to stampVoltageSource(), etc and will be passed back in calls to
	// setCurrent()
	protected virtual void setVoltageSource(int n, int v) {
		// default implementation only makes sense for subclasses with one voltage
		// source. If we have 0 this isn't used, if we have >1 this won't work
		voltSource = v;
	}

	// int getVoltageSource() { return voltSource; } // Never used except for debug
	// code which is commented out

	protected virtual double getVoltageDiff() {
		return volts[0] - volts[1];
	}

	protected virtual bool nonLinear() {
		return false;
	}

	// set current for voltage source vn to c. vn will be the same value as in a
	// previous call to setVoltageSource(n, vn)
	protected virtual void setCurrent(int vn, double c) {
		current = c;
	}

	// get current for one- or two-terminal elements
	protected virtual double getCurrent() {
		return current;
	}

	protected virtual void setParentList(List<ElmBase> elmList) {
	}
	#endregion

	#region public static
	public static void initClass(CirSim s) {
		unitsFont = new Font("SansSerif", 12);
		sim = s;

		colorScale = new Color[colorScaleCount];

		ps1 = new Point();
		ps2 = new Point();

		showFormat = "####.###";
		shortFormat = "####.#";
	}

	public static void setColorScale() {
		if (positiveColor == Color.Transparent)
			positiveColor = Color.Green;
		if (negativeColor == Color.Transparent)
			negativeColor = Color.Red;
		if (neutralColor == Color.Transparent)
			neutralColor = Color.Gray;
		for (int i = 0; i != colorScaleCount; i++) {
			var p = i * 2.0 / colorScaleCount - 1;
			if (p < 0) {
				p *= -1;
				var n = 1 - p;
				colorScale[i] = Color.FromArgb(
					(int)(neutralColor.R * n + negativeColor.R * p),
					(int)(neutralColor.G * n + negativeColor.G * p),
					(int)(neutralColor.B * n + negativeColor.B * p)
				);
			} else {
				var n = 1 - p;
				colorScale[i] = Color.FromArgb(
					(int)(neutralColor.R * n + positiveColor.R * p),
					(int)(neutralColor.G * n + positiveColor.G * p),
					(int)(neutralColor.B * n + positiveColor.B * p)
				);
			}
		}
	}

	public static double distance(Point p1, Point p2) {
		var x = p1.X - p2.X;
		var y = p1.Y - p2.Y;
		return Math.Sqrt(x * x + y * y);
	}

	public static string getVoltageDText(double v) {
		return getUnitText(Math.Abs(v), "V");
	}

	public static string getVoltageText(double v) {
		return getUnitText(v, "V");
	}

	public static string getTimeText(double v) {
		if (v >= 60) {
			var h = Math.Floor(v / 3600);
			v -= 3600 * h;
			var m = Math.Floor(v / 60);
			v -= 60 * m;
			if (h == 0)
				return m + ":" + ((v >= 10) ? "" : "0") + v.ToString(showFormat);
			return h + ":" + ((m >= 10) ? "" : "0") + m + ":" + ((v >= 10) ? "" : "0") + v.ToString(showFormat);
		}
		return getUnitText(v, "s");
	}

	public static string format(double v, bool sf) {
		// if (sf && Math.abs(v) > 10)
		// return shortFormat.format(Math.round(v));
		return v.ToString(sf ? shortFormat : showFormat);
	}

	public static string getUnitText(double v, string u, bool sf) {
		var sp = sf ? "" : " ";
		var va = Math.Abs(v);
		if (va < 1e-14)
			// this used to return null, but then wires would display "null" with 0V
			return "0" + sp + u;
		if (va < 1e-9)
			return format(v * 1e12, sf) + sp + "p" + u;
		if (va < 1e-6)
			return format(v * 1e9, sf) + sp + "n" + u;
		if (va < 1e-3)
			return format(v * 1e6, sf) + sp + "µ" + u;
		if (va < 1)
			return format(v * 1e3, sf) + sp + "m" + u;
		if (va < 1e3)
			return format(v, sf) + sp + u;
		if (va < 1e6)
			return format(v * 1e-3, sf) + sp + "k" + u;
		if (va < 1e9)
			return format(v * 1e-6, sf) + sp + "M" + u;
		return format(v * 1e-9, sf) + sp + "G" + u;
	}

	public static string getUnitText(double v, string u) {
		return getUnitText(v, u, false);
	}

	public static string getShortUnitText(double v, string u) {
		return getUnitText(v, u, true);
	}

	public static string getCurrentText(double i) {
		return getUnitText(i, "A");
	}

	public static string getCurrentDText(double i) {
		return getUnitText(Math.Abs(i), "A");
	}

	public static string getUnitTextWithScale(double val, string utext, int scale) {
		if (scale == SCALE_1)
			return val.ToString(showFormat) + " " + utext;
		if (scale == SCALE_M)
			return (1e3 * val).ToString(showFormat) + " m" + utext;
		if (scale == SCALE_MU)
			return (1e6 * val).ToString(showFormat) + " µ" + utext;
		return getUnitText(val, utext);
	}

	// calculate point fraction f between a and b, linearly interpolated, return it
	// in c
	public static void interpPoint(Point a, Point b, ref Point c, double f) {
		c.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + .48);
		c.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + .48);
	}

	/**
	 * Returns a point fraction f along the line between a and b and offset
	 * perpendicular by g
	 * 
	 * @param a 1st Point
	 * @param b 2nd Point
	 * @param f Fraction along line
	 * @param g Fraction perpendicular to line
	 *          Returns interpolated point in c
	 */
	public static void interpPoint(Point a, Point b, ref Point c, double f, double g) {
		var gx = b.Y - a.Y;
		var gy = a.X - b.X;
		g /= Math.Sqrt(gx * gx + gy * gy);
		c.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + .48);
		c.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + .48);
	}

	/**
	 * Calculates two points fraction f along the line between a and b and offest
	 * perpendicular by +/-g
	 * 
	 * @param a 1st point (In)
	 * @param b 2nd point (In)
	 * @param c 1st point (Out)
	 * @param d 2nd point (Out)
	 * @param f Fraction along line
	 * @param g Fraction perpendicular to line
	 */
	public static void interpPoint2(Point a, Point b, ref Point c, ref Point d, double f, double g) {
		var gx = b.Y - a.Y;
		var gy = a.X - b.X;
		g /= Math.Sqrt(gx * gx + gy * gy);
		c.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + .48);
		c.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + .48);
		d.X = (int)Math.Floor(a.X * (1 - f) + b.X * f - g * gx + .48);
		d.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f - g * gy + .48);
	}

	// calculate point fraction f between a and b, linearly interpolated
	public static Point interpPoint(Point a, Point b, double f) {
		var p = new Point();
		interpPoint(a, b, ref p, f);
		return p;
	}

	/**
	 * Returns a point fraction f along the line between a and b and offset
	 * perpendicular by g
	 * 
	 * @param a 1st Point
	 * @param b 2nd Point
	 * @param f Fraction along line
	 * @param g Fraction perpendicular to line
	 * @return Interpolated point
	 */
	public static Point interpPoint(Point a, Point b, double f, double g) {
		var p = new Point();
		interpPoint(a, b, ref p, f, g);
		return p;
	}
	#endregion

	#region public virtual
	// dump component state for export/undo
	public virtual string dump() {
		var t = getDumpType();
		return $"{(t < 0x7F ? (char)t : t)} {x} {y} {x2} {y2} {flags}";
	}

	public virtual int getShortcut() {
		return 0;
	}

	// calculate post locations and other convenience values used for drawing.
	// Called when element is moved
	public virtual void setPoints() {
		dx = x2 - x;
		dy = y2 - y;
		dn = Math.Sqrt(dx * dx + dy * dy);
		dpx1 = dy / dn;
		dpy1 = -dx / dn;
		dsign = (dy == 0) ? Math.Sign(dx) : Math.Sign(dy);
		point1 = new Point(x, y);
		point2 = new Point(x2, y2);
	}

	// needed for calculating circuit bounds (need to special-case centered text
	// elements)
	public virtual bool isCenteredText() {
		return false;
	}

	// get position of nth node
	public virtual Point getPost(int n) {
		return (n == 0) ? point1 : (n == 1) ? point2 : PInvalid;
	}

	// get component info for display in lower right
	public virtual void getInfo(params string[] arr) {
	}

	public virtual string getScopeText(int v) {
		var info = new string[10];
		getInfo(info);
		return info[0];
	}

	// stamp matrix values for linear elements.
	// for non-linear elements, use this to stamp values that don't change each
	// iteration, and call stampRightSide() or stampNonLinear() as needed
	public virtual void stamp() {
	}

	// draw second point to xx, yy
	public virtual void drag(int xx, int yy) {
		xx = sim.snapGrid(xx);
		yy = sim.snapGrid(yy);
		if (noDiagonal) {
			if (Math.Abs(x - xx) < Math.Abs(y - yy)) {
				xx = x;
			} else {
				yy = y;
			}
		}
		x2 = xx;
		y2 = yy;
		setPoints();
	}

	// handle reset button
	public virtual void reset() {
		for (int i = 0; i != getPostCount() + getInternalNodeCount(); i++)
			volts[i] = 0;
		curcount = 0;
	}

	public virtual void delete() {
		if (mouseElmRef == this)
			mouseElmRef = null;
		sim.deleteSliders(this);
	}

	// stamp matrix values for non-linear elements
	public virtual void doStep() {
	}

	public virtual void startIteration() {
	}

	// calculate current in response to node voltages changing
	public virtual void calculateCurrent() {
	}

	// set voltage of x'th node, called by simulator logic
	public virtual void setNodeVoltage(int n, double c) {
		volts[n] = c;
		calculateCurrent();
	}

	public virtual double getCurrentIntoNode(int n) {
		// if we take out the getPostCount() == 2 it gives the wrong value for rails
		if (n == 0 && getPostCount() == 2)
			return -current;
		else
			return current;
	}

	public virtual double getPower() {
		return getVoltageDiff() * current;
	}

	public virtual void setMouseElm(bool v) {
		if (v)
			mouseElmRef = this;
		else if (mouseElmRef == this)
			mouseElmRef = null;
	}

	public virtual EditInfo? getEditInfo(int n) {
		return null;
	}

	public virtual void setEditValue(int n, EditInfo ei) {
	}
	#endregion

	#region public
	// get (global) node number of nth node
	public int getNode(int n) {
		return nodes[n];
	}

	public int getNodeAtPoint(int xp, int yp) {
		if (getPostCount() == 2)
			return (x == xp && y == yp) ? 0 : 1;
		for (int i = 0; i != getPostCount(); i++) {
			var p = getPost(i);
			if (p.X == xp && p.Y == yp)
				return i;
		}
		return 0;
	}

	public int getBasicInfo(string[] arr) {
		arr[1] = "I = " + getCurrentDText(getCurrent());
		arr[2] = "Vd = " + getVoltageDText(getVoltageDiff());
		return 3;
	}

	// get voltage of x'th node
	public double getPostVoltage(int x) {
		return volts[x];
	}

	// set/adjust bounding box used for selecting elements. getCircuitBounds() does
	// not use this!
	public void setBbox(int x1, int y1, int x2, int y2) {
		if (x1 > x2) {
			(x2, x1) = (x1, x2);
		}
		if (y1 > y2) {
			(y2, y1) = (y1, y2);
		}
		boundingBox = new Rectangle(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
	}

	// set bounding box for an element from p1 to p2 with width w
	public void setBbox(Point p1, Point p2, double w) {
		setBbox(p1.X, p1.Y, p2.X, p2.Y);
		var dpx = (int) (dpx1 * w);
		var dpy = (int) (dpy1 * w);
		adjustBbox(p1.X + dpx, p1.Y + dpy, p1.X - dpx, p1.Y - dpy);
	}

	// enlarge bbox to contain an additional rectangle
	public void adjustBbox(int x1, int y1, int x2, int y2) {
		if (x1 > x2) {
			(x2, x1) = (x1, x2);
		}
		if (y1 > y2) {
			(y2, y1) = (y1, y2);
		}
		x1 = Math.Min(boundingBox.X, x1);
		y1 = Math.Min(boundingBox.Y, y1);
		x2 = Math.Max(boundingBox.X + boundingBox.Width, x2);
		y2 = Math.Max(boundingBox.Y + boundingBox.Height, y2);
		boundingBox = new Rectangle(x1, y1, x2 - x1, y2 - y1);
	}

	public void adjustBbox(Point p1, Point p2) {
		adjustBbox(p1.X, p1.Y, p2.X, p2.Y);
	}

	// this is used to set the position of an internal element so we can draw it
	// inside the parent
	public void setPosition(int x_, int y_, int x2_, int y2_) {
		x = x_;
		y = y_;
		x2 = x2_;
		y2 = y2_;
		setPoints();
	}

	public void move(int dx, int dy) {
		x += dx;
		y += dy;
		x2 += dx;
		y2 += dy;
		boundingBox.X += dx;
		boundingBox.Y += dy;
		setPoints();
	}

	// determine if moving this element by (dx,dy) will put it on top of another
	// element
	public bool allowMove(int dx, int dy) {
		var nx = x + dx;
		var ny = y + dy;
		var nx2 = x2 + dx;
		var ny2 = y2 + dy;
		for (int i = 0; i != sim.elmList.Count; i++) {
			var ce = sim.getElm(i);
			if (ce.x == nx && ce.y == ny && ce.x2 == nx2 && ce.y2 == ny2)
				return false;
			if (ce.x == nx2 && ce.y == ny2 && ce.x2 == nx && ce.y2 == ny)
				return false;
		}
		return true;
	}

	public void movePoint(int n, int dx, int dy) {
		// modified by IES to prevent the user dragging points to create zero sized
		// nodes
		// that then render improperly
		var oldx = x;
		var oldy = y;
		var oldx2 = x2;
		var oldy2 = y2;
		if (n == 0) {
			x += dx;
			y += dy;
		} else {
			x2 += dx;
			y2 += dy;
		}
		if (x == x2 && y == y2) {
			x = oldx;
			y = oldy;
			x2 = oldx2;
			y2 = oldy2;
		}
		setPoints();
	}

	// called when an element is done being dragged out; returns true if it's zero
	// size and should be deleted
	public bool creationFailed() {
		return x == x2 && y == y2;
	}

	// calculate lead points for an element of length len. Handy for simple
	// two-terminal elements.
	// Posts are where the user connects wires; leads are ends of wire stubs drawn
	// inside the element.
	public void calcLeads(int len) {
		if (dn < len || len == 0) {
			lead1 = point1;
			lead2 = point2;
			return;
		}
		lead1 = interpPoint(point1, point2, (dn - len) / (2 * dn));
		lead2 = interpPoint(point1, point2, (dn + len) / (2 * dn));
	}

	public Point[] newPointArray(int n) {
		var a = new Point[n];
		while (n > 0)
			a[--n] = new Point();
		return a;
	}

	public Point[] calcArrow(Point a, Point b, double al, double aw) {
		var p1 = new Point();
		var p2 = new Point();
		var adx = b.X - a.X;
		var ady = b.Y - a.Y;
		var l = Math.Sqrt(adx * adx + ady * ady);
		interpPoint2(a, b, ref p1, ref p2, 1 - al / l, aw);
		return [b, p1, p2];
	}

	public Point[] createPolygon(params Point[] a) {
		var p = new List<Point>();
		for (int i = 0; i != a.Length; i++)
			p.Add(a[i]);
		return [.. p];
	}

	public Point[] getSchmittPolygon(float gsize, float ctr) {
		var hs = 3 * gsize;
		var h1 = 3 * gsize;
		var h2 = h1 * 2;
		var len = distance(lead1, lead2);
		var pts = new Point[] {
			interpPoint(lead1, lead2, ctr - h2 / len, hs),
			interpPoint(lead1, lead2, ctr + h1 / len, hs),
			interpPoint(lead1, lead2, ctr + h1 / len, -hs),
			interpPoint(lead1, lead2, ctr + h2 / len, -hs),
			interpPoint(lead1, lead2, ctr - h1 / len, -hs),
			interpPoint(lead1, lead2, ctr - h1 / len, hs)
		};
		return pts;
	}

	public int getHandleGrabbedClose(int xtest, int ytest, int deltaSq, int minSize) {
		lastHandleGrabbed = -1;
		if (CustomGraphics.distanceSq(x, y, x2, y2) >= minSize) {
			if (CustomGraphics.distanceSq(x, y, xtest, ytest) <= deltaSq)
				lastHandleGrabbed = 0;
			else if (CustomGraphics.distanceSq(x2, y2, xtest, ytest) <= deltaSq)
				lastHandleGrabbed = 1;
		}
		return lastHandleGrabbed;
	}

	// update dot positions (curcount) for drawing current (general case for
	// multiple currents)
	public double updateDotCount(double cur, double cc) {
		if (!sim.simIsRunning())
			return cc;
		var cadd = cur * currentMult;
		/*
		 * if (cur != 0 && cadd <= .05 && cadd >= -.05)
		 * cadd = (cadd < 0) ? -.05 : .05;
		 */
		cadd %= 8;
		/*
		 * if (cadd > 8)
		 * cadd = 8;
		 * if (cadd < -8)
		 * cadd = -8;
		 */
		return cc + cadd;
	}

	// update dot positions (curcount) for drawing current (simple case for single
	// current)
	public void updateDotCount() {
		curcount = updateDotCount(current, curcount);
	}

	// update and draw current for simple two-terminal element
	public void doDots(CustomGraphics g) {
		updateDotCount();
		if (sim.dragElm != this)
			drawDots(g, point1, point2, curcount);
	}
	#endregion

	#region draw methods
	public virtual void draw(CustomGraphics g) {
	}

	public bool needsHighlight() {
		return mouseElmRef == this
			|| selected
			|| sim.plotYElm == this
			// Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm
			|| (mouseElmRef is ScopeElm && ((ScopeElm)mouseElmRef).elmScope.getElm() == this);
	}

	public Color getVoltageColor(CustomGraphics g, double volts) {
		if (needsHighlight()) {
			return selectColor;
		}
		if (!sim.voltsCheckItem.Checked) {
			return whiteColor;
		}
		var c = (int) ((volts + voltageRange)
			* (colorScaleCount - 1)
			/ (voltageRange * 2));
		if (c < 0)
			c = 0;
		if (c >= colorScaleCount)
			c = colorScaleCount - 1;
		return colorScale[c];
	}

	public void setVoltageColor(CustomGraphics g, double volts) {
		var color = getVoltageColor(g, volts);
		g.setStrokeStyle(color);
		g.setFillStyle(color);
	}

	public void setPowerColor(CustomGraphics g, double w0) {
		if (!sim.powerCheckItem.Checked)
			return;
		if (needsHighlight()) {
			g.setStrokeStyle(selectColor);
			g.setFillStyle(selectColor);
			return;
		}
		w0 *= powerMult;
		// System.out.println(w);
		int i = (int) ((colorScaleCount / 2) + (colorScaleCount / 2) * -w0);
		if (i < 0)
			i = 0;
		if (i >= colorScaleCount)
			i = colorScaleCount - 1;
		g.setStrokeStyle(colorScale[i]);
		g.setFillStyle(colorScale[i]);
	}

	// yellow argument is unused, can't remember why it was there
	public void setPowerColor(CustomGraphics g, bool yellow) {
		/*
		 * if (conductanceCheckItem.getState()) {
		 * setConductanceColor(g, current/getVoltageDiff());
		 * return;
		 * }
		 */
		if (sim.powerCheckItem.Checked)
			setPowerColor(g, getPower());
	}

	public void drawPosts(CustomGraphics g) {
		// we normally do this in updateCircuit() now because the logic is more
		// complicated.
		// we only handle the case where we have to draw all the posts. That happens
		// when
		// this element is selected or is being created
		if (sim.dragElm == null && !needsHighlight())
			return;
		if (sim.mouseMode == CirSim.MODE_DRAG_ROW ||
				sim.mouseMode == CirSim.MODE_DRAG_COLUMN)
			return;
		for (int i = 0; i != getPostCount(); i++) {
			var p = getPost(i);
			drawPost(g, p);
		}
	}

	public void draw2Leads(CustomGraphics g) {
		// draw first lead
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		// draw second lead
		setVoltageColor(g, volts[1]);
		drawThickLine(g, lead2, point2);
	}

	// draw component values (number of resistor ohms, etc). hs = offset
	public void drawValues(CustomGraphics g, string s, double hs) {
		if (s == null)
			return;
		g.setFont(unitsFont);
		var w = (int) g.measureText(s).Width;
		g.setStrokeStyle(whiteColor);
		g.setFillStyle(whiteColor);
		var ya = (int) g.currentFontSize / 2;
		int xc, yc;
		if (this is RailElm || this is SweepElm) {
			xc = x2;
			yc = y2;
		} else {
			xc = (x2 + x) / 2;
			yc = (y2 + y) / 2;
		}
		var dpx = (int) (dpx1 * hs);
		var dpy = (int) (dpy1 * hs);
		if (dpx == 0)
			g.fillText(s, xc - w / 2, yc - Math.Abs(dpy) - 2);
		else {
			var xx = xc + Math.Abs(dpx) + 2;
			if (this is VoltageElm || (x < x2 && y > y2))
				xx = xc - (w + Math.Abs(dpx) + 2);
			g.fillText(s, xx, yc + dpy + ya);
		}
	}

	// draw current dots from point a to b
	public void drawDots(CustomGraphics g, Point pa, Point pb, double pos) {
		if ((!sim.simIsRunning()) || pos == 0 || !sim.dotsCheckItem.Checked)
			return;
		var dx = pb.X - pa.X;
		var dy = pb.Y - pa.Y;
		var dn = Math.Sqrt(dx * dx + dy * dy);
		var color = sim.conventionCheckItem.Checked ? Color.Yellow : Color.Cyan;
		g.setStrokeStyle(color);
		g.setFillStyle(color);
		int ds = 16;
		pos %= ds;
		if (pos < 0)
			pos += ds;
		double di = 0;
		for (di = pos; di < dn; di += ds) {
			var x0 = (int) (pa.X + di * dx / dn);
			var y0 = (int) (pa.Y + di * dy / dn);
			g.fillRect(x0 - 2, y0 - 2, 4, 4);
		}
	}

	/*
	 * void drawPost(Graphics g, int x0, int y0, int n) {
	 * if (sim.dragElm == null && !needsHighlight() &&
	 * sim.getCircuitNode(n).links.size() == 2)
	 * return;
	 * if (sim.mouseMode == CirSim.MODE_DRAG_ROW ||
	 * sim.mouseMode == CirSim.MODE_DRAG_COLUMN)
	 * return;
	 * drawPost(g, x0, y0);
	 * }
	 */
	public static void drawPost(CustomGraphics g, Point pt) {
		g.setStrokeStyle(whiteColor);
		g.setFillStyle(whiteColor);
		g.fillCircle(pt.X, pt.Y, 7, 7);
	}

	public static void drawThickLine(CustomGraphics g, int x, int y, int x2, int y2) {
		g.setLineWidth(3.0f);
		g.drawLine(x, y, x2, y2);
		g.setLineWidth(1.0f);
	}

	public static void drawThickLine(CustomGraphics g, Point pa, Point pb) {
		g.setLineWidth(3.0f);
		g.drawLine(pa.X, pa.Y, pb.X, pb.Y);
		g.setLineWidth(1.0f);
	}

	public void drawCoil(CustomGraphics g, int hs, Point p1, Point p2, double v1, double v2) {
		var len = (float)distance(p1, p2);

		g.save();
		g.setLineWidth(3.0f);
		g.transform(
			(p2.X - p1.X) / len,
			(p2.Y - p1.Y) / len,
			-(p2.Y - p1.Y) / len,
			(p2.X - p1.X) / len,
			p1.X,
			p1.Y
		);
		if (sim.voltsCheckItem.Checked) {
			var grad = new LinearGradientBrush(
				Point.Empty,
				new PointF(len, 0),
				getVoltageColor(g, v1),
				getVoltageColor(g, v2)
			);
			g.setStrokeStyle(grad);
		}
		g.setLineCap(LineCap.Round);
		g.scale(1, hs > 0 ? 1 : -1);

		// draw more loops for a longer coil
		var loopCt = (int) Math.Ceiling(len / 11);
		for (int loop = 0; loop != loopCt; loop++) {
			var start = len * loop / loopCt;
			var p = new List<PointF>();
			p.Add(new PointF(start, 0));
			p.AddRange(CustomGraphics.CreateArc(
				len * (loop + .5f) / loopCt, 0,
				len / (2 * loopCt), len / (2 * loopCt),
				180, 180
			));
			p.Add(new PointF(len * (loop + 1) / loopCt, 0));
			g.drawPolygon([.. p]);
		}

		g.restore();
	}
	#endregion
}
