using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace MainForm;

public abstract class ElmBase : Editable {
	public const int SCALE_AUTO = 0;
	const int SCALE_1 = 1;
	const int SCALE_M = 2;
	const int SCALE_MU = 3;

	#region static variables
	static double voltageRange = 5;
	static int colorScaleCount = 32;
	static Color[] colorScale;
	public static double currentMult, powerMult;

	// scratch points for convenience
	protected static Point ps1, ps2;

	protected static CirSim sim;
	public static Color whiteColor, selectColor, lightGrayColor;
	public static Font unitsFont;

	protected static string showFormat, shortFormat;
	public static Stopwatch sw = new();
	static ElmBase? mouseElmRef = null;

	public static Color positiveColor = Color.Transparent;
	public static Color negativeColor = Color.Transparent;
	public static Color neutralColor = Color.Transparent;
	#endregion

	#region dinamic variables
	// initial point where user created element. For simple two-terminal elements,
	// this is the first node/post.
	public int x, y;

	// point to which user dragged out element. For simple two-terminal elements,
	// this is the second node/post
	public int x2, y2;

	public int flags, voltSource;
	public int[] nodes;

	// length along x and y axes, and sign of difference
	protected int dx, dy, dsign;

	int lastHandleGrabbed = -1;

	// length of element
	protected double dn;

	protected double dpx1, dpy1;

	// (x,y) and (x2,y2) as Point objects
	public Point point1, point2;

	// lead points (ends of wire stubs for simple two-terminal elements)
	public Point lead1, lead2;

	// voltages at each node
	public double[] volts;

	public double current, curcount;
	public Rectangle boundingBox;

	// if subclasses set this to true, element will be horizontal or vertical only
	protected bool noDiagonal;

	public bool selected;
	#endregion

	protected ElmBase(int xx, int yy) {
		x = x2 = xx;
		y = y2 = yy;
		flags = getDefaultFlags();
		allocNodes();
		initBoundingBox();
	}

	protected ElmBase(int xa, int ya, int xb, int yb, int f) {
		x = xa;
		y = ya;
		x2 = xb;
		y2 = yb;
		flags = f;
		allocNodes();
		initBoundingBox();
	}

	private void initBoundingBox() {
		boundingBox = new Rectangle()
		{
			X = Math.Min(x, x2),
			Y = Math.Min(y, y2),
			Width = Math.Abs(x2 - x) + 1,
			Height = Math.Abs(y2 - y) + 1
		};
	}

	#region virtual methods
	public virtual EditInfo? getEditInfo(int n) {
		return null;
	}

	public virtual void setEditValue(int n, EditInfo ei) {
	}

	public virtual int getDefaultFlags() {
		return 0;
	}

	public virtual int getPostCount() {
		return 2;
	}

	// number of internal nodes (nodes not visible in UI that are needed for implementation)
	public virtual int getInternalNodeCount() {
		return 0;
	}

	public virtual Type getDumpClass() {
		return GetType();
	}

	public virtual int getDumpType() {
		throw new NotImplementedException();
	}

	public virtual string dump() {
		var t = getDumpType();
		return $"{(t < 127 ? (char)t : t)} {x} {y} {x2} {y2} {flags}";
	}

	public virtual Point getPost(int n) {
		return (n == 0) ? point1 : (n == 1) ? point2 : CustomGraphics.PInvalid;
	}

	public virtual void reset() {
		for (int i = 0; i != getPostCount() + getInternalNodeCount(); ++i)
			volts[i] = 0;
		curcount = 0;
	}

	public virtual void delete() {
		if (mouseElmRef == this)
			mouseElmRef = null;
		sim.deleteSliders(this);
	}

	public virtual void setParentList(List<ElmBase> elmList) {
	}

	// set current for voltage source vn to c. vn will be the same value as in a
	// previous call to setVoltageSource(n, vn)
	public virtual void setCurrent(int vn, double c) {
		current = c;
	}

	// get current for one- or two-terminal elements
	public virtual double getCurrent() {
		return current;
	}

	// set voltage of x'th node, called by simulator logic
	public virtual void setNodeVoltage(int n, double c) {
		volts[n] = c;
		calculateCurrent();
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

	// called when an element is done being dragged out; returns true if it's zero
	// size and should be deleted
	public virtual bool creationFailed() {
		return x == x2 && y == y2;
	}

	// number of voltage sources this element needs
	public virtual int getVoltageSourceCount() {
		return 0;
	}

	// notify this element that its pth node is n. This value n can be passed to
	// stampMatrix()
	public virtual void setNode(int p, int n) {
		nodes[p] = n;
	}

	// notify this element that its nth voltage source is v. This value v can be
	// passed to stampVoltageSource(), etc and will be passed back in calls to
	// setCurrent()
	public virtual void setVoltageSource(int n, int v) {
		// default implementation only makes sense for subclasses with one voltage
		// source. If we have 0 this isn't used, if we have >1 this won't work
		voltSource = v;
	}

	// int getVoltageSource() { return voltSource; } // Never used except for debug
	// code which is commented out
	public virtual double getVoltageDiff() {
		return volts[0] - volts[1];
	}

	public virtual bool nonLinear() {
		return false;
	}

	// needed for calculating circuit bounds (need to special-case centered text elements)
	public virtual bool isCenteredText() {
		return false;
	}

	// get component info for display in lower right
	public virtual void getInfo(string[] arr) {
	}

	public virtual string getScopeText(int v) {
		var info = new string[10];
		getInfo(info);
		return info[0];
	}

	public virtual double getPower() {
		return getVoltageDiff() * current;
	}

	public virtual double getScopeValue(int x) {
		return (x == Scope.VAL_CURRENT) ? getCurrent() : (x == Scope.VAL_POWER) ? getPower() : getVoltageDiff();
	}

	public virtual int getScopeUnits(int x) {
		return (x == Scope.VAL_CURRENT) ? Scope.UNITS_A : (x == Scope.VAL_POWER) ? Scope.UNITS_W : Scope.UNITS_V;
	}

	// get number of nodes that can be retrieved by getConnectionNode()
	public virtual int getConnectionNodeCount() {
		return getPostCount();
	}

	// get nodes that can be passed to getConnection(), to test if this element
	// connects
	// those two nodes; this is the same as getNode() for all but labeled nodes.
	public virtual int getConnectionNode(int n) {
		return getNode(n);
	}

	// are n1 and n2 connected by this element? this is used to determine
	// unconnected nodes, and look for loops
	public virtual bool getConnection(int n1, int n2) {
		return true;
	}

	// is n1 connected to ground somehow?
	public virtual bool hasGroundConnection(int n1) {
		return false;
	}

	// is this a wire or equivalent to a wire?
	public virtual bool isWire() {
		return false;
	}

	public virtual bool canViewInScope() {
		return getPostCount() <= 2;
	}

	public virtual bool canShowValueInScope(int v) {
		return false;
	}

	public virtual int getShortcut() {
		return 0;
	}

	public virtual void setMouseElm(bool v) {
		if (v)
			mouseElmRef = this;
		else if (mouseElmRef == this)
			mouseElmRef = null;
	}

	public virtual void draggingDone() {
	}

	public virtual string dumpModel() {
		return null;
	}

	public virtual void updateModels() {
	}

	public virtual double getCurrentIntoNode(int n) {
		// if we take out the getPostCount() == 2 it gives the wrong value for rails
		if (n == 0 && getPostCount() == 2)
			return -current;
		else
			return current;
	}

	// stamp matrix values for linear elements.
	// for non-linear elements, use this to stamp values that don't change each
	// iteration, and call stampRightSide() or stampNonLinear() as needed
	public virtual void stamp() {
	}

	public virtual void doStep() {
	}

	public virtual void startIteration() {
	}

	public virtual void stepFinished() {
	}

	// calculate current in response to node voltages changing
	public virtual void calculateCurrent() {
	}

	public virtual void draw(CustomGraphics g) {
	}
	#endregion

	#region dynamic methods
	public bool needsShortcut() {
		return getShortcut() > 0;
	}

	public bool needsHighlight() {
		return mouseElmRef == this || selected || sim.plotYElm == this ||
			// Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm
			(mouseElmRef is ScopeElm && ((ScopeElm)mouseElmRef).elmScope.getElm() == this);
	}

	public bool isMouseElm() {
		return mouseElmRef == this;
	}

	public bool isSelected() {
		return selected;
	}

	public void setSelected(bool x) {
		selected = x;
	}

	public void allocNodes() {
		var n = getPostCount() + getInternalNodeCount();
		// preserve voltages if possible
		if (nodes == null || nodes.Length != n) {
			nodes = new int[n];
			volts = new double[n];
		}
	}

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

	public double getPostVoltage(int x) {
		return volts[x];
	}

	public void calcLeads(int len) {
		if (dn < len || len == 0) {
			lead1 = point1;
			lead2 = point2;
			return;
		}
		lead1 = interpPoint(point1, point2, (dn - len) / (2 * dn));
		lead2 = interpPoint(point1, point2, (dn + len) / (2 * dn));
	}

	public void move(int dx, int dy) {
		x += dx;
		y += dy;
		x2 += dx;
		y2 += dy;
		boundingBox.Offset(dx, dy);
		setPoints();
	}

	public void setPosition(int x_, int y_, int x2_, int y2_) {
		x = x_;
		y = y_;
		x2 = x2_;
		y2 = y2_;
		setPoints();
	}

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

	public void setBbox(int x1, int y1, int x2, int y2) {
		if (x1 > x2) {
			(x2, x1) = (x1, x2);
		}
		if (y1 > y2) {
			(y2, y1) = (y1, y2);
		}
		boundingBox = new Rectangle(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
	}

	public void setBbox(Point p1, Point p2, double w) {
		setBbox(p1.X, p1.Y, p2.X, p2.Y);
		var dpx = (int) (dpx1 * w);
		var dpy = (int) (dpy1 * w);
		adjustBbox(p1.X + dpx, p1.Y + dpy, p1.X - dpx, p1.Y - dpy);
	}

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

	public static Point[] calcArrow(Point a, Point b, double al, double aw) {
		var poly = new Point[3];
		var adx = b.X - a.X;
		var ady = b.Y - a.Y;
		var l = Math.Sqrt(adx * adx + ady * ady);
		poly[0] = b;
		interpPoint2(a, b, ref poly[1], ref poly[2], 1 - al / l, aw);
		return poly;
	}

	public Point[] getSchmittPolygon(float gsize, float ctr) {
		var pts = newPointArray(6);
		var hs = 3 * gsize;
		var h1 = 3 * gsize;
		var h2 = h1 * 2;
		var len = distance(lead1, lead2);
		pts[0] = interpPoint(lead1, lead2, ctr - h2 / len, hs);
		pts[1] = interpPoint(lead1, lead2, ctr + h1 / len, hs);
		pts[2] = interpPoint(lead1, lead2, ctr + h1 / len, -hs);
		pts[3] = interpPoint(lead1, lead2, ctr + h2 / len, -hs);
		pts[4] = interpPoint(lead1, lead2, ctr - h1 / len, -hs);
		pts[5] = interpPoint(lead1, lead2, ctr - h1 / len, hs);
		return createPolygon(pts);
	}

	public void updateDotCount() {
		curcount = updateDotCount(current, curcount);
	}

	public void doDots(CustomGraphics g) {
		updateDotCount();
		if (sim.dragElm != this)
			drawDots(g, point1, point2, curcount);
	}

	public int getBasicInfo(string[] arr) {
		arr[1] = "I = " + getCurrentDText(getCurrent());
		arr[2] = "Vd = " + getVoltageDText(getVoltageDiff());
		return 3;
	}

	public Color getVoltageColor(CustomGraphics g, double volts) {
		if (needsHighlight()) {
			return (selectColor);
		}
		if (!sim.voltsCheckItem.Checked) {
			return (whiteColor);
		}
		int c = (int) ((volts + voltageRange) * (colorScaleCount - 1) /
				(voltageRange * 2));
		if (c < 0)
			c = 0;
		if (c >= colorScaleCount)
			c = colorScaleCount - 1;
		return (colorScale[c]);
	}

	public void setVoltageColor(CustomGraphics g, double volts) {
		g.setColor(getVoltageColor(g, volts));
	}

	public void setPowerColor(CustomGraphics g, bool yellow) {
		if (!sim.powerCheckItem.Checked)
			return;
		setPowerColor(g, getPower());
	}

	public void setPowerColor(CustomGraphics g, double w0) {
		if (!sim.powerCheckItem.Checked)
			return;
		if (needsHighlight()) {
			g.setColor(selectColor);
			return;
		}
		w0 *= powerMult;
		int i = (int) ((colorScaleCount / 2) + (colorScaleCount / 2) * -w0);
		if (i < 0)
			i = 0;
		if (i >= colorScaleCount)
			i = colorScaleCount - 1;
		g.setColor(colorScale[i]);
	}

	public void selectRect(Rectangle r) {
		selected = r.IntersectsWith(boundingBox);
	}

	public Rectangle getBoundingBox() {
		return boundingBox;
	}

	public void flipPosts() {
		var oldx = x;
		var oldy = y;
		x = x2;
		y = y2;
		x2 = oldx;
		y2 = oldy;
		setPoints();
	}

	public void draw2Leads(CustomGraphics g) {
		// draw first lead
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		// draw second lead
		setVoltageColor(g, volts[1]);
		drawThickLine(g, lead2, point2);
	}

	public void drawPosts(CustomGraphics g) {
		// we normally do this in updateCircuit() now because the logic is more
		// complicated.
		// we only handle the case where we have to draw all the posts. That happens
		// when
		// this element is selected or is being created
		if (sim.dragElm == null && !needsHighlight())
			return;
		if (sim.mouseMode == CirSim.MODE_DRAG_ROW || sim.mouseMode == CirSim.MODE_DRAG_COLUMN)
			return;
		for (int i = 0; i != getPostCount(); i++) {
			var p = getPost(i);
			drawPost(g, p);
		}
	}

	public void drawHandles(CustomGraphics g, Color c) {
		g.setColor(c);
		if (lastHandleGrabbed == -1)
			g.fillRect(x - 3, y - 3, 7, 7);
		else if (lastHandleGrabbed == 0)
			g.fillRect(x - 4, y - 4, 9, 9);
		if (getPostCount() > 1 || this is ScopeElm) {
			if (lastHandleGrabbed == -1)
				g.fillRect(x2 - 3, y2 - 3, 7, 7);
			else if (lastHandleGrabbed == 1)
				g.fillRect(x2 - 4, y2 - 4, 9, 9);
		}
	}

	public void drawCenteredText(CustomGraphics g, string s, int x, int y, bool cx) {
		/// TODO:drawCenteredText
		var w = (int) g.measureText(s).Width;
		var h2 = (int) g.currentFontSize / 2;
		g.save();
		//g.setTextBaseline("middle");
		if (cx) {
			//g.setTextAlign("center");
			adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
		} else {
			adjustBbox(x, y - h2, x + w, y + h2);
		}
		//if (cx)
		//	g.setTextAlign("center");
		g.drawString(s, x, y);
		g.restore();
	}

	public void drawValues(CustomGraphics g, string s, double hs) {
		if (s == null)
			return;
		g.setFont(unitsFont);
		// FontMetrics fm = g.getFontMetrics();
		var w = (int) g.measureText(s).Width;
		g.setColor(whiteColor);
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
			g.drawString(s, xc - w / 2, yc - Math.Abs(dpy) - 2);
		else {
			var xx = xc + Math.Abs(dpx) + 2;
			if (this is VoltageElm || (x < x2 && y > y2))
				xx = xc - (w + Math.Abs(dpx) + 2);
			g.drawString(s, xx, yc + dpy + ya);
		}
	}

	public void drawCoil(CustomGraphics g, int hs, Point p1, Point p2, double v1, double v2) {
		var len = distance(p1, p2);

		g.save();
		g.setLineWidth(3.0f);
		g.transform(
			(p2.X - p1.X) / (float)len, (p2.Y - p1.Y) / (float)len,
			-(p2.Y - p1.Y) / (float)len, (p2.X - p1.X) / (float)len,
			p1.X, p1.Y
		);
		setPowerColor(g, true);
		g.setLineCap(LineCap.Round);
		g.scale(1, hs > 0 ? 1 : -1);

		int loop;
		// draw more loops for a longer coil
		var loopCt = (int) Math.Ceiling(len / 11);
		for (loop = 0; loop != loopCt; loop++) {
			g.beginPath();
			var start = (float)(len * loop / loopCt);
			g.moveTo(start, 0);
			g.arc(len * (loop + .5) / loopCt, 0, len / (2 * loopCt), Math.PI, Math.PI * 2);
			g.lineTo((float)len * (loop + 1) / loopCt, 0);
			g.stroke();
		}

		g.restore();
	}
	#endregion

	#region public static methods
	public static void initClass(CirSim s) {
		unitsFont = new Font("SansSerif", 12);
		sim = s;
		sw.Start();

		colorScale = new Color[colorScaleCount];

		ps1 = new Point();
		ps2 = new Point();

		showFormat = "####.###";
		shortFormat = "####.#";
	}

	public static void setColorScale() {
		if (positiveColor == Color.Empty)
			positiveColor = Color.Green;
		if (negativeColor == Color.Empty)
			negativeColor = Color.Red;
		if (neutralColor == Color.Empty)
			neutralColor = Color.Gray;
		for (int i = 0; i != colorScaleCount; i++) {
			var v = i * 2.0 / colorScaleCount - 1;
			double r, g, b;
			if (v < 0) {
				v *= -1;
				r = negativeColor.R * v + neutralColor.R * (1.0 - v);
				g = negativeColor.G * v + neutralColor.G * (1.0 - v);
				b = negativeColor.B * v + neutralColor.B * (1.0 - v);
			} else {
				r = positiveColor.R * v + neutralColor.R * (1.0 - v);
				g = positiveColor.G * v + neutralColor.G * (1.0 - v);
				b = positiveColor.B * v + neutralColor.B * (1.0 - v);
			}
			colorScale[i] = Color.FromArgb((int)r, (int)g, (int)b);
		}
	}

	public static bool comparePair(int x1, int x2, int y1, int y2) {
		return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
	}

	public static Point[] newPointArray(int n) {
		var a = new Point[n];
		while (n > 0)
			a[--n] = new Point();
		return a;
	}

	public static void drawPost(CustomGraphics g, Point pt) {
		g.setColor(whiteColor);
		g.drawArc(pt.X - 3, pt.Y - 3, 7, 7, 0, 360);
	}

	public static void drawThickLine(CustomGraphics g, int x, int y, int x2, int y2) {
		g.setLineWidth(3.0f);
		g.drawLine(x, y, x2, y2);
		g.setLineWidth(1.0f);
	}

	public static void drawThickCircle(CustomGraphics g, int cx, int cy, int ri) {
		g.setLineWidth(3.0f);
		g.drawArc(cx - ri, cy - ri, 2 * ri + 1, 2 * ri + 1, 0, 360);
		g.setLineWidth(1.0f);
	}

	public static void drawDots(CustomGraphics g, Point pa, Point pb, double pos) {
		if ((!sim.simIsRunning()) || pos == 0 || !sim.dotsCheckItem.Checked)
			return;
		var dx = pb.X - pa.X;
		var dy = pb.Y - pa.Y;
		var dn = Math.Sqrt(dx * dx + dy * dy);
		g.setColor(sim.conventionCheckItem.Checked ? Color.Yellow : Color.Cyan);
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

	public static double updateDotCount(double cur, double count) {
		if (sim.simIsRunning()) {
			var cDelta = cur * currentMult;
			cDelta %= 8;
			return count + cDelta;
		} else {
			return count;
		}
	}

	public static Point[] createPolygon(Point a, Point b, Point c) {
		var poly = new Point[] {
			a, b, c
		};
		return poly;
	}

	public static Point[] createPolygon(Point a, Point b, Point c, Point d) {
		var p = new Point[] {
			a, b, c, d
		};
		return p;
	}

	public static Point[] createPolygon(params Point[] a) {
		var p = new Point[a.Length];
		for (int i = 0; i != a.Length; i++)
			p[i] = a[i];
		return p;
	}

	public static string getVoltageText(double v) {
		return getUnitText(v, "V");
	}

	public static string getCurrentText(double i) {
		return getUnitText(i, "A");
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

	public static string getUnitText(double v, string unit) {
		return getUnitText(v, unit, false);
	}

	public static string getShortUnitText(double v, string unit) {
		return getUnitText(v, unit, true);
	}

	public static void interpPoint(Point a, Point b, ref Point returnValue, double f) {
		returnValue.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + .48);
		returnValue.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + .48);
	}

	public static void interpPoint(Point a, Point b, ref Point returnValue, double f, double g) {
		var gx = b.Y - a.Y;
		var gy = a.X - b.X;
		g /= Math.Sqrt(gx * gx + gy * gy);
		returnValue.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + .48);
		returnValue.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + .48);
	}

	public static void interpPoint2(Point a, Point b, ref Point returnValueA, ref Point returnValueB, double f, double g) {
		var gx = b.Y - a.Y;
		var gy = a.X - b.X;
		g /= Math.Sqrt(gx * gx + gy * gy);
		returnValueA.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + .48);
		returnValueA.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + .48);
		returnValueB.X = (int)Math.Floor(a.X * (1 - f) + b.X * f - g * gx + .48);
		returnValueB.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f - g * gy + .48);
	}

	public static Point interpPoint(Point a, Point b, double f) {
		var p = new Point();
		interpPoint(a, b, ref p, f);
		return p;
	}

	public static Point interpPoint(Point a, Point b, double f, double g) {
		var p = new Point();
		interpPoint(a, b, ref p, f, g);
		return p;
	}
	#endregion

	#region protected static methods
	protected static void drawThickLine(CustomGraphics g, Point pa, Point pb) {
		g.setLineWidth(3.0f);
		g.drawLine(pa.X, pa.Y, pb.X, pb.Y);
		g.setLineWidth(1.0f);
	}

	protected static void drawThickPolygon(CustomGraphics g, int[] xs, int[] ys, int c) {
		g.setLineWidth(3.0f);
		g.drawPolyline(xs, ys, c);
		g.setLineWidth(1.0f);
	}

	protected static void drawThickPolygon(CustomGraphics g, Point[] p) {
		g.setLineWidth(3.0f);
		g.drawPolyline(p);
		g.setLineWidth(1.0f);
	}

	protected static void drawPolygon(CustomGraphics g, Point[] p) {
		g.drawPolyline(p);
	}

	protected static string getVoltageDText(double v) {
		return getUnitText(Math.Abs(v), "V");
	}

	protected static string getCurrentDText(double i) {
		return getUnitText(Math.Abs(i), "A");
	}

	protected static string getUnitTextWithScale(double val, string utext, int scale) {
		if (scale == SCALE_1)
			return (val).ToString(showFormat) + " " + utext;
		if (scale == SCALE_M)
			return (1e3 * val).ToString(showFormat) + " m" + utext;
		if (scale == SCALE_MU)
			return (1e6 * val).ToString(showFormat) + " u" + utext;
		return getUnitText(val, utext);
	}

	protected static double distance(Point p1, Point p2) {
		var x = p1.X - p2.X;
		var y = p1.Y - p2.Y;
		return Math.Sqrt(x * x + y * y);
	}

	protected static string format(double v, bool sf) {
		return v.ToString(sf ? shortFormat : showFormat);
	}

	protected static string getUnitText(double v, string u, bool sf) {
		var sp = sf ? "" : " ";
		var sg = (v < 0) ? "" : " ";
		var va = Math.Abs(v);
		if (va < 1e-14)
			return $" 0{sp}{u}";
		if (va < 1e-9)
			return $"{sg}{format(v * 1e12, sf)}{sp}p{u}";
		if (va < 1e-6)
			return $"{sg}{format(v * 1e9, sf)}{sp}n{u}";
		if (va < 1e-3)
			return $"{sg}{format(v * 1e6, sf)}{sp}u{u}";
		if (va < 1)
			return $"{sg}{format(v * 1e3, sf)}{sp}m{u}";
		if (va < 1e3)
			return $"{sg}{format(v, sf)}{sp}{u}";
		if (va < 1e6)
			return $"{sg}{format(v * 1e-3, sf)}{sp}k{u}";
		if (va < 1e9)
			return $"{sg}{format(v * 1e-6, sf)}{sp}M{u}";
		return $"{sg}{format(v * 1e-9, sf)}{sp}G{u}";
	}
	#endregion
}
