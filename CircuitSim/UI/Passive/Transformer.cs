using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Transformer : BaseUI {
        public const int FLAG_REVERSE = 4;

        const int BODY_LEN = 24;

        PointF mTermPri1;
        PointF mTermPri2;
        PointF mTermSec1;
        PointF mTermSec2;
        PointF mCoilPri1;
        PointF mCoilSec1;
        PointF mCoilPri2;
        PointF mCoilSec2;

        PointF[] mCore;
        PointF[] mDots;
        PointF[] mCoilPri;
        PointF[] mCoilSec;
        float mCoilWidth;
        float mCoilAngle;

        public Transformer(Point pos) : base(pos) {
            Elm = new ElmTransformer();
            Elm.AllocNodes();
            Post.NoDiagonal = true;
            ReferenceName = "T";
        }

        public Transformer(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmTransformer();
            Elm = elm;
            elm.PInductance = st.nextTokenDouble(1e3);
            elm.Ratio = st.nextTokenDouble(1);
            elm.Currents[0] = st.nextTokenDouble(0);
            elm.Currents[1] = st.nextTokenDouble(0);
            elm.CouplingCoef = st.nextTokenDouble(0.999);
            elm.AllocNodes();
            Post.NoDiagonal = true;
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.TRANSFORMER; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmTransformer)Elm;
            optionList.Add(ce.PInductance.ToString("g3"));
            optionList.Add(ce.Ratio);
            optionList.Add(ce.Currents[0].ToString("g3"));
            optionList.Add(ce.Currents[1].ToString("g3"));
            optionList.Add(ce.CouplingCoef);
        }

        public override void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            Post.B = pos;
            SetPoints();
        }

        public override void SetPoints() {
            var ce = (ElmTransformer)Elm;
            if (Post.B.Y < Post.A.Y + BODY_LEN) {
                Post.B.Y = Post.A.Y + BODY_LEN;
            }
            if (Post.B.X < Post.A.X + BODY_LEN) {
                Post.B.X = Post.A.X + BODY_LEN;
            }
            var width = Math.Max(BODY_LEN, Math.Abs(Post.B.X - Post.A.X));
            var height = Math.Max(BODY_LEN, Math.Abs(Post.B.Y - Post.A.Y));
            base.SetPoints();

            mTermPri1 = Post.A;
            mTermSec1 = Post.B;
            mTermSec1.Y = mTermPri1.Y;

            Utils.InterpPoint(mTermPri1, mTermSec1, ref mTermPri2, 0, -Post.Dsign * height);
            Utils.InterpPoint(mTermPri1, mTermSec1, ref mTermSec2, 1, -Post.Dsign * height);

            var pce = 0.5 - 10.0 / width;
            Utils.InterpPoint(mTermPri1, mTermSec1, ref mCoilPri1, pce);
            Utils.InterpPoint(mTermPri1, mTermSec1, ref mCoilSec1, 1 - pce);
            Utils.InterpPoint(mTermPri2, mTermSec2, ref mCoilPri2, pce);
            Utils.InterpPoint(mTermPri2, mTermSec2, ref mCoilSec2, 1 - pce);

            var pcd = 0.5 - 1.0 / width;
            mCore = new PointF[4];
            Utils.InterpPoint(mTermPri1, mTermSec1, ref mCore[0], pcd);
            Utils.InterpPoint(mTermPri1, mTermSec1, ref mCore[1], 1 - pcd);
            Utils.InterpPoint(mTermPri2, mTermSec2, ref mCore[2], pcd);
            Utils.InterpPoint(mTermPri2, mTermSec2, ref mCore[3], 1 - pcd);

            if (-1 == ce.Polarity) {
                mDots = new PointF[2];
                var dotp = Math.Abs(7.0 / height);
                Utils.InterpPoint(mCoilPri1, mCoilPri2, ref mDots[0], dotp, -7 * Post.Dsign);
                Utils.InterpPoint(mCoilSec2, mCoilSec1, ref mDots[1], dotp, -7 * Post.Dsign);
                var x = mTermSec1;
                mTermSec1 = mTermSec2;
                mTermSec2 = x;
                var t = mCoilSec1;
                mCoilSec1 = mCoilSec2;
                mCoilSec2 = t;
            } else {
                mDots = null;
            }

            setCoilPos(mCoilPri1, mCoilPri2, 90 * Post.Dsign, out mCoilPri);
            setCoilPos(mCoilSec1, mCoilSec2, -90 * Post.Dsign * ce.Polarity, out mCoilSec);
            setNamePos();

            ce.SetNodePos(mTermPri1, mTermSec1, mTermPri2, mTermSec2);
        }

        public override double Distance(Point p) {
            return Math.Min(
                Utils.DistanceOnLine(mTermPri1, mCoilPri1, p), Math.Min(
                Utils.DistanceOnLine(mTermSec1, mCoilSec1, p), Math.Min(
                Utils.DistanceOnLine(mTermPri2, mCoilPri2, p), Math.Min(
                Utils.DistanceOnLine(mTermSec2, mCoilSec2, p), Math.Min(
                Utils.DistanceOnLine(mCoilPri1, mCoilSec2, p),
                Utils.DistanceOnLine(mCoilSec1, mCoilPri2, p)
            )))));
        }

        void setCoilPos(PointF a, PointF b, float dir, out PointF[] pos) {
            var coilLen = (float)Utils.Distance(a, b);
            var loopCt = (int)Math.Ceiling(coilLen / 9);
            mCoilWidth = coilLen / loopCt;
            if (Utils.Angle(a, b) < 0) {
                mCoilAngle = -dir;
            } else {
                mCoilAngle = dir;
            }
            var arr = new List<PointF>();
            for (int loop = 0; loop != loopCt; loop++) {
                var p = new PointF();
                Utils.InterpPoint(a, b, ref p, (loop + 0.5) / loopCt, 0);
                arr.Add(p);
            }
            pos = arr.ToArray();
        }

        void setNamePos() {
            _NamePos = new Point((int)mCore[0].X + 1, (int)mCore[0].Y - 8);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmTransformer)Elm;

            drawLine(mTermPri1, mCoilPri1);
            drawLine(mTermSec1, mCoilSec1);
            drawLine(mTermPri2, mCoilPri2);
            drawLine(mTermSec2, mCoilSec2);

            foreach (var p in mCoilPri) {
                drawArc(p, mCoilWidth, mCoilAngle, 180);
            }
            foreach (var p in mCoilSec) {
                drawArc(p, mCoilWidth, mCoilAngle, -180);
            }

            drawLine(mCore[0], mCore[2]);
            drawLine(mCore[1], mCore[3]);

            if (mDots != null) {
                drawCircle(mDots[0], 2.5f);
                drawCircle(mDots[1], 2.5f);
            }

            updateDotCount(ce.Currents[0], ref ce.CurCounts[0]);
            updateDotCount(ce.Currents[1], ref ce.CurCounts[1]);
            drawCurrent(mTermPri1, mCoilPri1, ce.CurCounts[0]);
            drawCurrent(mCoilPri1, mCoilPri2, ce.CurCounts[0]);
            drawCurrent(mCoilPri2, mTermPri2, ce.CurCounts[0]);
            drawCurrent(mTermSec1, mCoilSec1, ce.CurCounts[1]);
            drawCurrent(mCoilSec1, mCoilSec2, ce.CurCounts[1]);
            drawCurrent(mCoilSec2, mTermSec2, ce.CurCounts[1]);

            if (ControlPanel.ChkShowName.Checked) {
                drawCenteredText(ReferenceName, _NamePos);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmTransformer)Elm;
            arr[0] = "トランス：" + Utils.UnitText(ce.PInductance, "H");
            arr[1] = "2次側巻数比：" + ce.Ratio;
            arr[2] = "電位差(1次)：" + Utils.VoltageText(ce.Volts[ElmTransformer.PRI_T] - ce.Volts[ElmTransformer.PRI_B]);
            arr[3] = "電位差(2次)：" + Utils.VoltageText(ce.Volts[ElmTransformer.SEC_T] - ce.Volts[ElmTransformer.SEC_B]);
            arr[4] = "電流(1次)：" + Utils.CurrentText(ce.Currents[0]);
            arr[5] = "電流(2次)：" + Utils.CurrentText(ce.Currents[1]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmTransformer)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("一次側インダクタンス(H)", ce.PInductance);
            }
            if (r == 1) {
                return new ElementInfo("二次側巻数比", ce.Ratio);
            }
            if (r == 2) {
                return new ElementInfo("結合係数(0～1)", ce.CouplingCoef);
            }
            if (r == 3) {
                return new ElementInfo("名前", ReferenceName);
            }
            if (r == 4) {
                return new ElementInfo("極性反転", ce.Polarity == -1);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmTransformer)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.PInductance = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ce.Ratio = ei.Value;
            }
            if (n == 2 && ei.Value > 0 && ei.Value < 1) {
                ce.CouplingCoef = ei.Value;
            }
            if (n == 3) {
                ReferenceName = ei.Text;
                setNamePos();
            }
            if (n == 4) {
                ce.Polarity = ei.CheckBox.Checked ? -1 : 1;
                if (ei.CheckBox.Checked) {
                    _Flags |= FLAG_REVERSE;
                } else {
                    _Flags &= ~FLAG_REVERSE;
                }
                SetPoints();
            }
        }
    }
}
