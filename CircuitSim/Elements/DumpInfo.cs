﻿using System;
using System.Drawing;

namespace Circuit.Elements {
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
            P1.X = P2.X = pos.X;
            P1.Y = P2.Y = pos.Y;
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
            int oldx = P1.X;
            int oldy = P1.Y;
            P1.X = P2.X;
            P1.Y = P2.Y;
            P2.X = oldx;
            P2.Y = oldy;
        }

        public void Drag(Point pos, bool noDiagonal) {
            pos = CirSimForm.Sim.SnapGrid(pos);
            if (noDiagonal) {
                if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                    pos.X = P1.X;
                } else {
                    pos.Y = P1.Y;
                }
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
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
        }

        public void SetBbox(Point a, Point b) {
            if (a.X > b.X) { var q = a.X; a.X = b.X; b.X = q; }
            if (a.Y > b.Y) { var q = a.Y; a.Y = b.Y; b.Y = q; }
            BoundingBox.X = a.X;
            BoundingBox.Y = a.Y;
            BoundingBox.Width = b.X - a.X + 1;
            BoundingBox.Height = b.Y - a.Y + 1;
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

        public string GetValue(DUMP_ID type, string value) {
            return string.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
                type,
                P1.X, P1.Y,
                P2.X, P2.Y,
                Flags,
                value,
                ReferenceName
            );
        }

        Rectangle getBoundingBox() {
            return new Rectangle(
                Math.Min(P1.X, P2.X), Math.Min(P1.Y, P2.Y),
                Math.Abs(P2.X - P1.X) + 1, Math.Abs(P2.Y - P1.Y) + 1
            );
        }
    }
}
