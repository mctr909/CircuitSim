﻿using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Ground : BaseUI {
        const int BODY_LEN = 8;

        PointF[][] mLine;

        public Ground(Point pos) : base(pos) {
            Elm = new ElmGround();
        }

        public Ground(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmGround();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.GROUND; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.GROUND; } }

        public override void SetPoints() {
            base.SetPoints();
            var pa = new PointF();
            var pb = new PointF();
            mLine = new PointF[4][];
            for (int i = 0; i != 3; i++) {
                var a = i * 3 + 2;
                var b = i * BODY_LEN * 0.5;
                interpPostAB(ref pa, ref pb, 1 - b / Post.Len, a);
                mLine[i] = new PointF[] { pa, pb };
            }
            interpPost(ref pb, 1 - BODY_LEN / Post.Len);
            mLine[3] = new PointF[] { pb, Elm.Post[0] };
            Post.SetBbox(Elm.Post[0], pa, 11);
        }

        public override void Draw(CustomGraphics g) {
            foreach (var p in mLine) {
                drawLine(p[0], p[1]);
            }
            doDots();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "接地";
            arr[1] = "電流：" + Utils.CurrentText(Elm.Current);
        }
    }
}
