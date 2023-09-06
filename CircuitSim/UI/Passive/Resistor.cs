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
            ReferenceName = mLastReferenceName;
        }

        public Resistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmResistor();
            Elm = elm;
            elm.Resistance = st.nextTokenDouble(1e3);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.RESISTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmResistor)Elm;
            optionList.Add(ce.Resistance.ToString("g3"));
        }

        public override void SetPoints() {
            base.SetPoints();
            setLeads(BODY_LEN);
            setTextPos();
            setPoly();
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
            int on, ov;
            if (0 == deg) {
                on = 11;
                ov = -11;
            } else if (0 < deg && deg < 45 * 3) {
                on = 9;
                ov = -11;
            } else if (45 * 3 <= deg && deg <= 180) {
                on = -9;
                ov = 13;
            } else if (180 < deg && deg < 45 * 7) {
                on = -9;
                ov = 11;
            } else {
                on = 11;
                ov = -12;
            }
            interpPost(ref _NamePos, 0.5, on);
            interpPost(ref _ValuePos, 0.5, ov);
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
            var len = (float)Utils.Distance(_Lead1, _Lead2);
            if (0 == len) {
                return;
            }

            int hs = ControlPanel.ChkUseAnsiSymbols.Checked ? ANSI_HEIGHT : EU_HEIGHT;
            Post.SetBbox(hs);

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

            drawName();
            drawValue(Utils.UnitText(ce.Resistance));

            doDots();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmResistor)Elm;
            if (string.IsNullOrEmpty(ReferenceName)) {
                arr[0] = "抵抗：" + Utils.UnitText(ce.Resistance, CirSimForm.OHM_TEXT);
                getBasicInfo(1, arr);
            } else {
                arr[0] = ReferenceName;
                arr[1] = "抵抗：" + Utils.UnitText(ce.Resistance, CirSimForm.OHM_TEXT);
                getBasicInfo(2, arr);
            }
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
                return new ElementInfo("名前", ReferenceName);
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
                ReferenceName = ei.Text;
                mLastReferenceName = ReferenceName;
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
