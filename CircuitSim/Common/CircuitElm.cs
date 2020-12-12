using System;
using System.Drawing;

namespace Circuit.Elements {
    abstract class CircuitElm : Editable {
        public static readonly Color SelectColor = Color.Cyan;

        protected const double Pi = Math.PI;
        protected const double Pi2 = Math.PI * 2;
        protected const double ToDeg = 180 / Pi;
        protected const double ToRad = Pi / 180;

        const int ColorScaleCount = 64;
        static readonly Brush PenHandle = Brushes.Cyan;

        protected static Circuit mCir;
        static Color[] mColorScale;
        static CircuitElm mMouseElmRef = null;

        #region static property
        public static CirSim Sim { get; private set; }
        public static double VoltageRange { get; set; } = 5;
        public static double CurrentMult { get; set; }
        public static Color TextColor { get; set; }
        public static Color WhiteColor { get; set; }
        public static Color GrayColor { get; set; }
        #endregion

        #region dynamic property
        /* initial point where user created element.
         * For simple two-terminal elements, this is the first node/post. */
        public int X1 { get; set; }
        public int Y1 { get; set; }

        /* point to which user dragged out element.
         * For simple two-terminal elements, this is the second node/post */
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public bool IsSelected { get; set; }

        public bool IsMouseElm {
            get {
                if (null == mMouseElmRef) {
                    return false;
                }
                return mMouseElmRef.Equals(this);
            }
        }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public bool IsCreationFailed { get { return X1 == X2 && Y1 == Y2; } }

        public int[] Nodes { get; protected set; }

        /// <summary>
        /// voltages at each node
        /// </summary>
        public double[] Volts { get; protected set; }

        /// <summary>
        /// dump component state for export/undo
        /// </summary>
        public string Dump {
            get {
                var type = DumpType;
                return string.Format("{0} {1} {2} {3} {4} {5} {6}", type, X1, Y1, X2, Y2, mFlags, dump());
            }
        }

        public bool NeedsShortcut { get { return Shortcut > 0 && (int)Shortcut <= 127; } }

        public bool NeedsHighlight {
            get {
                if (null == mMouseElmRef) {
                    return false;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScopeElm = (mMouseElmRef is ScopeElm) && ((ScopeElm)mMouseElmRef).elmScope.getElm().Equals(this);
                return mMouseElmRef.Equals(this) || IsSelected || isScopeElm;
            }
        }
        #endregion

        #region virtual property
        public virtual DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        /// <summary>
        /// is this a wire or equivalent to a wire?
        /// </summary>
        /// <returns></returns>
        public virtual bool IsWire { get { return false; } }

        public virtual bool IsGraphicElmt { get { return false; } }

        /// <summary>
        /// needed for calculating circuit bounds (need to special-case centered text elements)
        /// </summary>
        /// <returns></returns>
        public virtual bool IsCenteredText { get { return false; } }

        public virtual bool CanViewInScope { get { return PostCount <= 2; } }

        public virtual double Current { get { return mCurrent; } }

        public virtual double VoltageDiff { get { return Volts[0] - Volts[1]; } }

        public virtual double Power { get { return VoltageDiff * mCurrent; } }

        /// <summary>
        /// number of voltage sources this element needs
        /// </summary>
        /// <returns></returns>
        public virtual int VoltageSourceCount { get { return 0; } }

        /// <summary>
        /// number of internal nodes (nodes not visible in UI that are needed for implementation)
        /// </summary>
        /// <returns></returns>
        public virtual int InternalNodeCount { get { return 0; } }

        public virtual bool NonLinear { get { return false; } }

        public virtual int PostCount { get { return 2; } }

        /// <summary>
        /// get number of nodes that can be retrieved by ConnectionNode
        /// </summary>
        /// <returns></returns>
        public virtual int ConnectionNodeCount { get { return PostCount; } }

        public virtual int DefaultFlags { get { return 0; } }
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
        protected double mLen;

        /* direction of element */
        protected double mDirX;
        protected double mDirY;

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
            mCir = c;
            mMouseElmRef = null;
            CurrentMult = 0;
        }

        public static void setColorScale() {
            mColorScale = new Color[ColorScaleCount];
            for (int i = 0; i != ColorScaleCount; i++) {
                double v = (i * 2.0 / ColorScaleCount - 1.0) * 0.66;
                if (v < 0) {
                    int n1 = (int)(128 * -v) + 127;
                    int n2 = (int)(127 * (1 + v));
                    mColorScale[i] = Color.FromArgb(n2, n2, n1);
                } else {
                    int n1 = (int)(128 * v) + 127;
                    int n2 = (int)(127 * (1 - v));
                    mColorScale[i] = Color.FromArgb(n2, n1, n2);
                }
            }
        }

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="g"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected static void drawDots(CustomGraphics g, Point a, Point b, double pos) {
            if ((!Sim.simIsRunning()) || pos == 0 || !Sim.chkShowDots.Checked) {
                return;
            }
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            double dn = Math.Sqrt(dx * dx + dy * dy);
            int ds = 16;
            pos %= ds;
            if (pos < 0) {
                pos += ds;
            }
            double di = 0;
            if (Sim.chkPrintable.Checked) {
                g.LineColor = GrayColor;
            } else {
                g.LineColor = Color.Yellow;
            }
            for (di = pos; di < dn; di += ds) {
                var x0 = (float)(a.X + di * dx / dn);
                var y0 = (float)(a.Y + di * dy / dn);
                g.FillCircle(x0, y0, 2);
            }
        }
        
        /// <summary>
        /// create new element with one post at xx,yy, to be dragged out by user
        /// </summary>
        protected CircuitElm(int xx, int yy) {
            X1 = X2 = xx;
            Y1 = Y2 = yy;
            mFlags = DefaultFlags;
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

        public abstract DUMP_ID DumpType { get; }

        protected abstract string dump();

        void initBoundingBox() {
            BoundingBox = new Rectangle(Math.Min(X1, X2), Math.Min(Y1, Y2), Math.Abs(X2 - X1) + 1, Math.Abs(Y2 - Y1) + 1);
        }

        #region [protected method]
        /// <summary>
        /// allocate nodes/volts arrays we need
        /// </summary>
        protected void allocNodes() {
            int n = PostCount + InternalNodeCount;
            /* preserve voltages if possible */
            if (Nodes == null || Nodes.Length != n) {
                Nodes = new int[n];
                Volts = new double[n];
            }
        }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int len) {
            if (mLen < len || len == 0) {
                mLead1 = mPoint1;
                mLead2 = mPoint2;
                return;
            }
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, (mLen - len) / (2 * mLen));
            mLead2 = Utils.InterpPoint(mPoint1, mPoint2, (mLen + len) / (2 * mLen));
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
            int dpx = (int)(mDirX * w);
            int dpy = (int)(mDirY * w);
            adjustBbox(
                p1.X + dpx, p1.Y + dpy,
                p1.X - dpx, p1.Y - dpy
            );
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
        protected void doDots(CustomGraphics g) {
            updateDotCount();
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mPoint2, mCurCount);
            }
        }

        protected int getBasicInfo(string[] arr) {
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
            arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            return 3;
        }

        protected bool comparePair(int x1, int x2, int y1, int y2) {
            return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
        }

        protected Color getVoltageColor(double volts) {
            if (NeedsHighlight) {
                return SelectColor;
            }
            if (!Sim.chkShowVolts.Checked || Sim.chkPrintable.Checked) {
                return GrayColor;
            }
            int c = (int)((volts + VoltageRange) * (ColorScaleCount - 1) / (VoltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= ColorScaleCount) {
                c = ColorScaleCount - 1;
            }
            return mColorScale[c];
        }

        protected void drawPosts(CustomGraphics g) {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (Sim.dragElm == null && !NeedsHighlight) {
                return;
            }
            if (Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_ROW || Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i != PostCount; i++) {
                var p = GetPost(i);
                g.DrawPost(p);
            }
        }

        protected void draw2Leads(CustomGraphics g) {
            /* draw first lead */
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            /* draw second lead */
            g.DrawThickLine(getVoltageColor(Volts[1]), mLead2, mPoint2);
        }

        protected void drawCenteredText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredText(s, x, y);
        }

        protected void drawCenteredLText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetLTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredLText(s, x, y);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="g"></param>
        /// <param name="s"></param>
        protected void drawValues(CustomGraphics g, string s, int offsetX = 0, int offsetY = 0) {
            if (s == null) {
                return;
            }
            var textSize = g.GetTextSize(s);
            int xc, yc;
            if ((this is RailElm) || (this is SweepElm)) {
                xc = X2;
                yc = Y2;
            } else {
                xc = (X2 + X1) / 2;
                yc = (Y2 + Y1) / 2;
            }
            g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float h = w * 1.2f;
            float wh = w * 0.5f;
            float hh = h * 0.5f;
            float th = (float)(Utils.Angle(p1, p2) * ToDeg);
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, th, -180);
            }
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2, float dir) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float wh = w * 0.5f;
            if (Utils.Angle(p1, p2) < 0) {
                dir = -dir;
            }
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, dir, -180);
            }
        }
        #endregion

        #region [public method]
        public void DrawHandles(CustomGraphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(PenHandle, X1 - 3, Y1 - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(PenHandle, X1 - 4, Y1 - 4, 9, 9);
            }
            if (mNumHandles == 2) {
                if (mLastHandleGrabbed == -1) {
                    g.FillRectangle(PenHandle, X2 - 3, Y2 - 3, 7, 7);
                } else if (mLastHandleGrabbed == 1) {
                    g.FillRectangle(PenHandle, X2 - 4, Y2 - 4, 9, 9);
                }
            }
        }

        /// <summary>
        /// this is used to set the position of an internal element so we can draw it inside the parent
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        public void SetPosition(int ax, int ay, int bx, int by) {
            X1 = ax;
            Y1 = ay;
            X2 = bx;
            Y2 = by;
            SetPoints();
        }

        public void Move(int dx, int dy) {
            X1 += dx;
            Y1 += dy;
            X2 += dx;
            Y2 += dy;
            BoundingBox.X += dx;
            BoundingBox.Y += dy;
            SetPoints();
        }

        /// <summary>
        /// determine if moving this element by (dx,dy) will put it on top of another element
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool AllowMove(int dx, int dy) {
            int nx = X1 + dx;
            int ny = Y1 + dy;
            int nx2 = X2 + dx;
            int ny2 = Y2 + dy;
            for (int i = 0; i != Sim.elmList.Count; i++) {
                var ce = Sim.getElm(i);
                if (ce.X1 == nx && ce.Y1 == ny && ce.X2 == nx2 && ce.Y2 == ny2) {
                    return false;
                }
                if (ce.X1 == nx2 && ce.Y1 == ny2 && ce.X2 == nx && ce.Y2 == ny) {
                    return false;
                }
            }
            return true;
        }

        public void MovePoint(int n, int dx, int dy) {
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
            SetPoints();
        }

        public void FlipPosts() {
            int oldx = X1;
            int oldy = Y1;
            X1 = X2;
            Y1 = Y2;
            X2 = oldx;
            Y2 = oldy;
            SetPoints();
        }

        public void SelectRect(Rectangle r) {
            IsSelected = r.IntersectsWith(BoundingBox);
        }

        public int GetHandleGrabbedClose(int xtest, int ytest, int deltaSq, int minSize) {
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

        public int GetNodeAtPoint(int xp, int yp) {
            if (PostCount == 2) {
                return (X1 == xp && Y1 == yp) ? 0 : 1;
            }
            for (int i = 0; i != PostCount; i++) {
                var p = GetPost(i);
                if (p.X == xp && p.Y == yp) {
                    return i;
                }
            }
            return 0;
        }

        public string DispPostVoltage(int x) {
            if (x < Volts.Length) {
                return Utils.UnitText(Volts[x], "V");
            } else {
                return "";
            }
        }
        #endregion

        #region [virtual method]
        /// <summary>
        /// handle reset button
        /// </summary>
        public virtual void Reset() {
            for (int i = 0; i != PostCount + InternalNodeCount; i++) {
                Volts[i] = 0;
            }
            mCurCount = 0;
        }

        public virtual void Delete() {
            if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
            if (null != Sim) {
                Sim.deleteSliders(this);
            }
        }

        /// <summary>
        /// stamp matrix values for linear elements.
        /// for non-linear elements, use this to stamp values that don't change each iteration,
        /// and call stampRightSide() or stampNonLinear() as needed
        /// </summary>
        public virtual void Stamp() { }

        /// <summary>
        /// stamp matrix values for non-linear elements
        /// </summary>
        public virtual void DoStep() { }

        public virtual void StartIteration() { }

        public virtual void StepFinished() { }

        public virtual void Draw(CustomGraphics g) { }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="xx"></param>
        /// <param name="yy"></param>
        public virtual void Drag(int xx, int yy) {
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
            SetPoints();
        }

        public virtual void DraggingDone() { }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void SetPoints() {
            mDx = X2 - X1;
            mDy = Y2 - Y1;
            mLen = Math.Sqrt(mDx * mDx + mDy * mDy);
            var sx = mPoint2.X - mPoint1.X;
            var sy = mPoint2.Y - mPoint1.Y;
            var r = Math.Sqrt(sx * sx + sy * sy);
            if (r == 0) {
                mDirX = 0;
                mDirY = 0;
            } else {
                mDirX = sy / r;
                mDirY = -sx / r;
            }
            mDsign = (mDy == 0) ? Math.Sign(mDx) : Math.Sign(mDy);
            mPoint1 = new Point(X1, Y1);
            mPoint2 = new Point(X2, Y2);
        }

        public virtual void SetMouseElm(bool v) {
            if (v) {
                mMouseElmRef = this;
            } else if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
        }

        /// <summary>
        /// set current for voltage source vn to c.
        /// vn will be the same value as in a previous call to setVoltageSource(n, vn)
        /// </summary>
        /// <param name="vn"></param>
        /// <param name="c"></param>
        public virtual void SetCurrent(int vn, double c) { mCurrent = c; }

        /// <summary>
        /// notify this element that its pth node is n.
        /// This value n can be passed to stampMatrix()
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public virtual void SetNode(int p, int n) {
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
        public virtual void SetVoltageSource(int n, int v) {
            /* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
            mVoltSource = v;
        }

        /// <summary>
        /// set voltage of x'th node, called by simulator logic
        /// </summary>
        /// <param name="n"></param>
        /// <param name="c"></param>
        public virtual void SetNodeVoltage(int n, double c) {
            Volts[n] = c;
            calculateCurrent();
        }

        /// <summary>
        /// calculate current in response to node voltages changing
        /// </summary>
        protected virtual void calculateCurrent() { }

        public virtual double GetCurrentIntoNode(int n) {
            /* if we take out the getPostCount() == 2 it gives the wrong value for rails */
            if (n == 0 && PostCount == 2) {
                return -mCurrent;
            } else {
                return mCurrent;
            }
        }

        /// <summary>
        /// get nodes that can be passed to getConnection(), to test if this element connects
        /// those two nodes; this is the same as getNode() for all but labeled nodes.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual int GetConnectionNode(int n) { return Nodes[n]; }

        /// <summary>
        /// are n1 and n2 connected by this element?  this is used to determine
        /// unconnected nodes, and look for loops
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public virtual bool GetConnection(int n1, int n2) { return true; }

        /// <summary>
        /// is n1 connected to ground somehow?
        /// </summary>
        /// <param name="n1"></param>
        /// <returns></returns>
        public virtual bool HasGroundConnection(int n1) { return false; }

        /// <summary>
        /// get position of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : new Point();
        }

        /// <summary>
        /// get component info for display in lower right
        /// </summary>
        /// <param name="arr"></param>
        public virtual void GetInfo(string[] arr) { }

        public virtual bool CanShowValueInScope(int v) { return false; }

        public virtual string GetScopeText(int v) {
            var info = new string[10];
            GetInfo(info);
            return info[0];
        }

        public virtual double GetScopeValue(int x) {
            return (x == Scope.VAL_CURRENT) ? mCurrent : (x == Scope.VAL_POWER) ? Power : VoltageDiff;
        }

        public virtual int GetScopeUnits(int x) {
            return (x == Scope.VAL_CURRENT) ? Scope.UNITS_A :
                (x == Scope.VAL_POWER) ? Scope.UNITS_W : Scope.UNITS_V;
        }

        public virtual string DumpModel() { return null; }

        public virtual void UpdateModels() { }

        public virtual EditInfo GetEditInfo(int n) { return null; }

        public virtual void SetEditValue(int n, EditInfo ei) { }
        #endregion
    }
}
