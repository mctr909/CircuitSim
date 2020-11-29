﻿using System;
using System.Drawing;

namespace Circuit.Elements {
    partial class CircuitElm : Editable {
        #region CONST
        protected const int SCALE_AUTO = 0;
        protected const int SCALE_1 = 1;
        protected const int SCALE_M = 2;
        protected const int SCALE_MU = 3;

        protected const double pi = Math.PI;
        #endregion

        #region static variable
        public static CirSim sim;
        protected static Circuit cir;

        static CircuitElm mMouseElmRef = null;

        /* scratch points for convenience */
        protected static Point ps1;
        protected static Point ps2;

        public static double voltageRange = 5;

        public static double currentMult;
        #endregion

        #region dynamic variable
        /* initial point where user created element.
         * For simple two-terminal elements, this is the first node/post. */
        public int x1;
        public int y1;

        /* point to which user dragged out element.
         * For simple two-terminal elements, this is the second node/post */
        public int x2;
        public int y2;

        protected int flags;
        protected int voltSource;
        public int[] nodes { get; protected set; }

        /* length along x and y axes, and sign of difference */
        protected int dx;
        protected int dy;
        protected int dsign;

        int lastHandleGrabbed = -1;
        protected int numHandles = 2;

        /* length of element */
        protected double dn;

        double dpx1;
        double dpy1;

        /* (x,y) and (x2,y2) as Point objects */
        protected Point point1;
        protected Point point2;

        /* lead points (ends of wire stubs for simple two-terminal elements) */
        protected Point lead1;
        protected Point lead2;

        /* voltages at each node */
        public double[] volts { get; protected set; }

        protected double current;
        protected double curcount;
        public Rectangle boundingBox;

        /* if subclasses set this to true, element will be horizontal or vertical only */
        protected bool noDiagonal;

        public bool selected;
        #endregion

        public static void initClass(CirSim s, Circuit c) {
            sim = s;
            cir = c;
            ps1 = new Point();
            ps2 = new Point();
            colorScale = new Color[colorScaleCount];
        }

        /// <summary>
        /// create new element with one post at xx,yy, to be dragged out by user
        /// </summary>
        protected CircuitElm(int xx, int yy) {
            x1 = x2 = xx;
            y1 = y2 = yy;
            flags = getDefaultFlags();
            allocNodes();
            initBoundingBox();
        }

        /// <summary>
        /// create element between xa,ya and xb,yb from undump
        /// </summary>
        protected CircuitElm(int xa, int ya, int xb, int yb, int f) {
            x1 = xa;
            y1 = ya;
            x2 = xb;
            y2 = yb;
            flags = f;
            allocNodes();
            initBoundingBox();
        }

        public virtual DUMP_ID getDumpType() {
            throw new Exception("CircuitElm.getDumpType()");
            /* Seems necessary to work-around what appears to be a compiler
            /* bug affecting OTAElm to make sure this method (which should really be abstract) throws
            /* an exception */
        }

        /* leftover from java, doesn't do anything anymore.  */
        public object getDumpClass() { return this; }

        public virtual int getDefaultFlags() { return 0; }

        void initBoundingBox() {
            boundingBox = new Rectangle(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1) + 1, Math.Abs(y2 - y1) + 1);
        }

        /// <summary>
        /// allocate nodes/volts arrays we need
        /// </summary>
        protected void allocNodes() {
            int n = getPostCount() + getInternalNodeCount();
            /* preserve voltages if possible */
            if (nodes == null || nodes.Length != n) {
                nodes = new int[n];
                volts = new double[n];
            }
        }

        /// <summary>
        /// dump component state for export/undo
        /// </summary>
        public virtual string dump() {
            var type = getDumpType();
            return string.Format("{0} {1} {2} {3} {4} {5}", type, x1, y1, x2, y2, flags);
        }

        /// <summary>
        /// handle reset button
        /// </summary>
        public virtual void reset() {
            for (int i = 0; i != getPostCount() + getInternalNodeCount(); i++) {
                volts[i] = 0;
            }
            curcount = 0;
        }

        public virtual void draw(Graphics g) { }

        /// <summary>
        /// set current for voltage source vn to c.
        /// vn will be the same value as in a previous call to setVoltageSource(n, vn)
        /// </summary>
        /// <param name="vn"></param>
        /// <param name="c"></param>
        public virtual void setCurrent(int vn, double c) { current = c; }

        /// <summary>
        /// get current for one- or two-terminal elements
        /// </summary>
        /// <returns></returns>
        public virtual double getCurrent() { return current; }

        /// <summary>
        /// stamp matrix values for linear elements.
        /// for non-linear elements, use this to stamp values that don't change each iteration,
        /// and call stampRightSide() or stampNonLinear() as needed
        /// </summary>
        public virtual void stamp() { }

        /// <summary>
        /// stamp matrix values for non-linear elements
        /// </summary>
        public virtual void doStep() { }

        public virtual void delete() {
            if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
            if (null != sim) {
                sim.deleteSliders(this);
            }
        }

        public virtual void startIteration() { }

        /// <summary>
        /// get voltage of x'th node
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double getPostVoltage(int x) { return volts[x]; }

        public string dispPostVoltage(int x) {
            if (x < volts.Length) {
                return getUnitText(volts[x], "V");
            } else {
                return "";
            }
        }

        /// <summary>
        /// set voltage of x'th node, called by simulator logic
        /// </summary>
        /// <param name="n"></param>
        /// <param name="c"></param>
        public virtual void setNodeVoltage(int n, double c) {
            volts[n] = c;
            calculateCurrent();
        }

        /// <summary>
        /// calculate current in response to node voltages changing
        /// </summary>
        public virtual void calculateCurrent() { }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void setPoints() {
            dx = x2 - x1;
            dy = y2 - y1;
            dn = Math.Sqrt(dx * dx + dy * dy);
            dpx1 = dy / dn;
            dpy1 = -dx / dn;
            dsign = (dy == 0) ? Math.Sign(dx) : Math.Sign(dy);
            point1 = new Point(x1, y1);
            point2 = new Point(x2, y2);
        }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int len) {
            if (dn < len || len == 0) {
                lead1 = point1;
                lead2 = point2;
                return;
            }
            lead1 = interpPoint(point1, point2, (dn - len) / (2 * dn));
            lead2 = interpPoint(point1, point2, (dn + len) / (2 * dn));
        }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        public virtual void drag(int xx, int yy) {
            xx = sim.snapGrid(xx);
            yy = sim.snapGrid(yy);
            if (noDiagonal) {
                if (Math.Abs(x1 - xx) < Math.Abs(y1 - yy)) {
                    xx = x1;
                } else {
                    yy = y1;
                }
            }
            x2 = xx; y2 = yy;
            setPoints();
        }

        public void move(int dx, int dy) {
            x1 += dx;
            y1 += dy;
            x2 += dx;
            y2 += dy;
            boundingBox.X += dx;
            boundingBox.Y += dy;
            setPoints();
        }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public bool creationFailed() {
            return x1 == x2 && y1 == y2;
        }

        /// <summary>
        /// this is used to set the position of an internal element so we can draw it inside the parent
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        void setPosition(int ax, int ay, int bx, int by) {
            x1 = ax;
            y1 = ay;
            x2 = bx;
            y2 = by;
            setPoints();
        }

        /// <summary>
        /// determine if moving this element by (dx,dy) will put it on top of another element
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool allowMove(int dx, int dy) {
            int nx = x1 + dx;
            int ny = y1 + dy;
            int nx2 = x2 + dx;
            int ny2 = y2 + dy;
            int i;
            for (i = 0; i != sim.elmList.Count; i++) {
                CircuitElm ce = sim.getElm(i);
                if (ce.x1 == nx && ce.y1 == ny && ce.x2 == nx2 && ce.y2 == ny2) {
                    return false;
                }
                if (ce.x1 == nx2 && ce.y1 == ny2 && ce.x2 == nx && ce.y2 == ny) {
                    return false;
                }
            }
            return true;
        }

        public void movePoint(int n, int dx, int dy) {
            /* modified by IES to prevent the user dragging points to create zero sized nodes
            /* that then render improperly */
            int oldx = x1;
            int oldy = y1;
            int oldx2 = x2;
            int oldy2 = y2;
            if (n == 0) {
                x1 += dx; y1 += dy;
            } else {
                x2 += dx; y2 += dy;
            }
            if (x1 == x2 && y1 == y2) {
                x1 = oldx;
                y1 = oldy;
                x2 = oldx2;
                y2 = oldy2;
            }
            setPoints();
        }

        public int getHandleGrabbedClose(int xtest, int ytest, int deltaSq, int minSize) {
            lastHandleGrabbed = -1;
            var x12 = x2 - x1;
            var y12 = y2 - y1;
            if (Math.Sqrt(x12 * x12 + y12 * y12) >= minSize) {
                var x1t = xtest - x1;
                var y1t = ytest - y1;
                var x2t = xtest - x2;
                var y2t = ytest - y2;
                if (Math.Sqrt(x1t * x1t + y1t * y1t) <= deltaSq) {
                    lastHandleGrabbed = 0;
                } else if (Math.Sqrt(x2t * x2t + y2t * y2t) <= deltaSq) {
                    lastHandleGrabbed = 1;
                }
            }
            return lastHandleGrabbed;
        }

        /// <summary>
        /// number of voltage sources this element needs
        /// </summary>
        /// <returns></returns>
        public virtual int getVoltageSourceCount() { return 0; }

        /// <summary>
        /// number of internal nodes (nodes not visible in UI that are needed for implementation)
        /// </summary>
        /// <returns></returns>
        public virtual int getInternalNodeCount() { return 0; }

        /// <summary>
        /// notify this element that its pth node is n.
        /// This value n can be passed to stampMatrix()
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public virtual void setNode(int p, int n) {
            if (p < nodes.Length) {
                nodes[p] = n;
            }
        }

        /// <summary>
        /// notify this element that its nth voltage source is v.
        /// This value v can be passed to stampVoltageSource(),
        /// etc and will be passed back in calls to setCurrent()
        /// </summary>
        /// <param name="n"></param>
        /// <param name="v"></param>
        public virtual void setVoltageSource(int n, int v) {
            /* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
            voltSource = v;
        }

        public virtual double getVoltageDiff() {
            return volts[0] - volts[1];
        }

        public virtual bool nonLinear() { return false; }

        public virtual int getPostCount() { return 2; }

        /// <summary>
        /// get (global) node number of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public int getNode(int n) { return nodes[n]; }

        /// <summary>
        /// get position of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual Point getPost(int n) {
            return (n == 0) ? point1 : (n == 1) ? point2 : new Point();
        }

        public int getNodeAtPoint(int xp, int yp) {
            if (getPostCount() == 2) {
                return (x1 == xp && y1 == yp) ? 0 : 1;
            }
            for (int i = 0; i != getPostCount(); i++) {
                var p = getPost(i);
                if (p.X == xp && p.Y == yp) {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// set/adjust bounding box used for selecting elements.
        /// getCircuitBounds() does not use this!
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        protected void setBbox(int x1, int y1, int x2, int y2) {
            if (x1 > x2) { int q = x1; x1 = x2; x2 = q; }
            if (y1 > y2) { int q = y1; y1 = y2; y2 = q; }
            boundingBox.X = x1;
            boundingBox.Y = y1;
            boundingBox.Width = x2 - x1 + 1;
            boundingBox.Height = y2 - y1 + 1;
        }

        /// <summary>
        /// set bounding box for an element from p1 to p2 with width w
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="w"></param>
        protected void setBbox(Point p1, Point p2, double w) {
            setBbox(p1.X, p1.Y, p2.X, p2.Y);
            int dpx = (int)(dpx1 * w);
            int dpy = (int)(dpy1 * w);
            adjustBbox(p1.X + dpx, p1.Y + dpy, p1.X - dpx, p1.Y - dpy);
        }

        /// <summary>
        /// enlarge bbox to contain an additional rectangle
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        protected void adjustBbox(int x1, int y1, int x2, int y2) {
            if (x1 > x2) { int q = x1; x1 = x2; x2 = q; }
            if (y1 > y2) { int q = y1; y1 = y2; y2 = q; }
            x1 = Math.Min(boundingBox.X, x1);
            y1 = Math.Min(boundingBox.Y, y1);
            x2 = Math.Max(boundingBox.X + boundingBox.Width, x2);
            y2 = Math.Max(boundingBox.Y + boundingBox.Height, y2);
            boundingBox.X = x1;
            boundingBox.Y = y1;
            boundingBox.Width = x2 - x1;
            boundingBox.Height = y2 - y1;
        }

        protected void adjustBbox(Point p1, Point p2) {
            adjustBbox(p1.X, p1.Y, p2.X, p2.Y);
        }

        /// <summary>
        /// needed for calculating circuit bounds (need to special-case centered text elements)
        /// </summary>
        /// <returns></returns>
        public virtual bool isCenteredText() { return false; }

        /// <summary>
        /// update dot positions (curcount) for drawing current (simple case for single current)
        /// </summary>
        protected void updateDotCount() {
            curcount = updateDotCount(current, curcount);
        }

        /// <summary>
        ///  update dot positions (curcount) for drawing current (general case for multiple currents)
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="cc"></param>
        /// <returns></returns>
        protected double updateDotCount(double cur, double cc) {
            if (!sim.simIsRunning()) {
                return cc;
            }
            double cadd = cur * currentMult;
            cadd %= 8;
            return cc + cadd;
        }

        /// <summary>
        /// update and draw current for simple two-terminal element
        /// </summary>
        /// <param name="g"></param>
        protected void doDots(Graphics g) {
            updateDotCount();
            if (sim.dragElm != this) {
                drawDots(g, point1, point2, curcount);
            }
        }

        public virtual void doAdjust() { }

        public virtual void setupAdjust() { }

        /// <summary>
        /// get component info for display in lower right
        /// </summary>
        /// <param name="arr"></param>
        public virtual void getInfo(string[] arr) { }

        protected int getBasicInfo(string[] arr) {
            arr[1] = "I = " + getCurrentDText(getCurrent());
            arr[2] = "Vd = " + getVoltageDText(getVoltageDiff());
            return 3;
        }

        public virtual string getScopeText(int v) {
            var info = new string[10];
            getInfo(info);
            return info[0];
        }

        protected Color getVoltageColor(double volts) {
            if (needsHighlight()) {
                return selectColor;
            }
            if (!sim.chkVoltsCheckItem.Checked) {
                return whiteColor;
            }
            int c = (int)((volts + voltageRange) * (colorScaleCount - 1) / (voltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= colorScaleCount) {
                c = colorScaleCount - 1;
            }
            return colorScale[c];
        }

        public virtual double getPower() { return getVoltageDiff() * current; }

        public virtual double getScopeValue(int x) {
            return (x == Scope.VAL_CURRENT) ? getCurrent() :
                (x == Scope.VAL_POWER) ? getPower() : getVoltageDiff();
        }

        public virtual int getScopeUnits(int x) {
            return (x == Scope.VAL_CURRENT) ? Scope.UNITS_A :
                (x == Scope.VAL_POWER) ? Scope.UNITS_W : Scope.UNITS_V;
        }

        public virtual EditInfo getEditInfo(int n) { return null; }

        public virtual void setEditValue(int n, EditInfo ei) { }

        /// <summary>
        /// get number of nodes that can be retrieved by getConnectionNode()
        /// </summary>
        /// <returns></returns>
        public virtual int getConnectionNodeCount() { return getPostCount(); }

        /// <summary>
        /// get nodes that can be passed to getConnection(), to test if this element connects
        /// those two nodes; this is the same as getNode() for all but labeled nodes.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual int getConnectionNode(int n) { return getNode(n); }

        /// <summary>
        /// are n1 and n2 connected by this element?  this is used to determine
        /// unconnected nodes, and look for loops
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public virtual bool getConnection(int n1, int n2) { return true; }

        /// <summary>
        /// is n1 connected to ground somehow?
        /// </summary>
        /// <param name="n1"></param>
        /// <returns></returns>
        public virtual bool hasGroundConnection(int n1) { return false; }

        /// <summary>
        /// is this a wire or equivalent to a wire?
        /// </summary>
        /// <returns></returns>
        public virtual bool isWire() { return false; }

        public virtual bool canViewInScope() { return getPostCount() <= 2; }

        protected bool comparePair(int x1, int x2, int y1, int y2) {
            return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
        }

        protected bool needsHighlight() {
            if (null == mMouseElmRef || null == sim.plotYElm) {
                return false;
            }
            /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
            var isScopeElm = (mMouseElmRef is ScopeElm) && ((ScopeElm)mMouseElmRef).elmScope.getElm().Equals(this);
            return mMouseElmRef.Equals(this) || selected || sim.plotYElm.Equals(this) || isScopeElm;
        }

        public bool isSelected() { return selected; }

        public virtual bool canShowValueInScope(int v) { return false; }

        public void setSelected(bool x) { selected = x; }

        public void selectRect(Rectangle r) {
            selected = r.IntersectsWith(boundingBox);
        }

        public Rectangle getBoundingBox() { return boundingBox; }

        public bool needsShortcut() { return getShortcut() > 0 && (int)getShortcut() <= 127; }

        public virtual DUMP_ID getShortcut() { return DUMP_ID.INVALID; }

        public virtual bool isGraphicElmt() { return false; }

        public virtual void setMouseElm(bool v) {
            if (v) {
                mMouseElmRef = this;
            } else if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
        }

        public virtual void draggingDone() { }

        public virtual string dumpModel() { return null; }

        public bool isMouseElm() {
            if (null == mMouseElmRef) {
                return false;
            }
            return mMouseElmRef.Equals(this);
        }

        public virtual void updateModels() { }

        public virtual void stepFinished() { }

        public virtual double getCurrentIntoNode(int n) {
            /* if we take out the getPostCount() == 2 it gives the wrong value for rails */
            if (n == 0 && getPostCount() == 2) {
                return -current;
            } else {
                return current;
            }
        }

        public void flipPosts() {
            int oldx = x1;
            int oldy = y1;
            x1 = x2;
            y1 = y2;
            x2 = oldx;
            y2 = oldy;
            setPoints();
        }
    }
}
