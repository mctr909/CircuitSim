using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class AnalogSwitch : BaseUI {
        const int FLAG_INVERT = 1;
        const int OPEN_HS = 16;
        const int BODY_LEN = 24;

        PointF mCtrlTerm;
        PointF mCtrlLead;

        public AnalogSwitch(Point pos) : base(pos) {
            Elm = new ElmAnalogSwitch();
        }

        public AnalogSwitch(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            var elm = new ElmAnalogSwitch();
            Elm = elm;
            elm.Ron = st.nextTokenDouble(1e-3);
            elm.Roff = st.nextTokenDouble(1e9);
        }

        protected override void dump(List<object> optionList) {
            var ce = (ElmAnalogSwitch)Elm;
            optionList.Add(ce.Ron.ToString("g3"));
            optionList.Add(ce.Roff.ToString("g3"));
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.ANALOG_SW; } }

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

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(OPEN_HS);
            setLeads(BODY_LEN);
            interpPost(ref mCtrlTerm, 0.5, -OPEN_HS);
            interpPost(ref mCtrlLead, 0.5, -OPEN_HS / 3);
            Elm.SetNodePos(Post.A, Post.B, mCtrlTerm);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmAnalogSwitch)Elm;
            var hs = ce.IsOpen ? (OPEN_HS - 6) : 0;
            var ps = new PointF();
            interpLead(ref ps, 1, hs);

            draw2Leads();
            drawLine(_Lead1, ps);
            drawLine(mCtrlTerm, mCtrlLead);

            if (!ce.IsOpen) {
                doDots();
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmAnalogSwitch)Elm;
            arr[0] = "アナログスイッチ(" + (ce.IsOpen ? "OFF)" : "ON)");
            arr[1] = "電位差：" + Utils.VoltageAbsText(ce.GetVoltageDiff());
            arr[2] = "電流：" + Utils.CurrentAbsText(ce.Current);
            arr[3] = "制御電圧：" + Utils.VoltageText(ce.Volts[2]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmAnalogSwitch)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("ノーマリクローズ", (_Flags & FLAG_INVERT) != 0);
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
                _Flags = ei.CheckBox.Checked ? (_Flags | FLAG_INVERT) : (_Flags & ~FLAG_INVERT);
                ce.Invert = 0 != (_Flags & FLAG_INVERT);
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
