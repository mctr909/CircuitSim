﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Transformer : BaseUI {
        public const int FLAG_REVERSE = 4;

        const int BODY_LEN = 24;

        Point[] mPtCoil;
        Point[] mPtCore;
        Point[] mDots;

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
            mPtCoil = new Point[4];
            mPtCore = new Point[4];
            interpPoint(ref ce.Post[2], 0, -mDsign * height);
            interpPoint(ref ce.Post[3], 1, -mDsign * height);
            var pce = 0.5 - 10.0 / width;
            var pcd = 0.5 - 1.0 / width;
            for (int i = 0; i != 4; i += 2) {
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCoil[i], pce);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCoil[i + 1], 1 - pce);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCore[i], pcd);
                Utils.InterpPoint(ce.Post[i], ce.Post[i + 1], ref mPtCore[i + 1], 1 - pcd);
            }
            if (-1 == ce.Polarity) {
                mDots = new Point[2];
                var dotp = Math.Abs(7.0 / height);
                Utils.InterpPoint(mPtCoil[0], mPtCoil[2], ref mDots[0], dotp, -7 * mDsign);
                Utils.InterpPoint(mPtCoil[3], mPtCoil[1], ref mDots[1], dotp, -7 * mDsign);
                var x = ce.Post[1];
                ce.Post[1] = ce.Post[3];
                ce.Post[3] = x;
                x = mPtCoil[1];
                mPtCoil[1] = mPtCoil[3];
                mPtCoil[3] = x;
            } else {
                mDots = null;
            }
            setNamePos();
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmTransformer)Elm;

            drawLead(ce.Post[0], mPtCoil[0]);
            drawLead(ce.Post[1], mPtCoil[1]);
            drawLead(ce.Post[2], mPtCoil[2]);
            drawLead(ce.Post[3], mPtCoil[3]);

            drawCoil(mPtCoil[0], mPtCoil[2], ce.Volts[ElmTransformer.PRI_T], ce.Volts[ElmTransformer.PRI_B], 90 * mDsign);
            drawCoil(mPtCoil[1], mPtCoil[3], ce.Volts[ElmTransformer.SEC_T], ce.Volts[ElmTransformer.SEC_B], -90 * mDsign * ce.Polarity);

            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.DrawLine(mPtCore[0], mPtCore[2]);
            g.DrawLine(mPtCore[1], mPtCore[3]);
            if (mDots != null) {
                g.DrawCircle(mDots[0], 2.5f);
                g.DrawCircle(mDots[1], 2.5f);
            }

            updateDotCount(ce.Currents[0], ref ce.CurCounts[0]);
            updateDotCount(ce.Currents[1], ref ce.CurCounts[1]);
            for (int i = 0; i != 2; i++) {
                drawDots(ce.Post[i], mPtCoil[i], ce.CurCounts[i]);
                drawDots(mPtCoil[i], mPtCoil[i + 2], ce.CurCounts[i]);
                drawDots(ce.Post[i + 2], mPtCoil[i + 2], -ce.CurCounts[i]);
            }

            drawPosts();
            setBbox(ce.Post[0], ce.Post[ce.Polarity == 1 ? 3 : 1], 0);

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
                DumpInfo.ReferenceName = ei.Textf.Text;
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

        void setNamePos() {
            var wn = Context.GetTextSize(DumpInfo.ReferenceName).Width;
            mNamePos = new Point((int)(mPtCore[0].X - wn / 2 + 2), mPtCore[0].Y - 8);
        }
    }
}
