﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.Forms;
using Circuit.UI.Output;

namespace Circuit.UI {
    public abstract class BaseUI {
        public BaseElement Elm;
        public static CustomGraphics Context;

        static BaseUI _MouseElm = null;

        protected BaseUI(Point pos) {
            Post = new Post(pos);
            _Flags = 0;
        }

        protected BaseUI(Point p1, Point p2, int f) {
            Post = new Post(p1, p2);
            _Flags = f;
        }

        #region [property]
        public string ReferenceName { get; set; }

        public Post Post { get; protected set; }

        public bool IsSelected { get; set; }

        public bool IsMouseElm {
            get {
                if (null == _MouseElm) {
                    return false;
                }
                return _MouseElm.Equals(this);
            }
        }

        public abstract DUMP_ID DumpId { get; }

        /// <summary>
        /// called when an element is done being dragged out;
        /// </summary>
        /// <returns>returns true if it's zero size and should be deleted</returns>
        public virtual bool IsCreationFailed { get { return Post.IsCreationFailed; } }

        public virtual bool CanViewInScope { get { return Elm.TermCount <= 2; } }

        protected bool _NeedsHighlight {
            get {
                if (null == _MouseElm) {
                    return IsSelected;
                }
                /* Test if the current mouseElm is a ScopeElm and, if so, does it belong to this elm */
                var isScope = _MouseElm is Scope;
                if (isScope) {
                    var sc = (Scope)_MouseElm;
                    var ui = sc.Plot.GetUI();
                    isScope = null != ui && ui.Equals(this);
                }
                return _MouseElm.Equals(this) || IsSelected || isScope;
            }
        }

        protected virtual BaseLink _Link { get; set; } = new BaseLink();
        #endregion

        #region [protected variable]
        protected PointF _Lead1;
        protected PointF _Lead2;
        protected PointF _NamePos;
        protected PointF _ValuePos;
        protected int _Flags;
        protected double _CurCount;
        protected double _TextRot;
        #endregion

        #region [public method]
        public double DistancePostA(Point p) {
            return Utils.Distance(Post.A, p);
        }

        public double DistancePostB(Point p) {
            return Utils.Distance(Post.B, p);
        }

        /// <summary>
        /// dump component state for export/undo
        /// </summary>
        public string Dump() {
            var valueList = new List<object>();
            valueList.Add(DumpId);
            Post.Dump(valueList);
            valueList.Add(_Flags);
            dump(valueList);
            _Link.Dump(valueList);
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

        public void Move(int dx, int dy, EPOST n) {
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
                _MouseElm = this;
            } else if (_MouseElm == this) {
                _MouseElm = null;
            }
        }

        public void SelectRect(RectangleF r) {
            IsSelected = r.IntersectsWith(Post.BoundingBox);
        }
        #endregion

        #region [public virtual method]
        public virtual double Distance(Point p) {
            return Utils.DistanceOnLine(Post.A, Post.B, p);
        }

        public virtual void Delete() {
            if (_MouseElm == this) {
                _MouseElm = null;
            }
            CirSimForm.DeleteSliders(this);
        }

        public virtual void Draw(CustomGraphics g) { }

        /// <summary>
        /// draw second point to xx, yy
        /// </summary>
        /// <param name="pos"></param>
        public virtual void Drag(Point pos) {
            Post.Drag(pos);
            SetPoints();
        }

        /// <summary>
        /// calculate post locations and other convenience values used for drawing.
        /// Called when element is moved
        /// </summary>
        public virtual void SetPoints() {
            Post.SetValue();
            Elm.SetNodePos(Post.A, Post.B);
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
        /// update and draw current for simple two-terminal element
        /// </summary>
        protected void doDots() {
            updateDotCount();
            if (CirSimForm.ConstructElm != this) {
                drawCurrent(Post.A, Post.B, _CurCount);
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
            updateDotCount(Elm.Current, ref _CurCount);
        }

        protected void getBasicInfo(int begin, params string[] arr) {
            arr[begin] = "電流：" + Utils.CurrentAbsText(Elm.Current);
            arr[begin + 1] = "電位差：" + Utils.VoltageAbsText(Elm.GetVoltageDiff());
        }

        /// <summary>
        /// calculate lead points for an element of length len.  Handy for simple two-terminal elements.
        /// Posts are where the user connects wires; leads are ends of wire stubs drawn inside the element.
        /// </summary>
        /// <param name="len"></param>
        protected void setLeads(int bodyLength) {
            if (Post.Len < bodyLength || bodyLength == 0) {
                _Lead1 = Post.A;
                _Lead2 = Post.B;
                return;
            }
            setLead1((Post.Len - bodyLength) / (2 * Post.Len));
            setLead2((Post.Len + bodyLength) / (2 * Post.Len));
        }

        protected void setLead1(double w) {
            interpPost(ref _Lead1, w);
        }

        protected void setLead2(double w) {
            interpPost(ref _Lead2, w);
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
            p.X = (float)Math.Floor(_Lead1.X * (1 - f) + _Lead2.X * f);
            p.Y = (float)Math.Floor(_Lead1.Y * (1 - f) + _Lead2.Y * f);
        }

        protected void interpLead(ref PointF p, double f, double g) {
            var gx = _Lead2.Y - _Lead1.Y;
            var gy = _Lead1.X - _Lead2.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                p.X = _Lead1.X;
                p.Y = _Lead1.Y;
            } else {
                g /= r;
                p.X = (float)(_Lead1.X * (1 - f) + _Lead2.X * f + g * gx);
                p.Y = (float)(_Lead1.Y * (1 - f) + _Lead2.Y * f + g * gy);
            }
        }

        protected void interpLeadAB(ref PointF a, ref PointF b, double f, double g) {
            var gx = _Lead2.Y - _Lead1.Y;
            var gy = _Lead1.X - _Lead2.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                a.X = _Lead1.X;
                a.Y = _Lead1.Y;
                b.X = _Lead2.X;
                b.Y = _Lead2.Y;
            } else {
                g /= r;
                a.X = (float)(_Lead1.X * (1 - f) + _Lead2.X * f + g * gx);
                a.Y = (float)(_Lead1.Y * (1 - f) + _Lead2.Y * f + g * gy);
                b.X = (float)(_Lead1.X * (1 - f) + _Lead2.X * f - g * gx);
                b.Y = (float)(_Lead1.Y * (1 - f) + _Lead2.Y * f - g * gy);
            }
        }

        protected void setLinkedValues<T>(int linkID, double value) {
            _Link.SetValue(Elm, linkID, value);
            if (_Link.GetGroup(linkID) == 0) {
                return;
            }
            for (int i = 0; i != CirSimForm.UICount; i++) {
                var u2 = CirSimForm.GetUI(i);
                if (u2 is T) {
                    if (u2._Link.GetGroup(linkID) == _Link.GetGroup(linkID)) {
                        u2._Link.SetValue(u2.Elm, linkID, value);
                    }
                }
            }
        }
        #endregion

        #region [draw method]
        protected void drawLine(PointF a, PointF b) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(a.X, a.Y, b.X, b.Y);
        }
        protected void drawLine(float ax, float ay, float bx, float by) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(ax, ay, bx, by);
        }
        protected void drawDashRectangle(float x, float y, float w, float h) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawDashRectangle(x, y, w, h);
        }
        protected void drawCircle(PointF p, float radius) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawCircle(p, radius);
        }
        protected void drawArc(PointF p, float diameter, float start, float sweep) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawArc(p, diameter, start, sweep);
        }
        protected void drawPolygon(PointF[] p) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawPolygon(p);
        }
        protected void drawPolyline(PointF[] p) {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawPolyline(p);
        }
        protected void fillCircle(PointF p, float radius) {
            Context.FillColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.FillCircle(p.X, p.Y, radius);
        }
        protected void fillPolygon(PointF[] p) {
            Context.FillColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.FillPolygon(p);
        }
        protected void drawLeadA() {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(Post.A, _Lead1);
        }
        protected void drawLeadB() {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(_Lead2, Post.B);
        }
        protected void draw2Leads() {
            Context.DrawColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            Context.DrawLine(Post.A, _Lead1);
            Context.DrawLine(_Lead2, Post.B);
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
                Context.DrawCurrent(x0, y0, 0.5f);
            }
        }
        protected void drawCurrentA(double pos) {
            drawCurrent(Post.A, _Lead1, pos);
        }
        protected void drawCurrentB(double pos) {
            drawCurrent(_Lead2, Post.B, pos);
        }
        protected void drawLeftText(string text, float x, float y) {
            Context.FontColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.TextColor;
            Context.DrawLeftText(text, x, y);
        }
        protected void drawCenteredText(string text, PointF centerPos, double rotateAngle = 0) {
            Context.FontColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.TextColor;
            Context.DrawCenteredText(text, centerPos, rotateAngle);
        }
        protected void drawCenteredLText(string s, PointF p, bool cx) {
            Context.FontColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.TextColor;
            Context.DrawCenteredLText(s, p);
        }
        protected void drawValues(string s, int offsetX, int offsetY) {
            if (s == null) {
                return;
            }
            var textSize = Context.GetTextSize(s);
            var xc = Post.B.X;
            var yc = Post.B.Y;
            Context.FontColor = _NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.TextColor;
            Context.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }
        protected void drawValue(string s) {
            if (ControlPanel.ChkShowValues.Checked) {
                drawCenteredText(s, _ValuePos, _TextRot);
            }
        }
        protected void drawName() {
            if (ControlPanel.ChkShowName.Checked) {
                drawCenteredText(ReferenceName, _NamePos, _TextRot);
            }
        }
        #endregion
    }
}
