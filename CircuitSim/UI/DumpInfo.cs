using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.UI {
    public class DumpInfo {
        public int P1X;
        public int P1Y;
        public int P2X;
        public int P2Y;
        public int Flags;
        public string ReferenceName;
        public Rectangle BoundingBox;

        public bool IsCreationFailed {
            get { return P1X == P2X && P1Y == P2Y; }
        }

        public bool BoxIsCreationFailed {
            get { return Math.Abs(P2X - P1X) < 32 || Math.Abs(P2Y - P1Y) < 32; }
        }

        public DumpInfo(Point pos, int flags) {
            P1X = P2X = pos.X;
            P1Y = P2Y = pos.Y;
            Flags = flags;
            BoundingBox = getBoundingBox();
        }

        public DumpInfo(Point p1, Point p2, int flags) {
            P1X = p1.X;
            P1Y = p1.Y;
            P2X = p2.X;
            P2Y = p2.Y;
            Flags = flags;
            BoundingBox = getBoundingBox();
        }

        public void FlipPosts() {
            int oldx = P1X;
            int oldy = P1Y;
            P1X = P2X;
            P1Y = P2Y;
            P2X = oldx;
            P2Y = oldy;
        }

        public void Drag(Point pos, bool noDiagonal) {
            pos = CirSimForm.SnapGrid(pos);
            if (noDiagonal) {
                if (Math.Abs(P1X - pos.X) < Math.Abs(P1Y - pos.Y)) {
                    pos.X = P1X;
                } else {
                    pos.Y = P1Y;
                }
            }
            P2X = pos.X;
            P2Y = pos.Y;
        }

        public void SetPosition(int ax, int ay, int bx, int by) {
            P1X = ax;
            P1Y = ay;
            P2X = bx;
            P2Y = by;
        }

        public void SetP2(int x, int y) {
            P2X = x;
            P2Y = y;
        }

        public void SetP2(Point pos) {
            SetP2(pos.X, pos.Y);
        }

        public void Move(int dx, int dy) {
            P1X += dx;
            P1Y += dy;
            P2X += dx;
            P2Y += dy;
            BoundingBox.X += dx;
            BoundingBox.Y += dy;
        }

        public void MovePoint(int n, int dx, int dy) {
            /* modified by IES to prevent the user dragging points to create zero sized nodes
            /* that then render improperly */
            int oldx = P1X;
            int oldy = P1Y;
            int oldx2 = P2X;
            int oldy2 = P2Y;
            if (n == 0) {
                P1X += dx;
                P1Y += dy;
            } else {
                P2X += dx;
                P2Y += dy;
            }
            if (P1X == P2X && P1Y == P2Y) {
                P1X = oldx;
                P1Y = oldy;
                P2X = oldx2;
                P2Y = oldy2;
            }
        }

        public void SetBbox(Point a, Point b) {
            SetBbox(a.X, a.Y, b.X, b.Y);
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
            return Utils.DistanceOnLine(P1X, P1Y, P2X, P2Y, x, y);
        }

        public double BoxDistance(double x, double y) {
            return Math.Min(
                Utils.DistanceOnLine(P1X, P1Y, P2X, P1Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2X, P1Y, P2X, P2Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2X, P2Y, P1X, P2Y, x, y),
                Utils.DistanceOnLine(P1X, P2Y, P1X, P1Y, x, y)
            )));
        }

        public double BoxDistance(Rectangle box, double x, double y) {
            return box.Contains((int)x, (int)y) ? 0 : Math.Min(
                Utils.DistanceOnLine(P1X, P1Y, P2X, P1Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2X, P1Y, P2X, P2Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2X, P2Y, P1X, P2Y, x, y),
                Utils.DistanceOnLine(P1X, P2Y, P1X, P1Y, x, y)
            )));
        }

        public string GetValue(DUMP_ID type, List<object> optionList) {
            var separator = " ";
            var ret = string.Join(separator, type, P1X, P1Y, P2X, P2Y, Flags);
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
                Math.Min(P1X, P2X), Math.Min(P1Y, P2Y),
                Math.Abs(P2X - P1X) + 1, Math.Abs(P2Y - P1Y) + 1
            );
        }
    }
}
