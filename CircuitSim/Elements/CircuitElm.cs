using System;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Elements.Output;

namespace Circuit.Elements {
    abstract class CircuitElm : Editable {
        public static double CurrentMult { get; set; }
        protected static Circuit mCir;
        private static Color[] mColorScale;
        private static CircuitElm mMouseElmRef = null;

        #region dynamic property
        /// <summary>
        /// initial point where user created element.
        /// For simple two-terminal elements, this is the first node/post.
        /// </summary>
        public Point P1;

        /// <summary>
        /// point to which user dragged out element.
        /// For simple two-terminal elements, this is the second node/post
        /// </summary>
        public Point P2;

        public bool IsSelected;

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
        public bool IsCreationFailed {
            get { return P1.X == P2.X && P1.Y == P2.Y; }
        }

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
                return string.Format("{0} {1} {2} {3} {4} {5} {6}", type, P1.X, P1.Y, P2.X, P2.Y, mFlags, dump());
            }
        }

        public bool NeedsShortcut { get { return Shortcut > 0 && (int)Shortcut <= 127; } }

        public bool NeedsHighlight {
            get {
                if (null == mMouseElmRef) {
                    return false;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScopeElm = (mMouseElmRef is ScopeElm) && ((ScopeElm)mMouseElmRef).elmScope.Elm.Equals(this);
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
        public RectangleF BoundingBox;

        protected int mFlags;
        protected int mVoltSource;

        int mLastHandleGrabbed = -1;
        protected int mNumHandles = 2;

        /* length along x and y axes, and sign of difference */
        protected Point mDiff;
        protected int mDsign;

        /* length of element */
        protected double mLen;

        /* direction of element */
        protected PointF mDir;

        /* Point objects */
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

        /// <summary>
        /// create new element with one post at pos, to be dragged out by user
        /// </summary>
        protected CircuitElm(Point pos) {
            P1.X = P2.X = pos.X;
            P1.Y = P2.Y = pos.Y;
            mFlags = DefaultFlags;
            allocNodes();
            initBoundingBox();
        }

        /// <summary>
        /// create element between p1 and p2 from undump
        /// </summary>
        protected CircuitElm(Point p1, Point p2, int f) {
            P1 = p1;
            P2 = p2;
            mFlags = f;
            allocNodes();
            initBoundingBox();
        }

        public abstract DUMP_ID DumpType { get; }

        protected abstract string dump();

        void initBoundingBox() {
            BoundingBox = new Rectangle(Math.Min(P1.X, P2.X), Math.Min(P1.Y, P2.Y), Math.Abs(P2.X - P1.X) + 1, Math.Abs(P2.Y - P1.Y) + 1);
        }

        #region [static method]
        public static void InitClass(Circuit c) {
            CurrentMult = 0;
            mCir = c;
            mMouseElmRef = null;
        }

        public static void SetColorScale(int colorScaleCount) {
            mColorScale = new Color[colorScaleCount];
            for (int i = 0; i != colorScaleCount; i++) {
                double v = (i * 2.0 / colorScaleCount - 1.0) * 0.66;
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
        protected static void drawDots(CustomGraphics g, PointF a, PointF b, double pos) {
            if ((!CirSim.Sim.IsRunning) || pos == 0 || !ControlPanel.ChkShowDots.Checked) {
                return;
            }
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            double dn = Math.Sqrt(dx * dx + dy * dy);
            int ds = CirSim.GRID_SIZE * 4;
            pos %= ds;
            if (pos < 0) {
                pos += ds;
            }
            if (ControlPanel.ChkPrintable.Checked) {
                g.LineColor = CustomGraphics.GrayColor;
            } else {
                g.LineColor = Color.Yellow;
            }
            for (var di = pos; di < dn; di += ds) {
                var x0 = (float)(a.X + di * dx / dn);
                var y0 = (float)(a.Y + di * dy / dn);
                g.FillCircle(x0, y0, 1.5f);
            }
        }
        #endregion

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
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, (mLen - len) / (2 * mLen));
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead2, (mLen + len) / (2 * mLen));
        }

        /// <summary>
        /// set/adjust bounding box used for selecting elements.
        /// getCircuitBounds() does not use this!
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        protected void setBbox(float x1, float y1, float x2, float y2) {
            if (x1 > x2) { var q = x1; x1 = x2; x2 = q; }
            if (y1 > y2) { var q = y1; y1 = y2; y2 = q; }
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
        protected void setBbox(PointF p1, PointF p2, double w) {
            setBbox(p1.X, p1.Y, p2.X, p2.Y);
            int dpx = (int)(mDir.X * w);
            int dpy = (int)(mDir.Y * w);
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
        protected void adjustBbox(float x1, float y1, float x2, float y2) {
            if (x1 > x2) { var q = x1; x1 = x2; x2 = q; }
            if (y1 > y2) { var q = y1; y1 = y2; y2 = q; }
            x1 = Math.Min(BoundingBox.X, x1);
            y1 = Math.Min(BoundingBox.Y, y1);
            x2 = Math.Max(BoundingBox.X + BoundingBox.Width, x2);
            y2 = Math.Max(BoundingBox.Y + BoundingBox.Height, y2);
            BoundingBox.X = x1;
            BoundingBox.Y = y1;
            BoundingBox.Width = x2 - x1;
            BoundingBox.Height = y2 - y1;
        }

        protected void adjustBbox(PointF p1, PointF p2) {
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
            if (!CirSim.Sim.IsRunning) {
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
            if (CirSim.Sim.DragElm != this) {
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
                return CustomGraphics.SelectColor;
            }
            if (!ControlPanel.ChkShowVolts.Checked || ControlPanel.ChkPrintable.Checked) {
                return CustomGraphics.GrayColor;
            }
            int c = (int)((volts + ControlPanel.VoltageRange) * (mColorScale.Length - 1) / (ControlPanel.VoltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= mColorScale.Length) {
                c = mColorScale.Length - 1;
            }
            return mColorScale[c];
        }

        protected void drawPosts(CustomGraphics g) {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (CirSim.Sim.DragElm == null && !NeedsHighlight) {
                return;
            }
            if (CirSim.Sim.MouseMode == CirSim.MOUSE_MODE.DRAG_ROW || CirSim.Sim.MouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
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

        protected void drawCenteredText(CustomGraphics g, string s, float x, float y, bool cx) {
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

        protected void drawCenteredLText(CustomGraphics g, string s, float x, float y, bool cx) {
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
                xc = P2.X;
                yc = P2.Y;
            } else {
                xc = (P2.X + P1.X) / 2;
                yc = (P2.Y + P1.Y) / 2;
            }
            g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 11);
            float w = coilLen / loopCt;
            float h = w * 1.2f;
            float wh = w * 0.5f;
            float hh = h * 0.5f;
            float th = (float)(Utils.Angle(p1, p2) * 180 / Math.PI);
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos, w, th, -180);
            }
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2, float dir) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 11);
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
                g.DrawThickArc(pos, w, dir, -180);
            }
        }
        #endregion

        #region [public method]
        public double Distance(double x, double y) {
            return Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P2.Y, x, y);
        }

        public void DrawHandles(CustomGraphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(CustomGraphics.PenHandle, P1.X - 3, P1.Y - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(CustomGraphics.PenHandle, P1.X - 4, P1.Y - 4, 9, 9);
            }
            if (mNumHandles == 2) {
                if (mLastHandleGrabbed == -1) {
                    g.FillRectangle(CustomGraphics.PenHandle, P2.X - 3, P2.Y - 3, 7, 7);
                } else if (mLastHandleGrabbed == 1) {
                    g.FillRectangle(CustomGraphics.PenHandle, P2.X - 4, P2.Y - 4, 9, 9);
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
            P1.X = ax;
            P1.Y = ay;
            P2.X = bx;
            P2.Y = by;
            SetPoints();
        }

        public void Move(int dx, int dy) {
            P1.X += dx;
            P1.Y += dy;
            P2.X += dx;
            P2.Y += dy;
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
            int nx = P1.X + dx;
            int ny = P1.Y + dy;
            int nx2 = P2.X + dx;
            int ny2 = P2.Y + dy;
            for (int i = 0; i != CirSim.Sim.ElmList.Count; i++) {
                var ce = CirSim.Sim.getElm(i);
                if (ce.P1.X == nx && ce.P1.Y == ny && ce.P2.X == nx2 && ce.P2.Y == ny2) {
                    return false;
                }
                if (ce.P1.X == nx2 && ce.P1.Y == ny2 && ce.P2.X == nx && ce.P2.Y == ny) {
                    return false;
                }
            }
            return true;
        }

        public void MovePoint(int n, int dx, int dy) {
            /* modified by IES to prevent the user dragging points to create zero sized nodes
            /* that then render improperly */
            int oldx = P1.X;
            int oldy = P1.Y;
            int oldx2 = P2.X;
            int oldy2 = P2.Y;
            if (n == 0) {
                P1.X += dx;
                P1.Y += dy;
            } else {
                P2.X += dx;
                P2.Y += dy;
            }
            if (P1.X == P2.X && P1.Y == P2.Y) {
                P1.X = oldx;
                P1.Y = oldy;
                P2.X = oldx2;
                P2.Y = oldy2;
            }
            SetPoints();
        }

        public void FlipPosts() {
            int oldx = P1.X;
            int oldy = P1.Y;
            P1.X = P2.X;
            P1.Y = P2.Y;
            P2.X = oldx;
            P2.Y = oldy;
            SetPoints();
        }

        public void SelectRect(RectangleF r) {
            IsSelected = r.IntersectsWith(BoundingBox);
        }

        public int GetHandleGrabbedClose(int xtest, int ytest, int deltaSq, int minSize) {
            mLastHandleGrabbed = -1;
            var x12 = P2.X - P1.X;
            var y12 = P2.Y - P1.Y;
            if (Math.Sqrt(x12 * x12 + y12 * y12) >= minSize) {
                var x1t = xtest - P1.X;
                var y1t = ytest - P1.Y;
                var x2t = xtest - P2.X;
                var y2t = ytest - P2.Y;
                if (Math.Sqrt(x1t * x1t + y1t * y1t) <= deltaSq) {
                    mLastHandleGrabbed = 0;
                } else if (Math.Sqrt(x2t * x2t + y2t * y2t) <= deltaSq) {
                    mLastHandleGrabbed = 1;
                }
            }
            return mLastHandleGrabbed;
        }

        public int GetHandleGrabbedClose(Point testp, int deltaSq, int minSize) {
            mLastHandleGrabbed = -1;
            var x12 = P2.X - P1.X;
            var y12 = P2.Y - P1.Y;
            if (Math.Sqrt(x12 * x12 + y12 * y12) >= minSize) {
                var x1t = testp.X - P1.X;
                var y1t = testp.Y - P1.Y;
                var x2t = testp.X - P2.X;
                var y2t = testp.Y - P2.Y;
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
                return (P1.X == xp && P1.Y == yp) ? 0 : 1;
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
            if (null != CirSim.Sim) {
                CirSim.Sim.DeleteSliders(this);
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
        /// <param name="pos"></param>
        public virtual void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            if (mNoDiagonal) {
                if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                    pos.X = P1.X;
                } else {
                    pos.Y = P1.Y;
                }
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public virtual void DraggingDone() { }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void SetPoints() {
            mDiff.X = P2.X - P1.X;
            mDiff.Y = P2.Y - P1.Y;
            mLen = Math.Sqrt(mDiff.X * mDiff.X + mDiff.Y * mDiff.Y);
            var sx = mPoint2.X - mPoint1.X;
            var sy = mPoint2.Y - mPoint1.Y;
            var r = (float)Math.Sqrt(sx * sx + sy * sy);
            if (r == 0) {
                mDir.X = 0;
                mDir.Y = 0;
            } else {
                mDir.X = sy / r;
                mDir.Y = -sx / r;
            }
            mDsign = (mDiff.Y == 0) ? Math.Sign(mDiff.X) : Math.Sign(mDiff.Y);
            mPoint1 = new Point(P1.X, P1.Y);
            mPoint2 = new Point(P2.X, P2.Y);
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

        public virtual string GetScopeText(Scope.VAL v) {
            var info = new string[10];
            GetInfo(info);
            return info[0];
        }

        public virtual double GetScopeValue(Scope.VAL x) {
            return VoltageDiff;
        }

        public virtual string DumpModel() { return null; }

        public virtual void UpdateModels() { }

        public virtual ElementInfo GetElementInfo(int n) { return null; }

        public virtual void SetElementValue(int n, ElementInfo ei) { }
        #endregion
    }
}
