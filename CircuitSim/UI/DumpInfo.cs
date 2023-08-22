using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.UI {
    public class DumpInfo {
        public Point P1;
        public Point P2;
        public int Flags;
        public string ReferenceName;
        public Rectangle BoundingBox;

        public bool IsCreationFailed {
            get { return P1.X == P2.X && P1.Y == P2.Y; }
        }

        public bool BoxIsCreationFailed {
            get { return Math.Abs(P2.X - P1.X) < 32 || Math.Abs(P2.Y - P1.Y) < 32; }
        }

        public DumpInfo(Point pos, int flags) {
            P1 = P2 = pos;
            Flags = flags;
            BoundingBox = getBoundingBox();
        }

        public DumpInfo(Point p1, Point p2, int flags) {
            P1 = p1;
            P2 = p2;
            Flags = flags;
            BoundingBox = getBoundingBox();
        }

        public void FlipPosts() {
            var old = P1;
            P1 = P2;
            P2= old;
        }

        public void Drag(Point pos, bool noDiagonal) {
            pos = CirSimForm.SnapGrid(pos);
            if (noDiagonal) {
                if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                    pos.X = P1.X;
                } else {
                    pos.Y = P1.Y;
                }
            }
            P2 = pos;
        }

        public void SetPosition(int ax, int ay, int bx, int by) {
            P1.X = ax;
            P1.Y = ay;
            P2.X = bx;
            P2.Y = by;
        }

        public void SetP2(int x, int y) {
            P2.X = x;
            P2.Y = y;
        }

        public void SetP2(Point pos) {
            SetP2(pos.X, pos.Y);
        }

        public void Move(int dx, int dy) {
            P1.X += dx;
            P1.Y += dy;
            P2.X += dx;
            P2.Y += dy;
            BoundingBox.X += dx;
            BoundingBox.Y += dy;
        }

        public void MovePoint(int n, int dx, int dy) {
            /* modified by IES to prevent the user dragging points to create zero sized nodes
            /* that then render improperly */
            var old = P1;
            var old2 = P2;
            if (n == 0) {
                P1.X += dx;
                P1.Y += dy;
            } else {
                P2.X += dx;
                P2.Y += dy;
            }
            if (P1.X == P2.X && P1.Y == P2.Y) {
                P1 = old;
                P2 = old2;
            }
        }

        public void SetBbox(PointF a, PointF b) {
            SetBbox((int)a.X, (int)a.Y, (int)b.X, (int)b.Y);
        }

        public void SetBbox(Point a, int bx, int by) {
            SetBbox(a.X, a.Y, bx, by);
        }

        public void SetBbox(int ax, int ay, int bx, int by) {
            if (ax > bx) { var q = ax; ax = bx; bx = q; }
            if (ay > by) { var q = ay; ay = by; by = q; }
            BoundingBox.X = ax;
            BoundingBox.Y = ay;
            BoundingBox.Width = bx - ax + 1;
            BoundingBox.Height = by - ay + 1;
        }

        public void AdjustBbox(int x1, int y1, int x2, int y2) {
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

        public double Distance(double x, double y) {
            return Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P2.Y, x, y);
        }

        public double BoxDistance(double x, double y) {
            return Math.Min(
                Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P1.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P1.Y, P2.X, P2.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P2.Y, P1.X, P2.Y, x, y),
                Utils.DistanceOnLine(P1.X, P2.Y, P1.X, P1.Y, x, y)
            )));
        }

        public double BoxDistance(Rectangle box, double x, double y) {
            return box.Contains((int)x, (int)y) ? 0 : Math.Min(
                Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P1.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P1.Y, P2.X, P2.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P2.Y, P1.X, P2.Y, x, y),
                Utils.DistanceOnLine(P1.X, P2.Y, P1.X, P1.Y, x, y)
            )));
        }

        public string GetValue(DUMP_ID type, List<object> optionList) {
            var separator = " ";
            var ret = string.Join(separator, type, P1.X, P1.Y, P2.X, P2.Y, Flags);
            if (0 < optionList.Count) {
                ret += separator + string.Join(separator, optionList.ToArray());
            }
            if (!string.IsNullOrWhiteSpace(ReferenceName)) {
                ret += separator + Utils.Escape(ReferenceName);
            }
            return ret;
        }

        Rectangle getBoundingBox() {
            return new Rectangle(
                Math.Min(P1.X, P2.X), Math.Min(P1.Y, P2.Y),
                Math.Abs(P2.X - P1.X) + 1, Math.Abs(P2.Y - P1.Y) + 1
            );
        }
    }
}
