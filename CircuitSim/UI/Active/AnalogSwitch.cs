using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class AnalogSwitch : BaseUI {
        const int FLAG_INVERT = 1;
        const int OPEN_HS = 16;
        const int BODY_LEN = 24;

        Point mPs;
        Point mLead3;

        public AnalogSwitch(Point pos) : base(pos) {
            Elm = new ElmAnalogSwitch();
        }

        public AnalogSwitch(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            var elm = new ElmAnalogSwitch();
            Elm = elm;
            try {
                elm.Ron = double.Parse(st.nextToken());
                elm.Roff = double.Parse(st.nextToken());
            } catch { }
        }

        protected override void dump(List<object> optionList) {
            var ce = (ElmAnalogSwitch)Elm;
            optionList.Add(ce.Ron);
            optionList.Add(ce.Roff);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        public override void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            if (Math.Abs(DumpInfo.P1X - pos.X) < Math.Abs(DumpInfo.P1Y - pos.Y)) {
                pos.X = DumpInfo.P1X;
            } else {
                pos.Y = DumpInfo.P1Y;
            }
            int q1 = Math.Abs(DumpInfo.P1X - pos.X) + Math.Abs(DumpInfo.P1Y - pos.Y);
            int q2 = (q1 / 2) % CirSimForm.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            DumpInfo.SetP2(pos);
            SetPoints();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mPs = new Point();
            interpPoint(ref ((ElmAnalogSwitch)Elm).Post[2], 0.5, -OPEN_HS);
            interpPoint(ref mLead3, 0.5, -OPEN_HS / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmAnalogSwitch)Elm;
            int hs = ce.IsOpen ? OPEN_HS : 0;
            setBbox(OPEN_HS);

            draw2Leads();

            interpLead(ref mPs, 1, hs);
            g.DrawColor = CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mPs);

            drawLead(ce.Post[2], mLead3);

            if (!ce.IsOpen) {
                doDots();
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmAnalogSwitch)Elm;
            arr[0] = "analog switch";
            arr[1] = ce.IsOpen ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[4] = "Vc = " + Utils.VoltageText(ce.Volts[2]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmAnalogSwitch)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("ノーマリクローズ", (DumpInfo.Flags & FLAG_INVERT) != 0);
            }
            if (r == 1) {
                return new ElementInfo("オン抵抗(Ω)", ce.Ron);
            }
            if (r == 2) {
                return new ElementInfo("オフ抵抗(Ω)", ce.Roff);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmAnalogSwitch)Elm;
            if (n == 0) {
                DumpInfo.Flags = ei.CheckBox.Checked ? (DumpInfo.Flags | FLAG_INVERT) : (DumpInfo.Flags & ~FLAG_INVERT);
                ce.Invert = 0 != (DumpInfo.Flags & FLAG_INVERT);
            }
            if (n == 1 && 0 < ei.Value) {
                ce.Ron = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                ce.Roff = ei.Value;
            }
        }
    }
}
