using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Pot : BaseUI {
        const int FLAG_SHOW_VALUES = 1;

        const int HS = 5;
        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const double SEG_F = 1.0 / SEGMENTS;

        Point mCorner2;
        Point mArrowPoint;
        PointF mMidPoint;
        PointF mArrow1;
        PointF mArrow2;
        PointF[] mPs1;
        PointF[] mPs2;
        PointF[] mRect1;
        PointF[] mRect2;
        PointF[] mRect3;
        PointF[] mRect4;

        TrackBar mSlider;
        Label mLabel;
        string mName;

        public Pot(Point pos) : base(pos) {
            Elm = new ElmPot();
            Elm.AllocNodes();
            mFlags = FLAG_SHOW_VALUES;
            ReferenceName = "VR";
            createSlider();
        }

        public Pot(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmPot();
            Elm = elm;
            elm.MaxResistance = st.nextTokenDouble(1e3);
            elm.Position = st.nextTokenDouble(0.5);
            elm.AllocNodes();
            createSlider();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.POT; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmPot)Elm;
            optionList.Add(ce.MaxResistance.ToString("g3"));
            optionList.Add(ce.Position);
        }

        public override void Delete() {
            ControlPanel.RemoveSlider(mLabel);
            ControlPanel.RemoveSlider(mSlider);
            base.Delete();
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(HS);
            Post.Vertical = Math.Abs(Post.Diff.X) <= Math.Abs(Post.Diff.Y);
            Post.Horizontal = !Post.Vertical;

            int offset = 0;
            if (Post.Vertical) {
                /* vertical */
                var myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(Post.Diff.Y)
                    * ((Math.Abs(Post.Diff.Y) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
                if (Post.Diff.Y != 0) {
                    Elm.Term[1].Y = Elm.Term[0].Y + myLen;
                    offset = (0 < Post.Diff.Y) ? Post.Diff.X : -Post.Diff.X;
                    Elm.Term[1].X = Elm.Term[0].X;
                }
            } else {
                /* horizontal */
                var myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(Post.Diff.X)
                    * ((Math.Abs(Post.Diff.X) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
                Elm.Term[1].X = Elm.Term[0].X + myLen;
                offset = (Post.Diff.X < 0) ? Post.Diff.Y : -Post.Diff.Y;
                Elm.Term[1].Y = Elm.Term[0].Y;
            }
            if (offset < CirSimForm.GRID_SIZE) {
                offset = CirSimForm.GRID_SIZE;
            }
            Post.Len = Utils.Distance(Elm.Term[0], Elm.Term[1]);

            calcLeads(BODY_LEN);
            
            /* set slider */
            var ce = (ElmPot)Elm;
            ce.Position = mSlider.Value * 0.0099 + 0.0001;
            var poff = 0.5;
            var woff = -7.0;
            int soff = (int)((ce.Position - poff) * BODY_LEN);
            interpPost(ref ce.Term[2], poff, offset);
            interpPost(ref mCorner2, soff / Post.Len + poff, offset);
            interpPost(ref mArrowPoint, soff / Post.Len + poff, 7 * Math.Sign(offset));
            interpPost(ref mMidPoint, soff / Post.Len + poff);
            var clen = Math.Abs(offset) + woff;
            Utils.InterpPoint(mCorner2, mArrowPoint, ref mArrow1, ref mArrow2, (clen + woff) / clen, 4);

            setPoly();
            setTextPos();
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmPot)Elm;

            draw2Leads();

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i != SEGMENTS; i++) {
                    drawLine(mPs1[i], mPs2[i]);
                }
            } else {
                /* draw rectangle */
                drawLine(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                    drawLine(mRect1[j], mRect3[j]);
                    drawLine(mRect2[j], mRect4[j]);
                }
                drawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            /* draw slider */
            drawLine(ce.Term[2], mCorner2);
            drawLine(mCorner2, mArrowPoint);
            drawLine(mArrow1, mArrowPoint);
            drawLine(mArrow2, mArrowPoint);

            /* draw dot */
            updateDotCount(ce.Current1, ref ce.CurCount1);
            updateDotCount(ce.Current2, ref ce.CurCount2);
            updateDotCount(ce.Current3, ref ce.CurCount3);
            if (CirSimForm.ConstructElm != this) {
                drawCurrent(ce.Term[0], mMidPoint, ce.CurCount1);
                drawCurrent(ce.Term[1], mMidPoint, ce.CurCount2);
                drawCurrent(ce.Term[2], mCorner2, ce.CurCount3);
                drawCurrent(mCorner2, mMidPoint, ce.CurCount3 + Utils.Distance(ce.Term[2], mCorner2));
            }

            if (ControlPanel.ChkShowValues.Checked && ce.Resistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (ce.Term[2].X < mLead1.X && mLead1.X == mLead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (ce.Term[2].Y < mLead1.Y && mLead1.X != mLead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

                /* draw units */
                var s1 = Utils.UnitText(rev ? ce.Resistance2 : ce.Resistance1, "");
                var s2 = Utils.UnitText(rev ? ce.Resistance1 : ce.Resistance2, "");
                var txtHeightHalf = CustomGraphics.TextSize * 0.5f;
                var txtWidth1 = (int)g.GetTextSize(s1).Width;
                var txtWidth2 = (int)g.GetTextSize(s2).Width;

                if (Post.Horizontal) {
                    var y = (int)(mArrowPoint.Y + (reverseX ? -txtHeightHalf : txtHeightHalf));
                    g.DrawLeftText(s1, Math.Min(mArrow1.X, mArrow2.X) - txtWidth1, y);
                    g.DrawLeftText(s2, Math.Max(mArrow1.X, mArrow2.X), y);
                } else {
                    g.DrawLeftText(s1, reverseY ? (mArrowPoint.X - txtWidth1) : mArrowPoint.X, (int)(Math.Min(mArrow1.Y, mArrow2.Y) + txtHeightHalf * 3));
                    g.DrawLeftText(s2, reverseY ? (mArrowPoint.X - txtWidth2) : mArrowPoint.X, (int)(Math.Max(mArrow1.Y, mArrow2.Y) - txtHeightHalf * 3));
                }
            }
            if (Post.Vertical) {
                drawCenteredText(mName, mNamePos, -Math.PI / 2);
            } else {
                g.DrawLeftText(mName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmPot)Elm;
            arr[0] = "可変抵抗：" + Utils.UnitText(ce.MaxResistance, CirSimForm.OHM_TEXT);
            arr[1] = "Vd：" + Utils.VoltageAbsText(ce.GetVoltageDiff());
            arr[2] = "R1：" + Utils.UnitText(ce.Resistance1, CirSimForm.OHM_TEXT);
            arr[3] = "R2：" + Utils.UnitText(ce.Resistance2, CirSimForm.OHM_TEXT);
            arr[4] = "I1：" + Utils.CurrentAbsText(ce.Current1);
            arr[5] = "I2：" + Utils.CurrentAbsText(ce.Current2);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmPot)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("レジスタンス(Ω)", ce.MaxResistance);
            }
            if (r == 1) {
                return new ElementInfo("名前", ReferenceName);
            }
            if (r == 2) {
                return new ElementInfo("値を表示", (mFlags & FLAG_SHOW_VALUES) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmPot)Elm;
            if (n == 0) {
                ce.MaxResistance = ei.Value;
            }
            if (n == 1) {
                ReferenceName = ei.Text;
                mLabel.Text = ReferenceName;
                ControlPanel.SetSliderPanelHeight();
            }
            if (n == 2) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_SHOW_VALUES);
            }
            setTextPos();
        }

        void setPoly() {
            /* set zigzag */
            int oy = 0;
            int ny;
            mPs1 = new PointF[SEGMENTS + 1];
            mPs2 = new PointF[SEGMENTS + 1];
            for (int i = 0; i != SEGMENTS; i++) {
                switch (i & 3) {
                case 0:
                    ny = HS;
                    break;
                case 2:
                    ny = -HS;
                    break;
                default:
                    ny = 0;
                    break;
                }
                interpLead(ref mPs1[i], i * SEG_F, oy);
                interpLead(ref mPs2[i], (i + 1) * SEG_F, ny);
                oy = ny;
            }

            /* set rectangle */
            mRect1 = new PointF[SEGMENTS + 2];
            mRect2 = new PointF[SEGMENTS + 2];
            mRect3 = new PointF[SEGMENTS + 2];
            mRect4 = new PointF[SEGMENTS + 2];
            interpLeadAB(ref mRect1[0], ref mRect2[0], 0, HS);
            for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                interpLeadAB(ref mRect1[j], ref mRect2[j], i * SEG_F, HS);
                interpLeadAB(ref mRect3[j], ref mRect4[j], (i + 1) * SEG_F, HS);
            }
            interpLeadAB(ref mRect1[SEGMENTS + 1], ref mRect2[SEGMENTS + 1], 1, HS);
        }

        void setTextPos() {
            mName = "";
            if (ControlPanel.ChkShowName.Checked) {
                mName += ReferenceName;
            }
            if (ControlPanel.ChkShowValues.Checked) {
                if (!string.IsNullOrEmpty(mName)) {
                    mName += " ";
                }
                var ce = (ElmPot)Elm;
                mName += Utils.UnitText(ce.MaxResistance);
            }
            if (Post.Horizontal) {
                var wn = Context.GetTextSize(mName).Width * 0.5;
                if (Post.Diff.Y != 0) {
                    if (0 < Post.Diff.Y) {
                        /* right slider */
                        interpPost(ref mNamePos, 0.5 + wn / Post.Len, -12 * Post.Dsign);
                    } else {
                        /* left slider */
                        interpPost(ref mNamePos, 0.5 - wn / Post.Len, 12 * Post.Dsign);
                    }
                }
            } else {
                if (0 < Post.Diff.X) {
                    /* upper slider */
                    interpPost(ref mNamePos, 0.5, -10 * Post.Dsign);
                } else {
                    /* lower slider */
                    interpPost(ref mNamePos, 0.5, 11 * Post.Dsign);
                }
            }
        }

        void createSlider() {
            var ce = (ElmPot)Elm;
            ControlPanel.AddSlider(mLabel = new Label() {
                TextAlign = ContentAlignment.BottomLeft,
                Text = ReferenceName
            });
            int value = (int)(ce.Position * 100);
            ControlPanel.AddSlider(mSlider = new TrackBar() {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 10,
                Value = value,
                Width = 175
            });
            mSlider.ValueChanged += new EventHandler((s, e) => { execute(); });
        }

        void execute() {
            SetPoints();
            CirSimForm.NeedAnalyze();
        }
    }
}
