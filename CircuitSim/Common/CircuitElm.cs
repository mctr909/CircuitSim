using System;
using System.Drawing;

namespace Circuit.Elements {
    partial class CircuitElm : Editable {
        #region CONST
        protected const int SCALE_AUTO = 0;
        protected const int SCALE_1 = 1;
        protected const int SCALE_M = 2;
        protected const int SCALE_MU = 3;

        protected const double PI = Math.PI;
        protected const double PI2 = Math.PI * 2;
        protected const double TO_DEG = 180 / Math.PI;
        protected const double TO_RAD = Math.PI / 180;
        #endregion

        #region static variable
        public static CirSim Sim { get; private set; }
        protected static Circuit Cir { get; private set; }
        public static double CurrentMult { get; set; }

        /* scratch points for convenience */
        protected static Point ps1;
        protected static Point ps2;

        static CircuitElm mMouseElmRef = null;
        #endregion

        #region property
        /* initial point where user created element.
         * For simple two-terminal elements, this is the first node/post. */
        public int X1 { get; set; }
        public int Y1 { get; set; }

        /* point to which user dragged out element.
         * For simple two-terminal elements, this is the second node/post */
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public bool IsSelected { get; set; }

        public int[] Nodes { get; protected set; }

        /* voltages at each node */
        public double[] Volts { get; protected set; }
        #endregion

        #region dynamic variable
        public Rectangle BoundingBox;

        protected int mFlags;
        protected int mVoltSource;

        /* length along x and y axes, and sign of difference */
        protected int mDx;
        protected int mDy;
        protected int mDsign;

        int mLastHandleGrabbed = -1;
        protected int mNumHandles = 2;

        /* length of element */
        protected double mElmLen;

        protected double mUnitPx1;
        protected double mUnitPy1;

        /* (x,y) and (x2,y2) as Point objects */
        protected Point mPoint1;
        protected Point mPoint2;

        /* lead points (ends of wire stubs for simple two-terminal elements) */
        protected Point mLead1;
        protected Point mLead2;

        protected double mCurrent;
        protected double mCurCount;

        /* if subclasses set this to true, element will be horizontal or vertical only */
        protected bool mNoDiagonal;
        #endregion

        public static void initClass(CirSim s, Circuit c) {
            Sim = s;
            Cir = c;
            ps1 = new Point();
            ps2 = new Point();
            mMouseElmRef = null;
            CurrentMult = 0;
        }

        /// <summary>
        /// create new element with one post at xx,yy, to be dragged out by user
        /// </summary>
        protected CircuitElm(int xx, int yy) {
            X1 = X2 = xx;
            Y1 = Y2 = yy;
            mFlags = getDefaultFlags();
            allocNodes();
            initBoundingBox();
        }

        /// <summary>
        /// create element between xa,ya and xb,yb from undump
        /// </summary>
        protected CircuitElm(int xa, int ya, int xb, int yb, int f) {
            X1 = xa;
            Y1 = ya;
            X2 = xb;
            Y2 = yb;
            mFlags = f;
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
            BoundingBox = new Rectangle(Math.Min(X1, X2), Math.Min(Y1, Y2), Math.Abs(X2 - X1) + 1, Math.Abs(Y2 - Y1) + 1);
        }

        /// <summary>
        /// allocate nodes/volts arrays we need
        /// </summary>
        protected void allocNodes() {
            int n = getPostCount() + getInternalNodeCount();
            /* preserve voltages if possible */
            if (Nodes == null || Nodes.Length != n) {
                Nodes = new int[n];
                Volts = new double[n];
            }
        }

        /// <summary>
        /// dump component state for export/undo
        /// </summary>
        public virtual string dump() {
            var type = getDumpType();
            return string.Format("{0} {1} {2} {3} {4} {5}", type, X1, Y1, X2, Y2, mFlags);
        }

        /// <summary>
        /// handle reset button
        /// </summary>
        public virtual void reset() {
            for (int i = 0; i != getPostCount() + getInternalNodeCount(); i++) {
                Volts[i] = 0;
            }
            mCurCount = 0;
        }

        public virtual void draw(Graphics g) { }

        /// <summary>
        /// set current for voltage source vn to c.
        /// vn will be the same value as in a previous call to setVoltageSource(n, vn)
        /// </summary>
        /// <param name="vn"></param>
        /// <param name="c"></param>
        public virtual void setCurrent(int vn, double c) { mCurrent = c; }

        /// <summary>
        /// get current for one- or two-terminal elements
        /// </summary>
        /// <returns></returns>
        public virtual double getCurrent() { return mCurrent; }

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
            if (null != Sim) {
                Sim.deleteSliders(this);
            }
        }

        public virtual void startIteration() { }

        /// <summary>
        /// get voltage of x'th node
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double getPostVoltage(int x) { return Volts[x]; }

        public string dispPostVoltage(int x) {
            if (x < Volts.Length) {
                return getUnitText(Volts[x], "V");
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
            Volts[n] = c;
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
            mDx = X2 - X1;
            mDy = Y2 - Y1;
            mElmLen = Math.Sqrt(mDx * mDx + mDy * mDy);
            mUnitPx1 = mDy / mElmLen;
            mUnitPy1 = -mDx / mElmLen;
            mDsign = (mDy == 0) ? Math.Sign(mDx) : Math.Sign(mDy);
            mPoint1 = new Point(X1, Y1);
            mPoint2 = new Point(X2, Y2);
        }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int len) {
            if (mElmLen < len || len == 0) {
                mLead1 = mPoint1;
                mLead2 = mPoint2;
                return;
            }
            mLead1 = interpPoint(mPoint1, mPoint2, (mElmLen - len) / (2 * mElmLen));
            mLead2 = interpPoint(mPoint1, mPoint2, (mElmLen + len) / (2 * mElmLen));
        }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        public virtual void drag(int xx, int yy) {
            xx = Sim.snapGrid(xx);
            yy = Sim.snapGrid(yy);
            if (mNoDiagonal) {
                if (Math.Abs(X1 - xx) < Math.Abs(Y1 - yy)) {
                    xx = X1;
                } else {
                    yy = Y1;
                }
            }
            X2 = xx; Y2 = yy;
            setPoints();
        }

        public void move(int dx, int dy) {
            X1 += dx;
            Y1 += dy;
            X2 += dx;
            Y2 += dy;
            BoundingBox.X += dx;
            BoundingBox.Y += dy;
            setPoints();
        }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public bool creationFailed() {
            return X1 == X2 && Y1 == Y2;
        }

        /// <summary>
        /// this is used to set the position of an internal element so we can draw it inside the parent
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        void setPosition(int ax, int ay, int bx, int by) {
            X1 = ax;
            Y1 = ay;
            X2 = bx;
            Y2 = by;
            setPoints();
        }

        /// <summary>
        /// determine if moving this element by (dx,dy) will put it on top of another element
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool allowMove(int dx, int dy) {
            int nx = X1 + dx;
            int ny = Y1 + dy;
            int nx2 = X2 + dx;
            int ny2 = Y2 + dy;
            int i;
            for (i = 0; i != Sim.elmList.Count; i++) {
                CircuitElm ce = Sim.getElm(i);
                if (ce.X1 == nx && ce.Y1 == ny && ce.X2 == nx2 && ce.Y2 == ny2) {
                    return false;
                }
                if (ce.X1 == nx2 && ce.Y1 == ny2 && ce.X2 == nx && ce.Y2 == ny) {
                    return false;
                }
            }
            return true;
        }

        public void movePoint(int n, int dx, int dy) {
            /* modified by IES to prevent the user dragging points to create zero sized nodes
            /* that then render improperly */
            int oldx = X1;
            int oldy = Y1;
            int oldx2 = X2;
            int oldy2 = Y2;
            if (n == 0) {
                X1 += dx; Y1 += dy;
            } else {
                X2 += dx; Y2 += dy;
            }
            if (X1 == X2 && Y1 == Y2) {
                X1 = oldx;
                Y1 = oldy;
                X2 = oldx2;
                Y2 = oldy2;
            }
            setPoints();
        }

        public int getHandleGrabbedClose(int xtest, int ytest, int deltaSq, int minSize) {
            mLastHandleGrabbed = -1;
            var x12 = X2 - X1;
            var y12 = Y2 - Y1;
            if (Math.Sqrt(x12 * x12 + y12 * y12) >= minSize) {
                var x1t = xtest - X1;
                var y1t = ytest - Y1;
                var x2t = xtest - X2;
                var y2t = ytest - Y2;
                if (Math.Sqrt(x1t * x1t + y1t * y1t) <= deltaSq) {
                    mLastHandleGrabbed = 0;
                } else if (Math.Sqrt(x2t * x2t + y2t * y2t) <= deltaSq) {
                    mLastHandleGrabbed = 1;
                }
            }
            return mLastHandleGrabbed;
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
            if (p < Nodes.Length) {
                Nodes[p] = n;
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
            mVoltSource = v;
        }

        public virtual double getVoltageDiff() {
            return Volts[0] - Volts[1];
        }

        public virtual bool nonLinear() { return false; }

        public virtual int getPostCount() { return 2; }

        /// <summary>
        /// get (global) node number of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public int getNode(int n) { return Nodes[n]; }

        /// <summary>
        /// get position of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual Point getPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : new Point();
        }

        public int getNodeAtPoint(int xp, int yp) {
            if (getPostCount() == 2) {
                return (X1 == xp && Y1 == yp) ? 0 : 1;
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
            BoundingBox.X = x1;
            BoundingBox.Y = y1;
            BoundingBox.Width = x2 - x1 + 1;
            BoundingBox.Height = y2 - y1 + 1;
        }

        /// <summary>
        /// set bounding box for an element from p1 to p2 with width w
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="w"></param>
        protected void setBbox(Point p1, Point p2, double w) {
            setBbox(p1.X, p1.Y, p2.X, p2.Y);
            int dpx = (int)(mUnitPx1 * w);
            int dpy = (int)(mUnitPy1 * w);
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
            x1 = Math.Min(BoundingBox.X, x1);
            y1 = Math.Min(BoundingBox.Y, y1);
            x2 = Math.Max(BoundingBox.X + BoundingBox.Width, x2);
            y2 = Math.Max(BoundingBox.Y + BoundingBox.Height, y2);
            BoundingBox.X = x1;
            BoundingBox.Y = y1;
            BoundingBox.Width = x2 - x1;
            BoundingBox.Height = y2 - y1;
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
            mCurCount = updateDotCount(mCurrent, mCurCount);
        }

        /// <summary>
        ///  update dot positions (curcount) for drawing current (general case for multiple currents)
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="cc"></param>
        /// <returns></returns>
        protected double updateDotCount(double cur, double cc) {
            if (!Sim.simIsRunning()) {
                return cc;
            }
            double cadd = cur * CurrentMult;
            cadd %= 8;
            return cc + cadd;
        }

        /// <summary>
        /// update and draw current for simple two-terminal element
        /// </summary>
        /// <param name="g"></param>
        protected void doDots(Graphics g) {
            updateDotCount();
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mPoint2, mCurCount);
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
                return SelectColor;
            }
            if (!Sim.chkVoltsCheckItem.Checked) {
                return WhiteColor;
            }
            int c = (int)((volts + VoltageRange) * (COLOR_SCALE_COUNT - 1) / (VoltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= COLOR_SCALE_COUNT) {
                c = COLOR_SCALE_COUNT - 1;
            }
            return mColorScale[c];
        }

        public virtual double getPower() { return getVoltageDiff() * mCurrent; }

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
            if (null == mMouseElmRef || null == Sim.plotYElm) {
                return false;
            }
            /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
            var isScopeElm = (mMouseElmRef is ScopeElm) && ((ScopeElm)mMouseElmRef).elmScope.getElm().Equals(this);
            return mMouseElmRef.Equals(this) || IsSelected || Sim.plotYElm.Equals(this) || isScopeElm;
        }

        public virtual bool canShowValueInScope(int v) { return false; }

        public void selectRect(Rectangle r) {
            IsSelected = r.IntersectsWith(BoundingBox);
        }

        public Rectangle getBoundingBox() { return BoundingBox; }

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
                return -mCurrent;
            } else {
                return mCurrent;
            }
        }

        public void flipPosts() {
            int oldx = X1;
            int oldy = Y1;
            X1 = X2;
            Y1 = Y2;
            X2 = oldx;
            Y2 = oldy;
            setPoints();
        }
    }
}
