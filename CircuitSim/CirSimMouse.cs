using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.UI;

namespace Circuit {
    public static class CirSimMouse {
        public enum MODE {
            NONE = 0,
            ADD_ELM,
            SPLIT,
            SCROLL,
            SELECT,
            SELECT_AREA,
            DRAG_ITEM,
            DRAG_POST,
            DRAG_ROW,
            DRAG_COLUMN
        }

        public static BaseUI GrippedElm = null;

        public static MODE Mode = MODE.NONE;
        public static MouseButtons Button = MouseButtons.None;
        public static EPOST HoveringPost = EPOST.INVALID;
        public static EPOST DraggingPost = EPOST.INVALID;
        public static DateTime LastMove = DateTime.Now;
        public static bool IsDragging = false;

        public static Point Cursor;
        public static Point DragBegin;
        public static Point DragEnd;
        public static Point Offset;
        public static Rectangle SelectedArea;

        static Point mLastCursor;

        public static Point CommitCursor() {
            mLastCursor = Cursor;
            return mLastCursor;
        }
        public static Point GetAbsPos() {
            return new Point(Cursor.X - Offset.X, Cursor.Y - Offset.Y);
        }
        public static void SetCursor(Point p) {
            Cursor = p;
        }
        public static void SelectArea(Point p) {
            int x1 = Math.Min(p.X, DragBegin.X);
            int x2 = Math.Max(p.X, DragBegin.X);
            int y1 = Math.Min(p.Y, DragBegin.Y);
            int y2 = Math.Max(p.Y, DragBegin.Y);
            SelectedArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }
        public static void GripElm(BaseUI ui) {
            if (ui == GrippedElm) {
                return;
            }
            if (GrippedElm != null) {
                GrippedElm.SetMouseElm(false);
            }
            if (ui != null) {
                ui.SetMouseElm(true);
            }
            GrippedElm = ui;
        }
        public static Point ToScreenPos(Point pos) {
            return new Point(pos.X + Offset.X, pos.Y + Offset.Y);
        }
        public static Point ToAbsPos(Point pos) {
            return new Point(pos.X - Offset.X, pos.Y - Offset.Y);
        }
        public static Point ToAbsPos(int x, int y) {
            return new Point(x - Offset.X, y - Offset.Y);
        }
        public static void MoveGrippedElm(Point pos) {
            var dx = pos.X - DragEnd.X;
            var dy = pos.Y - DragEnd.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            GrippedElm.Move(dx, dy, DraggingPost);
        }
        public static void Scroll() {
            int dx = Cursor.X - mLastCursor.X;
            int dy = Cursor.Y - mLastCursor.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            Offset.X += dx;
            Offset.Y += dy;
            mLastCursor = Cursor;
        }
        public static void Centering(int width, int height, Rectangle bounds) {
            if (0 < bounds.Width) {
                Offset.X = (width - bounds.Width) / 2 - bounds.X;
            } else {
                Offset.X = 0;
            }
            if (0 < bounds.Height) {
                Offset.Y = (height - bounds.Height) / 2 - bounds.Y;
            } else {
                Offset.Y = 0;
            }
        }
    }
}
