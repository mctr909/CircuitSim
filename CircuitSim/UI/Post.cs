using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.UI {
    public enum EPOST {
        A,
        B,
        BOTH,
        INVALID
    }

    public class Post {
        public Point A;
        public Point B;
        public RectangleF BoundingBox;

        public bool Vertical;
        public bool Horizontal;
        public bool NoDiagonal;
        public double Len;
        public int Dsign;
        public PointF Dir;
        public Point Diff;

        public bool IsCreationFailed {
            get { return A.X == B.X && A.Y == B.Y; }
        }

        public bool BoxIsCreationFailed {
            get { return Math.Abs(B.X - A.X) < 32 || Math.Abs(B.Y - A.Y) < 32; }
        }

        public Post(Point pos) {
            A = B = pos;
            BoundingBox = getBoundingBox();
        }

        public Post(Point p1, Point p2) {
            A = p1;
            B = p2;
            BoundingBox = getBoundingBox();
        }

        public void SetValue() {
            var sx = B.X - A.X;
            var sy = B.Y - A.Y;
            Len = Math.Sqrt(sx * sx + sy * sy);
            Diff.X = sx;
            Diff.Y = sy;
            Dsign = (Diff.Y == 0) ? Math.Sign(Diff.X) : Math.Sign(Diff.Y);
            if (Len == 0) {
                Dir.X = 0;
                Dir.Y = 0;
            } else {
                Dir.X = (float)(sy / Len);
                Dir.Y = -(float)(sx / Len);
            }
            Vertical = A.X == B.X;
            Horizontal = A.Y == B.Y;
        }

        public void Dump(List<object> valueList) {
            valueList.AddRange(new object[] { A.X, A.Y, B.X, B.Y });
        }

        public void FlipPosts() {
            var old = A;
            A = B;
            B = old;
        }

        public void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            if (NoDiagonal) {
                if (Math.Abs(A.X - pos.X) < Math.Abs(A.Y - pos.Y)) {
                    pos.X = A.X;
                } else {
                    pos.Y = A.Y;
                }
            }
            B = pos;
        }

        public void SetPosition(int ax, int ay, int bx, int by) {
            A.X = ax;
            A.Y = ay;
            B.X = bx;
            B.Y = by;
        }

        public void Move(int dx, int dy) {
            A.X += dx;
            A.Y += dy;
            B.X += dx;
            B.Y += dy;
            BoundingBox.X += dx;
            BoundingBox.Y += dy;
        }

        public void Move(int dx, int dy, EPOST n) {
            var oldA = A;
            var oldB = B;
            switch (n) {
            case EPOST.A:
                A.X += dx;
                A.Y += dy;
                break;
            case EPOST.B:
                B.X += dx;
                B.Y += dy;
                break;
            case EPOST.BOTH:
                A.X += dx;
                A.Y += dy;
                B.X += dx;
                B.Y += dy;
                break;
            }
            if (A.X == B.X && A.Y == B.Y) {
                A = oldA;
                B = oldB;
            }
        }

        public void SetBbox(PointF a, PointF b) {
            SetBbox(a.X, a.Y, b.X, b.Y);
        }

        public void SetBbox(float ax, float ay, float bx, float by) {
            if (ax > bx) { var q = ax; ax = bx; bx = q; }
            if (ay > by) { var q = ay; ay = by; by = q; }
            BoundingBox.X = ax;
            BoundingBox.Y = ay;
            BoundingBox.Width = bx - ax + 1;
            BoundingBox.Height = by - ay + 1;
        }

        public void SetBbox(PointF a, PointF b, double w) {
            SetBbox(a.X, a.Y, b.X, b.Y, w);
        }

        public void SetBbox(float ax, float ay, float bx, float by, double w) {
            SetBbox(ax, ay, bx, by);
            var dpx = (float)(Dir.X * w);
            var dpy = (float)(Dir.Y * w);
            AdjustBbox(
                ax + dpx, ay + dpy,
                ax - dpx, ay - dpy
            );
        }

        public void SetBbox(double bodyLength) {
            var dpx = (float)(Dir.X * bodyLength);
            var dpy = (float)(Dir.Y * bodyLength);
            var posX = A.X;
            var posY = A.Y;
            SetBbox(
                posX + dpx, posY + dpy,
                posX - dpx, posY - dpy
            );
            AdjustBbox(
                posX + dpx, posY + dpy,
                posX - dpx, posY - dpy
            );
        }

        public void AdjustBbox(float x1, float y1, float x2, float y2) {
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

        public void AdjustBbox(Point a, Point b) {
            AdjustBbox(a.X, a.Y, b.X, b.Y);
        }

        public double BoxDistance(RectangleF box, Point p) {
            return box.Contains(p.X, p.Y) ? 0 : Math.Min(
                Utils.DistanceOnLine(A.X, A.Y, B.X, A.Y, p.X, p.Y), Math.Min(
                Utils.DistanceOnLine(B.X, A.Y, B.X, B.Y, p.X, p.Y), Math.Min(
                Utils.DistanceOnLine(B.X, B.Y, A.X, B.Y, p.X, p.Y),
                Utils.DistanceOnLine(A.X, B.Y, A.X, A.Y, p.X, p.Y)
            )));
        }

        Rectangle getBoundingBox() {
            return new Rectangle(
                Math.Min(A.X, B.X), Math.Min(A.Y, B.Y),
                Math.Abs(B.X - A.X) + 1, Math.Abs(B.Y - A.Y) + 1
            );
        }
    }
}
