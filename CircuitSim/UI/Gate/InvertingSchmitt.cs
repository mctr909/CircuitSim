﻿using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class InvertingSchmitt : BaseUI {
        protected PointF[] gatePoly;
        protected PointF[] symbolPoly;
        PointF pcircle;

        double dlt;
        double dut;

        public InvertingSchmitt(Point pos, int dummy) : base(pos) {
            mNoDiagonal = true;
        }

        public InvertingSchmitt(Point pos) : base(pos) {
            Elm = new ElmInvertingSchmitt();
            mNoDiagonal = true;
        }

        public InvertingSchmitt(Point p1, Point p2, int f) : base(p1, p2, f) {
            mNoDiagonal = true;
        }

        public InvertingSchmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmInvertingSchmitt(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT_SCHMITT; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmInvertingSchmitt)Elm;
            optionList.Add(ce.SlewRate.ToString("g3"));
            optionList.Add(ce.LowerTrigger);
            optionList.Add(ce.UpperTrigger);
            optionList.Add(ce.LogicOnLevel);
            optionList.Add(ce.LogicOffLevel);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmInvertingSchmitt)Elm;
            draw2Leads();
            drawPolygon(gatePoly);
            drawPolygon(symbolPoly);
            drawCircle(pcircle, 3);
            updateDotCount(ce.Current, ref mCurCount);
            drawCurrentB(mCurCount);
        }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 10;
            Post.SetBbox(hs);
            int ww = 12;
            if (ww > Post.Len / 2) {
                ww = (int)(Post.Len / 2);
            }
            setLead1(0.5 - ww / Post.Len);
            setLead2(0.5 + (ww + 2) / Post.Len);
            interpPost(ref pcircle, 0.5 + (ww - 2) / Post.Len);

            gatePoly = new PointF[3];
            interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
            interpPost(ref gatePoly[2], 0.5 + (ww - 5) / Post.Len);

            Utils.CreateSchmitt(Elm.Post[0], Elm.Post[1], out symbolPoly, 0.8, .5 - (ww - 7) / Post.Len);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmInvertingSchmitt)Elm;
            arr[0] = "inverting Schmitt trigger";
            arr[1] = "Vin：" + Utils.VoltageText(ce.Volts[0]);
            arr[2] = "Vout：" + Utils.VoltageText(ce.Volts[1]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmInvertingSchmitt)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                dlt = ce.LowerTrigger;
                return new ElementInfo("Lower threshold (V)", ce.LowerTrigger);
            }
            if (r == 1) {
                dut = ce.UpperTrigger;
                return new ElementInfo("Upper threshold (V)", ce.UpperTrigger);
            }
            if (r == 2) {
                return new ElementInfo("Slew Rate (V/ns)", ce.SlewRate);
            }
            if (r == 3) {
                return new ElementInfo("High Voltage (V)", ce.LogicOnLevel);
            }
            if (r == 4) {
                return new ElementInfo("Low Voltage (V)", ce.LogicOffLevel);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmInvertingSchmitt)Elm;
            if (n == 0) {
                dlt = ei.Value;
            }
            if (n == 1) {
                dut = ei.Value;
            }
            if (n == 2) {
                ce.SlewRate = ei.Value;
            }
            if (n == 3) {
                ce.LogicOnLevel = ei.Value;
            }
            if (n == 4) {
                ce.LogicOffLevel = ei.Value;
            }
            if (dlt > dut) {
                ce.UpperTrigger = dlt;
                ce.LowerTrigger = dut;
            } else {
                ce.UpperTrigger = dut;
                ce.LowerTrigger = dlt;
            }
        }
    }
}
