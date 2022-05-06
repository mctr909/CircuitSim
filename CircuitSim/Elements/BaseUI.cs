﻿using System;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Elements.Output;

namespace Circuit.Elements {
    abstract class BaseUI : Editable {
        static BaseUI mMouseElmRef = null;
        public static CustomGraphics Context;
        public BaseElement CirElm;

        #region [property]
        public string ReferenceName { get; set; }

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
                var isScopeElm = (mMouseElmRef is ScopeUI) && ((ScopeUI)mMouseElmRef).elmScope.Elm.Equals(this);
                return mMouseElmRef.Equals(this) || IsSelected || isScopeElm;
            }
        }

        public virtual DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public virtual bool IsCreationFailed { get { return P1.X == P2.X && P1.Y == P2.Y; } }

        public virtual bool IsGraphicElmt { get { return false; } }

        /// <summary>
        /// needed for calculating circuit bounds (need to special-case centered text elements)
        /// </summary>
        /// <returns></returns>
        public virtual bool IsCenteredText { get { return false; } }

        public virtual bool CanViewInScope { get { return CirElm.PostCount <= 2; } }

        public virtual int DefaultFlags { get { return 0; } }

        protected virtual int NumHandles { get { return 2; } }
        #endregion

        #region [variable]
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

        public Rectangle BoundingBox;

        int mLastHandleGrabbed = -1;

        /* length along x and y axes, and sign of difference */
        protected Point mDiff;
        protected int mDsign;

        /* length of element */
        protected double mLen;

        /* direction of element */
        protected PointF mDir;

        /* post of objects */
        protected Point mPost1;
        protected Point mPost2;

        /* lead points (ends of wire stubs for simple two-terminal elements) */
        protected Point mLead1;
        protected Point mLead2;

        protected bool mNameV;
        protected Point mNamePos;
        protected Point mValuePos;

        /* if subclasses set this to true, element will be horizontal or vertical only */
        protected bool mNoDiagonal;

        protected int mFlags;
        #endregion

        /// <summary>
        /// create new element with one post at pos, to be dragged out by user
        /// </summary>
        protected BaseUI(Point pos) {
            P1.X = P2.X = pos.X;
            P1.Y = P2.Y = pos.Y;
            mFlags = DefaultFlags;
            initBoundingBox();
        }

        /// <summary>
        /// create element between p1 and p2 from undump
        /// </summary>
        protected BaseUI(Point p1, Point p2, int f) {
            P1 = p1;
            P2 = p2;
            mFlags = f;
            initBoundingBox();
        }

        public abstract DUMP_ID DumpType { get; }

        protected abstract string dump();

        void initBoundingBox() {
            BoundingBox = new Rectangle(Math.Min(P1.X, P2.X), Math.Min(P1.Y, P2.Y), Math.Abs(P2.X - P1.X) + 1, Math.Abs(P2.Y - P1.Y) + 1);
        }

        #region [protected method]
        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int len) {
            if (mLen < len || len == 0) {
                mLead1 = mPost1;
                mLead2 = mPost2;
                return;
            }
            setLead1((mLen - len) / (2 * mLen));
            setLead2((mLen + len) / (2 * mLen));
        }

        /// <summary>
        /// set/adjust bounding box used for selecting elements.
        /// getCircuitBounds() does not use this!
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        protected void setBbox(Point a, Point b) {
            if (a.X > b.X) { var q = a.X; a.X = b.X; b.X = q; }
            if (a.Y > b.Y) { var q = a.Y; a.Y = b.Y; b.Y = q; }
            BoundingBox.X = a.X;
            BoundingBox.Y = a.Y;
            BoundingBox.Width = b.X - a.X + 1;
            BoundingBox.Height = b.Y - a.Y + 1;
        }

        /// <summary>
        /// set bounding box for an element from a to b with width w
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="w"></param>
        protected void setBbox(Point a, Point b, double w) {
            setBbox(a, b);
            var dpx = (int)(mDir.X * w);
            var dpy = (int)(mDir.Y * w);
            adjustBbox(
                a.X + dpx, a.Y + dpy,
                a.X - dpx, a.Y - dpy
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

        protected void adjustBbox(Point a, Point b) {
            adjustBbox(a.X, a.Y, b.X, b.Y);
        }

        /// <summary>
        /// update and draw current for simple two-terminal element
        /// </summary>
        protected void doDots() {
            updateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPost1, mPost2, CirElm.CurCount);
            }
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
            double cadd = cur * CirSim.CurrentMult;
            cadd %= 8;
            return cc + cadd;
        }

        /// <summary>
        /// update dot positions (curcount) for drawing current (simple case for single current)
        /// </summary>
        protected void updateDotCount() {
            CirElm.CurCount = updateDotCount(CirElm.Current, CirElm.CurCount);
        }

        protected int getBasicInfo(string[] arr) {
            arr[1] = "I = " + Utils.CurrentAbsText(CirElm.Current);
            arr[2] = "Vd = " + Utils.VoltageAbsText(CirElm.VoltageDiff);
            return 3;
        }

        protected void setLead1(double w) {
            interpPoint(ref mLead1, w);
        }

        protected void setLead2(double w) {
            interpPoint(ref mLead2, w);
        }

        protected void interpPoint(ref Point p, double f) {
            p.X = (int)Math.Floor(mPost1.X * (1 - f) + mPost2.X * f + 0.5);
            p.Y = (int)Math.Floor(mPost1.Y * (1 - f) + mPost2.Y * f + 0.5);
        }

        protected void interpPoint(ref Point p, double f, double g) {
            var gx = mPost2.Y - mPost1.Y;
            var gy = mPost1.X - mPost2.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = mPost1.X;
                p.Y = mPost1.Y;
            } else {
                g /= r;
                p.X = (int)Math.Floor(mPost1.X * (1 - f) + mPost2.X * f + g * gx + 0.5);
                p.Y = (int)Math.Floor(mPost1.Y * (1 - f) + mPost2.Y * f + g * gy + 0.5);
            }
        }

        protected void interpPointAB(ref Point a, ref Point b, double f, double g) {
            var gx = mPost2.Y - mPost1.Y;
            var gy = mPost1.X - mPost2.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                a.X = mPost1.X;
                a.Y = mPost1.Y;
                b.X = mPost2.X;
                b.Y = mPost2.Y;
            } else {
                g /= r;
                a.X = (int)Math.Floor(mPost1.X * (1 - f) + mPost2.X * f + g * gx + 0.5);
                a.Y = (int)Math.Floor(mPost1.Y * (1 - f) + mPost2.Y * f + g * gy + 0.5);
                b.X = (int)Math.Floor(mPost1.X * (1 - f) + mPost2.X * f - g * gx + 0.5);
                b.Y = (int)Math.Floor(mPost1.Y * (1 - f) + mPost2.Y * f - g * gy + 0.5);
            }
        }

        protected void interpLead(ref Point p, double f) {
            p.X = (int)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f + 0.5);
            p.Y = (int)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f + 0.5);
        }

        protected void interpLead(ref Point p, double f, double g) {
            var gx = mLead2.Y - mLead1.Y;
            var gy = mLead1.X - mLead2.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = mLead1.X;
                p.Y = mLead1.Y;
            } else {
                g /= r;
                p.X = (int)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f + g * gx + 0.5);
                p.Y = (int)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f + g * gy + 0.5);
            }
        }

        protected void interpLeadAB(ref Point a, ref Point b, double f, double g) {
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
                a.X = (int)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f + g * gx + 0.5);
                a.Y = (int)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f + g * gy + 0.5);
                b.X = (int)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f - g * gx + 0.5);
                b.Y = (int)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f - g * gy + 0.5);
            }
        }

        protected void drawPosts() {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (CirSim.Sim.DragElm == null && !NeedsHighlight) {
                return;
            }
            if (CirSim.Sim.MouseMode == CirSim.MOUSE_MODE.DRAG_ROW || CirSim.Sim.MouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i != CirElm.PostCount; i++) {
                var p = GetPost(i);
                Context.DrawPost(p);
            }
        }

        protected void drawLead(Point a, Point b) {
            Context.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            Context.DrawLine(a, b);
        }

        protected void draw2Leads() {
            Context.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            /* draw first lead */
            Context.DrawLine(mPost1, mLead1);
            /* draw second lead */
            Context.DrawLine(mLead2, mPost2);
        }

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected void drawDots(Point a, Point b, double pos) {
            if ((!CirSim.Sim.IsRunning) || pos == 0 || !ControlPanel.ChkShowDots.Checked) {
                return;
            }
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dr = Math.Sqrt(dx * dx + dy * dy);
            int ds = CirSim.GRID_SIZE * 2;
            pos %= ds;
            if (pos < 0) {
                pos += ds;
            }
            if (ControlPanel.ChkPrintable.Checked) {
                Context.LineColor = CustomGraphics.GrayColor;
            } else {
                Context.LineColor = Color.Yellow;
            }
            for (var di = pos; di < dr; di += ds) {
                var x0 = (int)(a.X + di * dx / dr);
                var y0 = (int)(a.Y + di * dy / dr);
                Context.FillCircle(x0, y0, 1.5f);
            }
        }

        protected void drawCenteredText(string s, Point p, bool cx) {
            var fs = Context.GetTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(p.X - w / 2, p.Y - h2, p.X + w / 2, p.Y + h2);
            } else {
                adjustBbox(p.X, p.Y - h2, p.X + w, p.Y + h2);
            }
            Context.DrawCenteredText(s, p.X, p.Y);
        }

        protected void drawCenteredLText(string s, Point p, bool cx) {
            var fs = Context.GetLTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(p.X - w / 2, p.Y - h2, p.X + w / 2, p.Y + h2);
            } else {
                adjustBbox(p.X, p.Y - h2, p.X + w, p.Y + h2);
            }
            Context.DrawCenteredLText(s, p.X, p.Y);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="s"></param>
        protected void drawValues(string s, int offsetX = 0, int offsetY = 0) {
            if (s == null) {
                return;
            }
            var textSize = Context.GetTextSize(s);
            int xc, yc;
            // Todo: drawValues
            //if ((this is RailElm) || (this is SweepElm)) {
            if (this is RailUI) {
                xc = P2.X;
                yc = P2.Y;
            } else {
                xc = (P2.X + P1.X) / 2;
                yc = (P2.Y + P1.Y) / 2;
            }
            Context.DrawRightText(s, xc + offsetX, (int)(yc - textSize.Height + offsetY));
        }

        protected void drawValue(double value) {
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.UnitText(value);
                if (mNameV) {
                    Context.DrawCenteredVText(s, mValuePos.X, mValuePos.Y);
                } else {
                    Context.DrawLeftText(s, mValuePos.X, mValuePos.Y);
                }
            }
        }

        protected void drawName() {
            if (ControlPanel.ChkShowName.Checked) {
                if (mNameV) {
                    Context.DrawCenteredVText(ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    Context.DrawRightText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        protected void drawCoil(Point a, Point b, double v1, double v2) {
            var coilLen = (float)Utils.Distance(a, b);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            Context.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            var loopCt = (int)Math.Ceiling(coilLen / 11);
            var w = coilLen / loopCt;
            var th = (float)(Utils.Angle(a, b) * 180 / Math.PI);
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(a, b, ref pos, (loop + 0.5) / loopCt, 0);
                Context.DrawArc(pos, w, th, -180);
            }
        }

        protected void drawCoil(Point a, Point b, double v1, double v2, float dir) {
            var coilLen = (float)Utils.Distance(a, b);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            Context.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            var loopCt = (int)Math.Ceiling(coilLen / 9);
            float w = coilLen / loopCt;
            if (Utils.Angle(a, b) < 0) {
                dir = -dir;
            }
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(a, b, ref pos, (loop + 0.5) / loopCt, 0);
                Context.DrawArc(pos, w, dir, -180);
            }
        }
        #endregion

        #region [public method]
        public void DrawHandles(CustomGraphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(CustomGraphics.PenHandle, P1.X - 3, P1.Y - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(CustomGraphics.PenHandle, P1.X - 4, P1.Y - 4, 9, 9);
            }
            if (NumHandles == 2) {
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
            for (int i = 0; i != CirSim.Sim.ElmCount; i++) {
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
            if (CirElm.PostCount == 2) {
                return (P1.X == xp && P1.Y == yp) ? 0 : 1;
            }
            for (int i = 0; i != CirElm.PostCount; i++) {
                var p = GetPost(i);
                if (p.X == xp && p.Y == yp) {
                    return i;
                }
            }
            return 0;
        }

        public string DispPostVoltage(int x) {
            if (x < CirElm.Volts.Length) {
                return Utils.UnitText(CirElm.Volts[x], "V");
            } else {
                return "";
            }
        }
        #endregion

        #region [virtual method]
        public virtual void Delete() {
            if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
            if (null != CirSim.Sim) {
                CirSim.Sim.DeleteSliders(this);
            }
        }

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

        public virtual double Distance(double x, double y) {
            return Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P2.Y, x, y);
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
            var sx = mPost2.X - mPost1.X;
            var sy = mPost2.Y - mPost1.Y;
            var r = (float)Math.Sqrt(sx * sx + sy * sy);
            if (r == 0) {
                mDir.X = 0;
                mDir.Y = 0;
            } else {
                mDir.X = sy / r;
                mDir.Y = -sx / r;
            }
            mDsign = (mDiff.Y == 0) ? Math.Sign(mDiff.X) : Math.Sign(mDiff.Y);
            mPost1 = new Point(P1.X, P1.Y);
            mPost2 = new Point(P2.X, P2.Y);
        }

        public virtual void SetMouseElm(bool v) {
            if (v) {
                mMouseElmRef = this;
            } else if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
        }

        /// <summary>
        /// get position of nth node
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual Point GetPost(int n) {
            return (n == 0) ? mPost1 : (n == 1) ? mPost2 : new Point();
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

        public virtual string DumpModel() { return null; }

        public virtual void UpdateModels() { }

        public virtual ElementInfo GetElementInfo(int n) { return null; }

        public virtual void SetElementValue(int n, ElementInfo ei) { }
        #endregion
    }
}
