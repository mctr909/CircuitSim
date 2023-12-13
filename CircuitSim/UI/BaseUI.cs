using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.UI.Output;

namespace Circuit.UI {
    abstract class BaseUI : IUI {
        static IUI mMouseElm = null;

        protected BaseUI(Point pos) {
            Post = new Post(pos);
            mFlags = 0;
        }

        protected BaseUI(Point p1, Point p2, int f) {
            Post = new Post(p1, p2);
            mFlags = f;
        }

        #region [property]
        public abstract DUMP_ID DumpId { get; }

        public string ReferenceName { get; set; }

        public BaseElement Elm { get; set; }

        public Post Post { get; set; }

        public bool IsSelected { get; set; }

        public bool IsMouseElm {
            get {
                if (null == mMouseElm) {
                    return false;
                }
                return mMouseElm.Equals(this);
            }
        }

        public bool NeedsHighlight {
            get {
                if (null == mMouseElm) {
                    return IsSelected;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScope = mMouseElm is Scope;
                if (isScope) {
                    var sc = (Scope)mMouseElm;
                    var ui = sc.Plot.GetUI();
                    isScope = null != ui && ui.Equals(this);
                }
                return mMouseElm.Equals(this) || IsSelected || isScope;
            }
        }

        public virtual bool IsCreationFailed { get { return Post.IsCreationFailed; } }

        public virtual bool CanViewInScope { get { return Elm.TermCount <= 2; } }
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
            return Utils.Distance(Post.A, p);
        }
        public double DistancePostB(Point p) {
            return Utils.Distance(Post.B, p);
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
        #endregion

        #region [public virtual method]
        public virtual double Distance(Point p) {
            return Utils.DistanceOnLine(Post.A, Post.B, p);
        }
        public virtual void Delete() {
            if (mMouseElm == this) {
                mMouseElm = null;
            }
            CirSimForm.DeleteSliders(this);
        }
        public virtual void Draw(CustomGraphics g) { }
        public virtual void Drag(Point pos) {
            Post.Drag(CirSimForm.SnapGrid(pos));
            SetPoints();
        }
        public virtual void SelectRect(RectangleF r) {
            IsSelected = r.IntersectsWith(Post.GetRect());
        }
        public virtual void SetPoints() {
            Post.SetValue();
            Elm.SetNodePos(Post.A, Post.B);
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
        protected void doDots() {
            updateDotCount();
            if (CirSimForm.ConstructElm != this) {
                drawCurrent(Post.A, Post.B, mCurCount);
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
            arr[begin + 1] = "電位差：" + Utils.VoltageAbsText(Elm.VoltageDiff);
        }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void setLeads(int bodyLength) {
            if (Post.Len < bodyLength || bodyLength == 0) {
                mLead1 = Post.A;
                mLead2 = Post.B;
                return;
            }
            setLead1((Post.Len - bodyLength) / (2 * Post.Len));
            setLead2((Post.Len + bodyLength) / (2 * Post.Len));
        }

        protected void setLead1(double w) {
            interpPost(ref mLead1, w);
        }

        protected void setLead2(double w) {
            interpPost(ref mLead2, w);
        }

        protected void interpPost(ref PointF p, double f) {
            p.X = (float)(Post.A.X * (1 - f) + Post.B.X * f);
            p.Y = (float)(Post.A.Y * (1 - f) + Post.B.Y * f);
        }

        protected void interpPost(ref Point p, double f, double g) {
            var gx = Post.B.Y - Post.A.Y;
            var gy = Post.A.X - Post.B.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = Post.A.X;
                p.Y = Post.A.Y;
            } else {
                g /= r;
                p.X = (int)Math.Floor(Post.A.X * (1 - f) + Post.B.X * f + g * gx + 0.5);
                p.Y = (int)Math.Floor(Post.A.Y * (1 - f) + Post.B.Y * f + g * gy + 0.5);
            }
        }

        protected void interpPost(ref PointF p, double f, double g) {
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

        protected void interpPostAB(ref PointF a, ref PointF b, double f, double g) {
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
                var u2 = (BaseUI)CirSimForm.GetUI(i);
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
            CustomGraphics.Instance.DrawLine(a.X, a.Y, b.X, b.Y);
        }
        protected void drawLine(float ax, float ay, float bx, float by) {
            CustomGraphics.Instance.DrawLine(ax, ay, bx, by);
        }
        protected void drawDashRectangle(float x, float y, float w, float h) {
            CustomGraphics.Instance.DrawDashRectangle(x, y, w, h);
        }
        protected void drawCircle(PointF p, float radius) {
            CustomGraphics.Instance.DrawCircle(p, radius);
        }
        protected void drawArc(PointF p, float diameter, float start, float sweep) {
            CustomGraphics.Instance.DrawArc(p, diameter, start, sweep);
        }
        protected void drawPolygon(PointF[] p) {
            CustomGraphics.Instance.DrawPolygon(p);
        }
        protected void drawPolyline(PointF[] p) {
            CustomGraphics.Instance.DrawPolyline(p);
        }
        protected void fillCircle(PointF p, float radius) {
            CustomGraphics.Instance.FillCircle(p.X, p.Y, radius);
        }
        protected void fillPolygon(PointF[] p) {
            CustomGraphics.Instance.FillPolygon(p);
        }
        protected void drawLeadA() {
            CustomGraphics.Instance.DrawLine(Post.A, mLead1);
        }
        protected void drawLeadB() {
            CustomGraphics.Instance.DrawLine(mLead2, Post.B);
        }
        protected void draw2Leads() {
            var g = CustomGraphics.Instance;
            g.DrawLine(Post.A, mLead1);
            g.DrawLine(mLead2, Post.B);
        }
        protected void drawCurrent(PointF a, PointF b, double pos) {
            drawCurrent(a.X, a.Y, b.X, b.Y, pos);
        }
        protected void drawCurrent(float ax, float ay, float bx, float by, double pos) {
            if ((!CirSimForm.IsRunning) || ControlPanel.ChkPrintable.Checked || !ControlPanel.ChkShowCurrent.Checked) {
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
                CustomGraphics.Instance.DrawCurrent(x0, y0, 0.5f);
            }
        }
        protected void drawCurrentA(double pos) {
            drawCurrent(Post.A, mLead1, pos);
        }
        protected void drawCurrentB(double pos) {
            drawCurrent(mLead2, Post.B, pos);
        }
        protected void drawLeftText(string text, float x, float y) {
            CustomGraphics.Instance.DrawLeftText(text, x, y);
        }
        protected void drawCenteredText(string text, PointF centerPos, double rotateAngle = 0) {
            CustomGraphics.Instance.DrawCenteredText(text, centerPos, rotateAngle);
        }
        protected void drawCenteredLText(string s, PointF p, bool cx) {
            CustomGraphics.Instance.DrawCenteredLText(s, p);
        }
        protected void drawValues(string s, int offsetX, int offsetY) {
            if (s == null) {
                return;
            }
            var g = CustomGraphics.Instance;
            var textSize = g.GetTextSize(s);
            var xc = Post.B.X;
            var yc = Post.B.Y;
            g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }
        protected void drawValue(string s) {
            if (ControlPanel.ChkShowValues.Checked) {
                drawCenteredText(s, mValuePos, mTextRot);
            }
        }
        protected void drawName() {
            if (ControlPanel.ChkShowName.Checked) {
                drawCenteredText(ReferenceName, mNamePos, mTextRot);
            }
        }
        #endregion
    }
}
