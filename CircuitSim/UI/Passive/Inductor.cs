using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Inductor : BaseUI {
        protected static string mLastReferenceName = "L";

        const int BODY_LEN = 24;
        const int COIL_WIDTH = 8;

        PointF[] mCoilPos;
        float mCoilAngle;

        public Inductor(Point pos) : base(pos) {
            Elm = new ElmInductor();
            ReferenceName = mLastReferenceName;
        }

        public Inductor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var ind = st.nextTokenDouble(1e-4);
            var c = st.nextTokenDouble(0);
            Elm = new ElmInductor(ind, c);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.INDUCTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmInductor)Elm;
            optionList.Add(ce.Inductance.ToString("g3"));
            optionList.Add(ce.Current.ToString("g3"));
        }

        public override void SetPoints() {
            base.SetPoints();
            setLeads(BODY_LEN);
            setCoilPos(_Lead1, _Lead2);
            setTextPos();
        }

        void setCoilPos(PointF a, PointF b) {
            var coilLen = (float)Utils.Distance(a, b);
            var loopCt = (int)Math.Ceiling(coilLen / 11);
            var arr = new List<PointF>();
            for (int loop = 0; loop != loopCt; loop++) {
                var p = new PointF();
                Utils.InterpPoint(a, b, ref p, (loop + 0.5) / loopCt, 0);
                arr.Add(p);
            }
            mCoilPos = arr.ToArray();
            mCoilAngle = (float)(Utils.Angle(a, b) * 180 / Math.PI);
        }

        void setTextPos() {
            var abX = Post.B.X - Post.A.X;
            var abY = Post.B.Y - Post.A.Y;
            _TextRot = Math.Atan2(abY, abX);
            var deg = -_TextRot * 180 / Math.PI;
            if (deg < 0.0) {
                deg += 360;
            }
            if (45 * 3 <= deg && deg < 45 * 7) {
                _TextRot += Math.PI;
            }
            if (0 < deg && deg < 45 * 3) {
                interpPost(ref _ValuePos, 0.5, 9 * Post.Dsign);
                interpPost(ref _NamePos, 0.5, -9 * Post.Dsign);
            } else if (45 * 3 <= deg && deg <= 180) {
                interpPost(ref _NamePos, 0.5, 7 * Post.Dsign);
                interpPost(ref _ValuePos, 0.5, -13 * Post.Dsign);
            } else if (180 < deg && deg < 45 * 7) {
                interpPost(ref _NamePos, 0.5, -7 * Post.Dsign);
                interpPost(ref _ValuePos, 0.5, 13 * Post.Dsign);
            } else {
                interpPost(ref _NamePos, 0.5, 11 * Post.Dsign);
                interpPost(ref _ValuePos, 0.5, -9 * Post.Dsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmInductor)Elm;
            draw2Leads();
            foreach(var p in mCoilPos) {
                drawArc(p, COIL_WIDTH, mCoilAngle, -180);
            }
            drawName();
            drawValue(Utils.UnitText(ce.Inductance));
            doDots();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmInductor)Elm;
            if (string.IsNullOrEmpty(ReferenceName)) {
                arr[0] = "コイル：" + Utils.UnitText(ce.Inductance, "H");
                getBasicInfo(1, arr);
            } else {
                arr[0] = ReferenceName;
                arr[1] = "コイル：" + Utils.UnitText(ce.Inductance, "H");
                getBasicInfo(2, arr);
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmInductor)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("インダクタンス(H)", ce.Inductance);
            }
            if (r == 1) {
                return new ElementInfo("名前", ReferenceName);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmInductor)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Inductance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                ReferenceName = ei.Text;
                mLastReferenceName = ReferenceName;
                setTextPos();
            }
            ce.Setup(ce.Inductance, Elm.Current);
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var ce = (ElmInductor)Elm;
            return new EventHandler((s, e) => {
                var trb = adj.Slider;
                ce.Inductance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
