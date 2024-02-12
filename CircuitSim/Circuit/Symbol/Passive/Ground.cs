using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
    class Ground : BaseSymbol {
        const int BODY_LEN = 8;

        PointF[][] mLine;
        ElmGround mElm;

        public override BaseElement Element { get { return mElm; } }

        public Ground(Point pos) : base(pos) {
            mElm = new ElmGround();
            Post.B.Y = pos.Y + CirSimForm.GRID_SIZE;
            SetPoints();
        }

        public Ground(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mElm = new ElmGround();
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.GROUND; } }

        public override void SetPoints() {
            base.SetPoints();
            var pa = new PointF();
            var pb = new PointF();
            mLine = new PointF[4][];
            for (int i = 0; i != 3; i++) {
                var a = i * 3 + 2;
                var b = i * BODY_LEN * 0.5;
                InterpolationPostAB(ref pa, ref pb, 1 - b / Post.Len, a);
                mLine[i] = new PointF[] { pa, pb };
            }
            InterpolationPost(ref pb, 1 - BODY_LEN / Post.Len);
            mLine[3] = new PointF[] { pb, Post.A };
        }

        public override void Draw(CustomGraphics g) {
            foreach (var p in mLine) {
                DrawLine(p[0], p[1]);
            }
            DoDots();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "接地";
            arr[1] = "電流：" + Utils.CurrentText(mElm.Current);
        }
    }
}
