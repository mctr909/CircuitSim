﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class TriState : BaseUI {
        const int BODY_LEN = 16;

        Point mLead3;
        PointF[] mGatePoly;

        public TriState(Point pos) : base(pos) {
            Elm = new ElmTriState();
        }

        public TriState(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmTriState(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRISTATE; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmTriState)Elm;
            optionList.Add(ce.Ron.ToString("g3"));
            optionList.Add(ce.Roff.ToString("g3"));
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(BODY_LEN);
            calcLeads(BODY_LEN);
            int hs = BODY_LEN / 2;
            int ww = BODY_LEN / 2;
            if (ww > Post.Len / 2) {
                ww = (int)(Post.Len / 2);
            }
            mGatePoly = new PointF[3];
            interpLeadAB(ref mGatePoly[0], ref mGatePoly[1], 0, hs);
            interpPost(ref mGatePoly[2], 0.5 + ww / Post.Len);
            interpPost(ref ((ElmTriState)Elm).Term[2], 0.5, -hs);
            interpPost(ref mLead3, 0.5, -hs / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmTriState)Elm;
            draw2Leads();
            drawPolygon(mGatePoly);
            drawLine(ce.Term[2], mLead3);
            updateDotCount(ce.Current, ref mCurCount);
            drawCurrentB(mCurCount);
        }

        public override void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            if (Math.Abs(Post.A.X - pos.X) < Math.Abs(Post.A.Y - pos.Y)) {
                pos.X = Post.A.X;
            } else {
                pos.Y = Post.A.Y;
            }
            int q1 = Math.Abs(Post.A.X - pos.X) + Math.Abs(Post.A.Y - pos.Y);
            int q2 = (q1 / 2) % CirSimForm.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            Post.B = pos;
            SetPoints();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmTriState)Elm;
            arr[0] = "tri-state buffer";
            arr[1] = ce.Open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.GetVoltageDiff());
            arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[4] = "Vc = " + Utils.VoltageText(ce.Volts[2]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmTriState)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("オン抵抗(Ω)", ce.Ron);
            }
            if (r == 1) {
                return new ElementInfo("オフ抵抗(Ω)", ce.Roff);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmTriState)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Ron = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ce.Roff = ei.Value;
            }
        }
    }
}
