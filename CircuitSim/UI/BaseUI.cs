using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.UI.Input;
using Circuit.UI.Output;

namespace Circuit.UI {
    public interface Editable {
        ElementInfo GetElementInfo(int r, int c);
        void SetElementValue(int r, int c, ElementInfo ei);
    }

    public class BaseLink {
        public virtual int GetGroup(int id) { return 0; }
        public virtual void SetValue(BaseElement element, int linkID, double value) { }
        public virtual void Load(StringTokenizer st) { }
        public virtual void Dump(List<object> optionList) { }
    }

    public abstract class BaseUI : Editable {
        public BaseElement Elm;
        public static CustomGraphics Context;
        static BaseUI mMouseElmRef = null;

        #region [property]
        public abstract DUMP_ID DumpType { get; }

        public DumpInfo DumpInfo { get; protected set; }

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
                var optionList = new List<object>();
                dump(optionList);
                mLink.Dump(optionList);
                return DumpInfo.GetValue(DumpType, optionList);
            }
        }

        public bool NeedsShortcut { get { return Shortcut > 0 && (int)Shortcut <= 127; } }

        public bool NeedsHighlight {
            get {
                if (null == mMouseElmRef) {
                    return IsSelected;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScopeElm = (mMouseElmRef is Scope) && ((Scope)mMouseElmRef).Properties.UI.Equals(this);
                return mMouseElmRef.Equals(this) || IsSelected || isScopeElm;
            }
        }

        public virtual DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public virtual bool IsCreationFailed { get { return DumpInfo.IsCreationFailed; } }

        public virtual bool IsGraphicElmt { get { return false; } }

        public virtual bool CanViewInScope { get { return Elm.PostCount <= 2; } }

        public virtual int DefaultFlags { get { return 0; } }

        protected virtual int NumHandles { get { return 2; } }

        protected double CurCount;

        protected virtual BaseLink mLink { get; set; } = new BaseLink();
        #endregion

        #region [variable]
        /* length along x and y axes, and sign of difference */
        protected Point mDiff;
        protected int mDsign;

        /* length of element */
        protected double mLen;

        /* direction of element */
        protected PointF mDir;

        /* lead points (ends of wire stubs for simple two-terminal elements) */
        protected Point mLead1;
        protected Point mLead2;

        protected bool mVertical;
        protected bool mHorizontal;
        protected Point mNamePos;
        protected Point mValuePos;

        /* if subclasses set this to true, element will be horizontal or vertical only */
        protected bool mNoDiagonal;
        #endregion

        protected BaseUI() { }

        /// <summary>
        /// create new element with one post at pos, to be dragged out by user
        /// </summary>
        protected BaseUI(Point pos) {
            DumpInfo = new DumpInfo(pos, DefaultFlags);
        }

        /// <summary>
        /// create element between p1 and p2 from undump
        /// </summary>
        protected BaseUI(Point p1, Point p2, int f) {
            DumpInfo = new DumpInfo(p1, p2, f);
        }

        #region [protected method]
        protected virtual void dump(List<object> optionList) { }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int len) {
            if (mLen < len || len == 0) {
                mLead1 = Elm.Post[0];
                mLead2 = Elm.Post[1];
                return;
            }
            setLead1((mLen - len) / (2 * mLen));
            setLead2((mLen + len) / (2 * mLen));
        }

        protected void setBbox(int ax, int ay, int bx, int by, double w) {
            DumpInfo.SetBbox(ax, ay, bx, by);
            var dpx = (int)(mDir.X * w);
            var dpy = (int)(mDir.Y * w);
            DumpInfo.AdjustBbox(
                ax + dpx, ay + dpy,
                ax - dpx, ay - dpy
            );
        }

        protected void setBbox(int ax, int ay, Point b, double w) {
            setBbox(ax, ay, b.X, b.Y, w);
        }

        /// <summary>
        /// set bounding box for an element from a to b with width w
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="w"></param>
        protected void setBbox(Point a, Point b, double w) {
            setBbox(a.X, a.Y, b.X, b.Y, w);
        }

        protected void setBbox(double w) {
            var dpx = (int)(mDir.X * w);
            var dpy = (int)(mDir.Y * w);
            DumpInfo.SetBbox(
                Elm.Post[0].X + dpx, Elm.Post[0].Y + dpy,
                Elm.Post[0].X - dpx, Elm.Post[0].Y - dpy
            );
            DumpInfo.AdjustBbox(
                Elm.Post[0].X + dpx, Elm.Post[0].Y + dpy,
                Elm.Post[0].X - dpx, Elm.Post[0].Y - dpy
            );
        }

        /// <summary>
        /// update and draw current for simple two-terminal element
        /// </summary>
        protected void doDots() {
            updateDotCount();
            if (CirSimForm.DragElm != this) {
                drawCurrent(Elm.Post[0], Elm.Post[1], CurCount);
            }
        }

        /// <summary>
        ///  update dot positions (curcount) for drawing current (general case for multiple currents)
        /// </summary>
        /// <param name="current"></param>
        /// <param name="count"></param>
        protected void updateDotCount(double current, ref double count) {
            if (!CirSimForm.IsRunning) {
                return;
            }
            var speed = current * CirSimForm.CurrentMult;
            speed %= CirSimForm.CURRENT_DOT_SIZE;
            count += speed;
        }

        /// <summary>
        /// update dot positions (curcount) for drawing current (simple case for single current)
        /// </summary>
        protected void updateDotCount() {
            updateDotCount(Elm.Current, ref CurCount);
        }

        protected int getBasicInfo(string[] arr) {
            arr[1] = "I = " + Utils.CurrentAbsText(Elm.Current);
            arr[2] = "Vd = " + Utils.VoltageAbsText(Elm.GetVoltageDiff());
            return 3;
        }

        protected void setLead1(double w) {
            interpPost(ref mLead1, w);
        }

        protected void setLead2(double w) {
            interpPost(ref mLead2, w);
        }

        protected void interpPost(ref Point p, double f) {
            p.X = (int)Math.Floor(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f + 0.5);
            p.Y = (int)Math.Floor(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f + 0.5);
        }

        protected void interpPost(ref Point p, double f, double g) {
            var gx = Elm.Post[1].Y - Elm.Post[0].Y;
            var gy = Elm.Post[0].X - Elm.Post[1].X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = Elm.Post[0].X;
                p.Y = Elm.Post[0].Y;
            } else {
                g /= r;
                p.X = (int)Math.Floor(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f + g * gx + 0.5);
                p.Y = (int)Math.Floor(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f + g * gy + 0.5);
            }
        }

        protected void interpPostAB(ref Point a, ref Point b, double f, double g) {
            var gx = Elm.Post[1].Y - Elm.Post[0].Y;
            var gy = Elm.Post[0].X - Elm.Post[1].X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                a = Elm.Post[0];
                b = Elm.Post[1];
            } else {
                g /= r;
                a.X = (int)Math.Floor(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f + g * gx + 0.5);
                a.Y = (int)Math.Floor(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f + g * gy + 0.5);
                b.X = (int)Math.Floor(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f - g * gx + 0.5);
                b.Y = (int)Math.Floor(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f - g * gy + 0.5);
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

        protected void setLinkedValues<T>(int linkID, double value) {
            mLink.SetValue(Elm, linkID, value);
            if (mLink.GetGroup(linkID) == 0) {
                return;
            }
            for (int i = 0; i != CirSimForm.UICount; i++) {
                var u2 = CirSimForm.GetUI(i);
                if (u2 is T) {
                    if (u2.mLink.GetGroup(linkID) == mLink.GetGroup(linkID)) {
                        mLink.SetValue(u2.Elm, linkID, value);
                    }
                }
            }
        }
        #endregion

        #region [public method]
        public void DrawHandles(CustomGraphics g) {
            g.DrawHandle(DumpInfo.P1);
            if (2 <= NumHandles) {
                g.DrawHandle(DumpInfo.P2);
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
            DumpInfo.SetPosition(ax, ay, bx, by);
            SetPoints();
        }

        public void Move(int dx, int dy) {
            DumpInfo.Move(dx, dy);
            SetPoints();
        }

        public void MovePoint(int n, int dx, int dy) {
            DumpInfo.MovePoint(n, dx, dy);
            SetPoints();
        }

        public void FlipPosts() {
            DumpInfo.FlipPosts();
            SetPoints();
        }

        /// <summary>
        /// determine if moving this element by (dx,dy) will put it on top of another element
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool AllowMove(int dx, int dy) {
            int nx = DumpInfo.P1.X + dx;
            int ny = DumpInfo.P1.Y + dy;
            int nx2 = DumpInfo.P2.X + dx;
            int ny2 = DumpInfo.P2.Y + dy;
            for (int i = 0; i != CirSimForm.UICount; i++) {
                var ce = CirSimForm.GetUI(i);
                var ceP1 = ce.DumpInfo.P1;
                var ceP2 = ce.DumpInfo.P2;
                if (ceP1.X == nx && ceP1.Y == ny && ceP2.X == nx2 && ceP2.Y == ny2) {
                    return false;
                }
                if (ceP1.X == nx2 && ceP1.Y == ny2 && ceP2.X == nx && ceP2.Y == ny) {
                    return false;
                }
            }
            return true;
        }

        public void SelectRect(RectangleF r) {
            IsSelected = r.IntersectsWith(DumpInfo.BoundingBox);
        }

        public string DispPostVoltage(int x) {
            if (x < Elm.Volts.Length) {
                return Utils.UnitText(Elm.Volts[x], "V");
            } else {
                return "";
            }
        }
        #endregion

        #region [virtual method]
        public virtual double Distance(int x, int y) {
            return DumpInfo.Distance(x, y);
        }

        public virtual void Delete() {
            if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
            CirSimForm.DeleteSliders(this);
        }

        public virtual void Draw(CustomGraphics g) { }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="pos"></param>
        public virtual void Drag(Point pos) {
            DumpInfo.Drag(pos, mNoDiagonal);
            SetPoints();
        }

        public virtual void DraggingDone() { }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void SetPoints() {
            mDiff.X = DumpInfo.P2.X - DumpInfo.P1.X;
            mDiff.Y = DumpInfo.P2.Y - DumpInfo.P1.Y;
            mLen = Math.Sqrt(mDiff.X * mDiff.X + mDiff.Y * mDiff.Y);
            mDsign = (mDiff.Y == 0) ? Math.Sign(mDiff.X) : Math.Sign(mDiff.Y);
            var sx = DumpInfo.P2.X - DumpInfo.P1.X;
            var sy = DumpInfo.P2.Y - DumpInfo.P1.Y;
            var r = (float)Math.Sqrt(sx * sx + sy * sy);
            if (r == 0) {
                mDir.X = 0;
                mDir.Y = 0;
            } else {
                mDir.X = sy / r;
                mDir.Y = -sx / r;
            }
            mVertical = DumpInfo.P1.X == DumpInfo.P2.X;
            mHorizontal = DumpInfo.P1.Y == DumpInfo.P2.Y;
            Elm.Post[0] = DumpInfo.P1;
            Elm.Post[1] = DumpInfo.P2;
        }

        public virtual void SetMouseElm(bool v) {
            if (v) {
                mMouseElmRef = this;
            } else if (mMouseElmRef == this) {
                mMouseElmRef = null;
            }
        }

        /// <summary>
        /// get component info for display in lower right
        /// </summary>
        /// <param name="arr"></param>
        public virtual void GetInfo(string[] arr) { }

        public virtual bool CanShowValueInScope(int v) { return false; }

        public virtual string GetScopeText() {
            var info = new string[10];
            GetInfo(info);
            return info[0];
        }

        public virtual string DumpModel() { return null; }

        public virtual void UpdateModels() { }

        public virtual ElementInfo GetElementInfo(int r, int c) { return null; }

        public virtual void SetElementValue(int r, int c, ElementInfo ei) { }

        public virtual EventHandler CreateSlider(ElementInfo ei, Adjustable adj) { return null; }
        #endregion

        #region [draw method]
        protected void drawLine(Point a, Point b) {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(a.X, a.Y, b.X, b.Y);
        }

        protected void drawLine(float ax, float ay, float bx, float by) {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(ax, ay, bx, by);
        }

        protected void drawCircle(Point p, float radius) {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawCircle(p, radius);
        }

        protected void drawPolygon(Point[] p) {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawPolygon(p);
        }

        protected void fillPolygon(Point[] p) {
            var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.FillPolygon(color, p);
        }

        protected void fillPolygon(PointF[] p) {
            var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.FillPolygon(color, p);
        }

        protected void drawPosts() {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (CirSimForm.DragElm == null && !NeedsHighlight) {
                return;
            }
            if (CirSimForm.MouseMode == CirSimForm.MOUSE_MODE.DRAG_ROW || CirSimForm.MouseMode == CirSimForm.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i < Elm.PostCount; i++) {
                var p = Elm.GetPost(i);
                Context.DrawPost(p);
            }
        }

        protected void drawLeadA() {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(Elm.Post[0], mLead1);
        }

        protected void drawLeadB() {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(mLead2, Elm.Post[1]);
        }

        protected void draw2Leads() {
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            /* draw first lead */
            Context.DrawLine(Elm.Post[0], mLead1);
            /* draw second lead */
            Context.DrawLine(mLead2, Elm.Post[1]);
        }

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected void drawCurrent(Point a, Point b, double pos) {
            drawCurrent(a.X, a.Y, b.X, b.Y, pos);
        }

        protected void drawCurrent(float ax, float ay, float bx, float by, double pos) {
            if ((!CirSimForm.IsRunning) || pos == 0 || !ControlPanel.ChkShowDots.Checked) {
                return;
            }
            if (ControlPanel.ChkPrintable.Checked) {
                return;
            }
            pos %= CirSimForm.CURRENT_DOT_SIZE;
            if (pos < 0) {
                pos += CirSimForm.CURRENT_DOT_SIZE;
            }
            var nx = bx - ax;
            var ny = by - ay;
            var r = (float)Math.Sqrt(nx * nx + ny * ny);
            nx /= r;
            ny /= r;
            for (var di = pos; di < r; di += CirSimForm.CURRENT_DOT_SIZE) {
                var x0 = (int)(ax + di * nx);
                var y0 = (int)(ay + di * ny);
                Context.DrawCurrent(x0, y0, 0.5f);
            }
        }

        protected void drawCurrentA(double pos) {
            drawCurrent(Elm.Post[0], mLead1, pos);
        }

        protected void drawCurrentB(double pos) {
            drawCurrent(mLead2, Elm.Post[1], pos);
        }

        protected void drawCenteredText(string s, int x, int y, bool cx) {
            var fs = Context.GetTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                DumpInfo.AdjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                DumpInfo.AdjustBbox(x, y - h2, x + w, y + h2);
            }
            Context.DrawCenteredText(s, x, y);
        }

        protected void drawCenteredText(string s, Point p, bool cx) {
            drawCenteredText(s, p.X, p.Y, cx);
        }

        protected void drawCenteredLText(string s, int x, int y, bool cx) {
            var fs = Context.GetTextSizeL(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                DumpInfo.AdjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                DumpInfo.AdjustBbox(x, y - h2, x + w, y + h2);
            }
            Context.DrawCenteredLText(s, x, y);
        }

        protected void drawCenteredLText(string s, Point p, bool cx) {
            drawCenteredLText(s, p.X, p.Y, cx);
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
            if (this is Rail || this is Sweep) {
                xc = DumpInfo.P2.X;
                yc = DumpInfo.P2.Y;
            } else {
                xc = (DumpInfo.P2.X + DumpInfo.P1.X) / 2;
                yc = (DumpInfo.P2.Y + DumpInfo.P1.Y) / 2;
            }
            Context.DrawRightText(s, xc + offsetX, (int)(yc - textSize.Height + offsetY));
        }

        /// <summary>
        /// draw component name
        /// </summary>
        /// <param name="s"></param>
        protected void drawName(string s, int offsetX = 0, int offsetY = 0) {
            if (s == null) {
                return;
            }
            var textSize = Context.GetTextSize(s);
            int xc, yc;
            if (this is Rail) {
                xc = DumpInfo.P2.X;
                yc = DumpInfo.P2.Y;
            } else {
                xc = (DumpInfo.P2.X + DumpInfo.P1.X) / 2;
                yc = (DumpInfo.P2.Y + DumpInfo.P1.Y) / 2;
            }
            Context.DrawLeftText(s, xc + offsetX, (int)(yc - textSize.Height + offsetY));
        }

        protected void drawValue(double value) {
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.UnitText(value);
                if (mVertical) {
                    Context.DrawCenteredVText(s, mValuePos.X, mValuePos.Y);
                } else if (mHorizontal) {
                    Context.DrawCenteredText(s, mValuePos.X, mValuePos.Y);
                } else {
                    Context.DrawLeftText(s, mValuePos.X, mValuePos.Y);
                }
            }
        }

        protected void drawName() {
            if (ControlPanel.ChkShowName.Checked) {
                if (mVertical) {
                    Context.DrawCenteredVText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                } else if (mHorizontal) {
                    Context.DrawCenteredText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    Context.DrawRightText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        protected void drawCoil(Point a, Point b, double v1, double v2) {
            var coilLen = (float)Utils.Distance(a, b);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
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
            Context.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
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
    }
}
