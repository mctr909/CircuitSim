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

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmInductor)Elm;
            optionList.Add(ce.Inductance);
            optionList.Add(ce.Current.ToString("0.000000"));
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(COIL_WIDTH);
            calcLeads(BODY_LEN);
            setCoilPos(mLead1, mLead2);
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
            if (Post.Horizontal) {
                interpPost(ref mValuePos, 0.5, -11 * Post.Dsign);
                interpPost(ref mNamePos, 0.5, 10 * Post.Dsign);
            } else if (Post.Vertical) {
                interpPost(ref mValuePos, 0.5, Post.Dsign);
                interpPost(ref mNamePos, 0.5, -18 * Post.Dsign);
            } else {
                interpPost(ref mValuePos, 0.5, 8 * Post.Dsign);
                interpPost(ref mNamePos, 0.5, -8 * Post.Dsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmInductor)Elm;
            draw2Leads();
            foreach(var p in mCoilPos) {
                Context.DrawArc(p, COIL_WIDTH, mCoilAngle, -180);
            }
            drawValue(ce.Inductance);
            drawName();
            doDots();
            drawPosts();
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
