﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Resistor : BaseUI {
        protected static string mLastReferenceName = "R";

        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const int ANSI_HEIGHT = 5;
        const int EU_HEIGHT = 4;
        const double SEG_F = 1.0 / SEGMENTS;

        PointF[] mP1;
        PointF[] mP2;
        PointF[] mRect1;
        PointF[] mRect2;
        PointF[] mRect3;
        PointF[] mRect4;

        public Resistor(Point pos) : base(pos) {
            Elm = new ElmResistor();
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public Resistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmResistor();
            Elm = elm;
            elm.Resistance = st.nextTokenDouble(1e3);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.RESISTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.RESISTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmResistor)Elm;
            optionList.Add(ce.Resistance);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
            setPoly();
        }

        void setTextPos() {
            if (mHorizontal) {
                interpPost(ref mValuePos, 0.5, -13 * mDsign);
                interpPost(ref mNamePos, 0.5, 11 * mDsign);
            } else if (mVertical) {
                interpPost(ref mValuePos, 0.5, 5 * mDsign);
                interpPost(ref mNamePos, 0.5, -18 * mDsign);
            } else {
                interpPost(ref mValuePos, 0.5, 10 * mDsign);
                interpPost(ref mNamePos, 0.5, -10 * mDsign);
            }
        }

        void setPoly() {
            /* zigzag */
            mP1 = new PointF[SEGMENTS];
            mP2 = new PointF[SEGMENTS];
            int oy = 0;
            int ny;
            for (int i = 0; i != SEGMENTS; i++) {
                switch (i & 3) {
                case 0:
                    ny = ANSI_HEIGHT;
                    break;
                case 2:
                    ny = -ANSI_HEIGHT;
                    break;
                default:
                    ny = 0;
                    break;
                }
                interpLead(ref mP1[i], i * SEG_F, oy);
                interpLead(ref mP2[i], (i + 1) * SEG_F, ny);
                oy = ny;
            }

            /* rectangle */
            mRect1 = new PointF[SEGMENTS + 2];
            mRect2 = new PointF[SEGMENTS + 2];
            mRect3 = new PointF[SEGMENTS + 2];
            mRect4 = new PointF[SEGMENTS + 2];
            interpLeadAB(ref mRect1[0], ref mRect2[0], 0, EU_HEIGHT);
            for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                interpLeadAB(ref mRect1[j], ref mRect2[j], i * SEG_F, EU_HEIGHT);
                interpLeadAB(ref mRect3[j], ref mRect4[j], (i + 1) * SEG_F, EU_HEIGHT);
            }
            interpLeadAB(ref mRect1[SEGMENTS + 1], ref mRect2[SEGMENTS + 1], 1, EU_HEIGHT);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmResistor)Elm;
            var len = (float)Utils.Distance(mLead1, mLead2);
            if (0 == len) {
                return;
            }

            int hs = ControlPanel.ChkUseAnsiSymbols.Checked ? ANSI_HEIGHT : EU_HEIGHT;
            setBbox(hs);

            draw2Leads();

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i < SEGMENTS; i++) {
                    drawLine(mP1[i], mP2[i]);
                }
            } else {
                /* draw rectangle */
                drawLine(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i < SEGMENTS; i++, j++) {
                    drawLine(mRect1[j], mRect3[j]);
                    drawLine(mRect2[j], mRect4[j]);
                }
                drawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            drawValue(ce.Resistance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmResistor)Elm;
            arr[0] = string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "抵抗" : DumpInfo.ReferenceName;
            getBasicInfo(arr);
            arr[3] = "R = " + Utils.UnitText(ce.Resistance, CirSimForm.OHM_TEXT);
        }

        public override string GetScopeText() {
            var ce = (ElmResistor)Elm;
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "抵抗" : DumpInfo.ReferenceName) + " "
                + Utils.UnitText(ce.Resistance, CirSimForm.OHM_TEXT);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmResistor)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("レジスタンス(Ω)", ce.Resistance);
            }
            if (r == 1) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmResistor)Elm;
            if (n == 0 && 0 < ei.Value) {
                ce.Resistance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Text;
                mLastReferenceName = DumpInfo.ReferenceName;
                setTextPos();
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var ce = (ElmResistor)Elm;
            return new EventHandler((s, e) => {
                var trb = adj.Slider;
                ce.Resistance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
