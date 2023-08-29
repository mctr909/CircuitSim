using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.UI.Input;
using Circuit.UI.Output;

namespace Circuit.UI {
    public abstract class BaseUI {
        public BaseElement Elm;
        public static CustomGraphics Context;

        static BaseUI mMouseElm = null;

        protected BaseUI(Point pos) {
            Post = new Post(pos);
            mFlags = 0;
        }

        protected BaseUI(Point p1, Point p2, int f) {
            Post = new Post(p1, p2);
            mFlags = f;
        }

        #region [property]
        public string ReferenceName { get; set; }

        public Post Post { get; protected set; }

        public bool IsSelected { get; set; }

        public bool IsMouseElm {
            get {
                if (null == mMouseElm) {
                    return false;
                }
                return mMouseElm.Equals(this);
            }
        }

        public abstract DUMP_ID DumpType { get; }

        public virtual DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public virtual bool IsCreationFailed { get { return Post.IsCreationFailed; } }

        public virtual bool CanViewInScope { get { return Elm.PostCount <= 2; } }

        protected bool mNeedsHighlight {
            get {
                if (null == mMouseElm) {
                    return IsSelected;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScopeElm = (mMouseElm is Scope) && ((Scope)mMouseElm).Properties.UI.Equals(this);
                return mMouseElm.Equals(this) || IsSelected || isScopeElm;
            }
        }

        protected virtual int mNumHandles { get { return 2; } }

        protected virtual BaseLink mLink { get; set; } = new BaseLink();
        #endregion

        #region [protected variable]
        protected PointF mLead1;
        protected PointF mLead2;
        protected Point mNamePos;
        protected Point mValuePos;
        protected int mFlags;
        protected double mCurCount;
        protected bool mNoDiagonal;
        #endregion

        #region [public method]
        /// <summary>
        /// dump component state for export/undo
        /// </summary>
        public string Dump() {
            var valueList = new List<object>();
            valueList.Add(DumpType);
            Post.Dump(valueList);
            valueList.Add(mFlags);
            dump(valueList);
            mLink.Dump(valueList);
            if (!string.IsNullOrWhiteSpace(ReferenceName)) {
                valueList.Add(Utils.Escape(ReferenceName));
            }
            return string.Join(" ", valueList.ToArray());
        }

        /// <summary>
        /// this is used to set the position of an internal element so we can draw it inside the parent
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="bx"></param>
        /// <param name="by"></param>
        public void SetPosition(int ax, int ay, int bx, int by) {
            Post.SetPosition(ax, ay, bx, by);
            SetPoints();
        }

        public void Move(int dx, int dy) {
            Post.Move(dx, dy);
            SetPoints();
        }

        public void Move(int dx, int dy, int n) {
            Post.Move(dx, dy, n);
            SetPoints();
        }

        /// <summary>
        /// determine if moving this element by (dx,dy) will put it on top of another element
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool AllowMove(int dx, int dy) {
            int nx = Post.A.X + dx;
            int ny = Post.A.Y + dy;
            int nx2 = Post.B.X + dx;
            int ny2 = Post.B.Y + dy;
            for (int i = 0; i != CirSimForm.UICount; i++) {
                var ce = CirSimForm.GetUI(i);
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

        public void SetMouseElm(bool v) {
            if (v) {
                mMouseElm = this;
            } else if (mMouseElm == this) {
                mMouseElm = null;
            }
        }

        public void SelectRect(RectangleF r) {
            IsSelected = r.IntersectsWith(Post.BoundingBox);
        }

        public void DrawHandles(CustomGraphics g) {
            g.DrawHandle(Post.A);
            if (2 <= mNumHandles) {
                g.DrawHandle(Post.B);
            }
        }

        public string GetPostVoltage(int n) {
            if (n < Elm.Volts.Length) {
                return Utils.UnitText(Elm.Volts[n], "V");
            } else {
                return "";
            }
        }
        #endregion

        #region [public virtual method]
        public virtual double Distance(int x, int y) {
            return Post.Distance(x, y);
        }

        public virtual void Delete() {
            if (mMouseElm == this) {
                mMouseElm = null;
            }
            CirSimForm.DeleteSliders(this);
        }

        public virtual void Draw(CustomGraphics g) { }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="pos"></param>
        public virtual void Drag(Point pos) {
            Post.Drag(pos, mNoDiagonal);
            SetPoints();
        }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void SetPoints() {
            Post.SetValue();
            Elm.Post[0] = Post.A;
            Elm.Post[1] = Post.B;
        }

        /// <summary>
        /// get component info for display in lower right
        /// </summary>
        /// <param name="arr"></param>
        public virtual void GetInfo(string[] arr) { }

        public virtual ElementInfo GetElementInfo(int r, int c) { return null; }

        public virtual void SetElementValue(int r, int c, ElementInfo ei) { }

        public virtual EventHandler CreateSlider(ElementInfo ei, Adjustable adj) { return null; }
        #endregion

        #region [protected method]
        protected virtual void dump(List<object> optionList) { }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void calcLeads(int bodyLength) {
            if (Post.Len < bodyLength || bodyLength == 0) {
                mLead1 = Elm.Post[0];
                mLead2 = Elm.Post[1];
                return;
            }
            setLead1((Post.Len - bodyLength) / (2 * Post.Len));
            setLead2((Post.Len + bodyLength) / (2 * Post.Len));
        }

        /// <summary>
        /// update and draw current for simple two-terminal element
        /// </summary>
        protected void doDots() {
            updateDotCount();
            if (CirSimForm.DragElm != this) {
                drawCurrent(Elm.Post[0], Elm.Post[1], mCurCount);
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
            updateDotCount(Elm.Current, ref mCurCount);
        }

        protected void getBasicInfo(int begin, params string[] arr) {
            arr[begin] = "電流：" + Utils.CurrentAbsText(Elm.Current);
            arr[begin + 1] = "電位差：" + Utils.VoltageAbsText(Elm.GetVoltageDiff());
        }

        protected void setLead1(double w) {
            interpPost(ref mLead1, w);
        }

        protected void setLead2(double w) {
            interpPost(ref mLead2, w);
        }

        protected void interpPost(ref PointF p, double f) {
            p.X = (float)(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f);
            p.Y = (float)(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f);
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

        protected void interpPost(ref PointF p, double f, double g) {
            var gx = Elm.Post[1].Y - Elm.Post[0].Y;
            var gy = Elm.Post[0].X - Elm.Post[1].X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = Elm.Post[0].X;
                p.Y = Elm.Post[0].Y;
            } else {
                g /= r;
                p.X = (float)(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f + g * gx);
                p.Y = (float)(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f + g * gy);
            }
        }

        protected void interpPostAB(ref PointF a, ref PointF b, double f, double g) {
            var gx = Elm.Post[1].Y - Elm.Post[0].Y;
            var gy = Elm.Post[0].X - Elm.Post[1].X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                a = Elm.Post[0];
                b = Elm.Post[1];
            } else {
                g /= r;
                a.X = (float)(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f + g * gx);
                a.Y = (float)(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f + g * gy);
                b.X = (float)(Elm.Post[0].X * (1 - f) + Elm.Post[1].X * f - g * gx);
                b.Y = (float)(Elm.Post[0].Y * (1 - f) + Elm.Post[1].Y * f - g * gy);
            }
        }

        protected void interpLead(ref PointF p, double f) {
            p.X = (float)Math.Floor(mLead1.X * (1 - f) + mLead2.X * f);
            p.Y = (float)Math.Floor(mLead1.Y * (1 - f) + mLead2.Y * f);
        }

        protected void interpLead(ref PointF p, double f, double g) {
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

        protected void interpLeadAB(ref PointF a, ref PointF b, double f, double g) {
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

        protected void setLinkedValues<T>(int linkID, double value) {
            mLink.SetValue(Elm, linkID, value);
            if (mLink.GetGroup(linkID) == 0) {
                return;
            }
            for (int i = 0; i != CirSimForm.UICount; i++) {
                var u2 = CirSimForm.GetUI(i);
                if (u2 is T) {
                    if (u2.mLink.GetGroup(linkID) == mLink.GetGroup(linkID)) {
                        u2.mLink.SetValue(u2.Elm, linkID, value);
                    }
                }
            }
        }
        #endregion

        #region [draw method]
        protected void drawLine(PointF a, PointF b) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(a.X, a.Y, b.X, b.Y);
        }

        protected void drawLine(float ax, float ay, float bx, float by) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(ax, ay, bx, by);
        }

        protected void drawDashRectangle(float x, float y, float w, float h) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawDashRectangle(x, y, w, h);
        }

        protected void drawCircle(PointF p, float radius) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawCircle(p, radius);
        }

        protected void drawPolygon(PointF[] p) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawPolygon(p);
        }

        protected void drawPolyline(PointF[] p) {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawPolyline(p);
        }

        protected void fillPolygon(PointF[] p) {
            var color = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.FillPolygon(color, p);
        }

        protected void drawPosts() {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (CirSimForm.DragElm == null && !mNeedsHighlight) {
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
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(Elm.Post[0], mLead1);
        }

        protected void drawLeadB() {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(mLead2, Elm.Post[1]);
        }

        protected void draw2Leads() {
            Context.DrawColor = mNeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
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
        protected void drawCurrent(PointF a, PointF b, double pos) {
            drawCurrent(a.X, a.Y, b.X, b.Y, pos);
        }

        protected void drawCurrent(float ax, float ay, float bx, float by, double pos) {
            if ((!CirSimForm.IsRunning) || pos == 0 || !ControlPanel.ChkShowCurrent.Checked) {
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

        protected void drawCenteredText(string s, PointF p, bool cx) {
            var fs = Context.GetTextSize(s);
            var w = fs.Width;
            var h2 = fs.Height / 2;
            if (cx) {
                Post.AdjustBbox(
                    (int)(p.X - w / 2), (int)(p.Y - h2),
                    (int)(p.X + w / 2), (int)(p.Y + h2)
                );
            } else {
                Post.AdjustBbox(
                    (int)p.X, (int)(p.Y - h2),
                    (int)(p.X + w), (int)(p.Y + h2)
                );
            }
            Context.DrawCenteredText(s, p);
        }

        protected void drawCenteredLText(string s, PointF p, bool cx) {
            var fs = Context.GetTextSizeL(s);
            var w = fs.Width;
            var h2 = fs.Height / 2;
            if (cx) {
                Post.AdjustBbox(
                    (int)(p.X - w / 2), (int)(p.Y - h2),
                    (int)(p.X + w / 2), (int)(p.Y + h2)
                );
            } else {
                Post.AdjustBbox(
                    (int)p.X, (int)(p.Y - h2),
                    (int)(p.X + w), (int)(p.Y + h2)
                );
            }
            Context.DrawCenteredLText(s, p);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="s"></param>
        protected void drawValues(string s, int offsetX, int offsetY) {
            if (s == null) {
                return;
            }
            var textSize = Context.GetTextSize(s);
            int xc, yc;
            if (this is Rail || this is Sweep) {
                xc = Post.B.X;
                yc = Post.B.Y;
            } else {
                xc = (Post.B.X + Post.A.X) / 2;
                yc = (Post.B.Y + Post.A.Y) / 2;
            }
            Context.DrawRightText(s, xc + offsetX, (int)(yc - textSize.Height + offsetY));
        }

        protected void drawValue(double value) {
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.UnitText(value);
                if (Post.Horizontal) {
                    Context.DrawCenteredText(s, mValuePos);
                } else if (Post.Vertical) {
                    Context.DrawCenteredVText(s, mValuePos);
                } else {
                    Context.DrawLeftText(s, mValuePos.X, mValuePos.Y);
                }
            }
        }

        /// <summary>
        /// draw component name
        /// </summary>
        /// <param name="s"></param>
        protected void drawName(string s, int offsetX, int offsetY) {
            if (s == null) {
                return;
            }
            var textSize = Context.GetTextSize(s);
            int xc, yc;
            if (this is Rail) {
                xc = Post.B.X;
                yc = Post.B.Y;
            } else {
                xc = (Post.B.X + Post.A.X) / 2;
                yc = (Post.B.Y + Post.A.Y) / 2;
            }
            Context.DrawLeftText(s, xc + offsetX, (int)(yc - textSize.Height + offsetY));
        }

        protected void drawName() {
            if (ControlPanel.ChkShowName.Checked) {
                if (Post.Horizontal) {
                    Context.DrawCenteredText(ReferenceName, mNamePos);
                } else if (Post.Vertical) {
                    Context.DrawCenteredVText(ReferenceName, mNamePos);
                } else {
                    Context.DrawRightText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }
        #endregion
    }
}
