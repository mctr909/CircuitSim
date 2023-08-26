﻿using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Ground : BaseUI {
        const int BODY_LEN = 8;

        PointF mP1;
        PointF mP2;

        public Ground(Point pos) : base(pos) {
            Elm = new ElmGround();
        }

        public Ground(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmGround();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.GROUND; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.GROUND; } }

        public override void Draw(CustomGraphics g) {
            for (int i = 0; i != 3; i++) {
                var a = BODY_LEN - i * 3;
                var b = i * BODY_LEN * 0.5;
                interpPostAB(ref mP1, ref mP2, 1 + b / mLen, a);
                drawLine(mP1, mP2);
            }
            drawLine(Elm.Post[0], Elm.Post[1]);
            doDots();
            setBbox(Elm.Post[0].X, Elm.Post[0].X, (int)mP1.X, (int)mP1.Y, 11);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "ground";
            arr[1] = "I = " + Utils.CurrentText(Elm.Current);
        }
    }
}
