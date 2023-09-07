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

        public RectangleF GetRect() {
            var ax = A.X;
            var ay = A.Y;
            var bx = B.X;
            var by = B.Y;
            if (bx < ax) {
                var t = ax;
                ax = bx;
                bx = t;
            }
            if (by < ay) {
                var t = ay;
                ay = by;
                by = t;
            }
            return new RectangleF(ax, ay, bx - ax + 1, by - ay + 1);
        }

        public Post(Point pos) {
            A = B = pos;
        }

        public Post(Point p1, Point p2) {
            A = p1;
            B = p2;
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

        public double BoxDistance(RectangleF box, Point p) {
            return box.Contains(p.X, p.Y) ? 0 : Math.Min(
                Utils.DistanceOnLine(A.X, A.Y, B.X, A.Y, p.X, p.Y), Math.Min(
                Utils.DistanceOnLine(B.X, A.Y, B.X, B.Y, p.X, p.Y), Math.Min(
                Utils.DistanceOnLine(B.X, B.Y, A.X, B.Y, p.X, p.Y),
                Utils.DistanceOnLine(A.X, B.Y, A.X, A.Y, p.X, p.Y)
            )));
        }
    }
}
