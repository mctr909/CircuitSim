using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Transformer : BaseUI {
        public const int FLAG_REVERSE = 4;

        const int BODY_LEN = 24;

        PointF[] mPtCoil;
        PointF[] mPtCore;
        PointF[] mDots;
        PointF[] mCoilPosA;
        PointF[] mCoilPosB;
        float mCoilWidth;
        float mCoilAngle;

        public Transformer(Point pos) : base(pos) {
            Elm = new ElmTransformer();
            Elm.AllocNodes();
            mNoDiagonal = true;
            DumpInfo.ReferenceName = "T";
        }

        public Transformer(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmTransformer();
            Elm = elm;
            try {
                elm.PInductance = st.nextTokenDouble();
                elm.Ratio = st.nextTokenDouble();
                elm.Currents[0] = st.nextTokenDouble();
                elm.Currents[1] = st.nextTokenDouble();
                elm.CouplingCoef = st.nextTokenDouble();
            } catch (Exception ex) {
                throw new Exception("Transformer load error:{0}", ex);
            }
            elm.AllocNodes();
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSFORMER; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmTransformer)Elm;
            optionList.Add(ce.PInductance);
            optionList.Add(ce.Ratio);
            optionList.Add(ce.Currents[0].ToString("0.000000"));
            optionList.Add(ce.Currents[1].ToString("0.000000"));
            optionList.Add(ce.CouplingCoef);
        }

        public override void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            DumpInfo.SetP2(pos);
            SetPoints();
        }

        public override void SetPoints() {
            var ce = (ElmTransformer)Elm;
            var width = Math.Max(BODY_LEN, Math.Abs(DumpInfo.P2.X - DumpInfo.P1.X));
            var height = Math.Max(BODY_LEN, Math.Abs(DumpInfo.P2.Y - DumpInfo.P1.Y));
            if (DumpInfo.P2.X == DumpInfo.P1.X) {
                DumpInfo.SetP2(DumpInfo.P2.X, DumpInfo.P1.Y);
            }
            base.SetPoints();
            ce.Post[1].Y = ce.Post[0].Y;
            mPtCoil = new PointF[4];
            mPtCore = new PointF[4];
            interpPost(ref ce.Post[2], 0, -mDsign * height);
            interpPost(ref ce.Post[3], 1, -mDsign * height);
            var pce = 0.5 - 10.0 / width;
            var pcd = 0.5 - 1.0 / width;
            for (int i = 0; i != 4; i += 2) {
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCoil[i], pce);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCoil[i + 1], 1 - pce);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCore[i], pcd);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCore[i + 1], 1 - pcd);
            }
            if (-1 == ce.Polarity) {
                mDots = new PointF[2];
                var dotp = Math.Abs(7.0 / height);
                Utils.InterpPoint(mPtCoil[0], mPtCoil[2], ref mDots[0], dotp, -7 * mDsign);
                Utils.InterpPoint(mPtCoil[3], mPtCoil[1], ref mDots[1], dotp, -7 * mDsign);
                var x = ce.Post[1];
                ce.Post[1] = ce.Post[3];
                ce.Post[3] = x;
                var t = mPtCoil[1];
                mPtCoil[1] = mPtCoil[3];
                mPtCoil[3] = t;
            } else {
                mDots = null;
            }
            setBbox(ce.Post[0], ce.Post[ce.Polarity == 1 ? 3 : 1], 0);
            setCoilPos(mPtCoil[0], mPtCoil[2], 90 * mDsign, out mCoilPosA);
            setCoilPos(mPtCoil[1], mPtCoil[3], -90 * mDsign * ce.Polarity, out mCoilPosB);
            setNamePos();
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
            var wn = Context.GetTextSize(DumpInfo.ReferenceName).Width;
            mNamePos = new Point((int)(mPtCore[0].X - wn / 2 + 2), (int)mPtCore[0].Y - 8);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmTransformer)Elm;

            drawLine(ce.Post[0], mPtCoil[0]);
            drawLine(ce.Post[1], mPtCoil[1]);
            drawLine(ce.Post[2], mPtCoil[2]);
            drawLine(ce.Post[3], mPtCoil[3]);

            foreach (var p in mCoilPosA) {
                Context.DrawArc(p, mCoilWidth, mCoilAngle, 180);
            }
            foreach (var p in mCoilPosB) {
                Context.DrawArc(p, mCoilWidth, mCoilAngle, -180);
            }

            drawLine(mPtCore[0], mPtCore[2]);
            drawLine(mPtCore[1], mPtCore[3]);
            if (mDots != null) {
                drawCircle(mDots[0], 2.5f);
                drawCircle(mDots[1], 2.5f);
            }

            updateDotCount(ce.Currents[0], ref ce.CurCounts[0]);
            updateDotCount(ce.Currents[1], ref ce.CurCounts[1]);
            for (int i = 0; i != 2; i++) {
                drawCurrent(ce.Post[i], mPtCoil[i], ce.CurCounts[i]);
                drawCurrent(mPtCoil[i], mPtCoil[i + 2], ce.CurCounts[i]);
                drawCurrent(ce.Post[i + 2], mPtCoil[i + 2], -ce.CurCounts[i]);
            }

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                g.DrawLeftText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmTransformer)Elm;
            arr[0] = "トランス";
            arr[1] = "L = " + Utils.UnitText(ce.PInductance, "H");
            arr[2] = "Ratio = 1:" + ce.Ratio;
            arr[3] = "Vd1 = " + Utils.VoltageText(ce.Volts[ElmTransformer.PRI_T] - ce.Volts[ElmTransformer.PRI_B]);
            arr[4] = "Vd2 = " + Utils.VoltageText(ce.Volts[ElmTransformer.SEC_T] - ce.Volts[ElmTransformer.SEC_B]);
            arr[5] = "I1 = " + Utils.CurrentText(ce.Currents[0]);
            arr[6] = "I2 = " + Utils.CurrentText(ce.Currents[1]);
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
                return new ElementInfo("名前", DumpInfo.ReferenceName);
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
                DumpInfo.ReferenceName = ei.Text;
                setNamePos();
            }
            if (n == 4) {
                ce.Polarity = ei.CheckBox.Checked ? -1 : 1;
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_REVERSE;
                } else {
                    DumpInfo.Flags &= ~FLAG_REVERSE;
                }
                SetPoints();
            }
        }
    }
}
