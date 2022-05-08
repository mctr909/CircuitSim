﻿using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class SwitchUI : BaseUI {
        const int OPEN_HS = 16;
        const int BODY_LEN = 28;

        Point mP1;
        Point mP2;

        public SwitchUI(Point pos, int dummy) : base(pos) { }

        public SwitchUI(Point pos) : base(pos) {
            Elm = new SwitchElm();
        }

        public SwitchUI(Point pos, bool mm) : base(pos) {
            Elm = new SwitchElm(mm);
        }

        public SwitchUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public SwitchUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new SwitchElm(st);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.SWITCH; } }
        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH; } }

        protected override string dump() {
            var ce = (SwitchElm)Elm;
            return ce.Position + " " + ce.Momentary;
        }

        public void MouseUp() {
            var ce = (SwitchElm)Elm;
            if (ce.Momentary) {
                Toggle();
            }
        }

        public virtual void Toggle() {
            var ce = (SwitchElm)Elm;
            ce.Position++;
            if (ce.PosCount <= ce.Position) {
                ce.Position = 0;
            }
        }

        public virtual Rectangle GetSwitchRect() {
            interpLead(ref mP1, 0, OPEN_HS);
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var l2 = new Rectangle(mLead2.X, mLead2.Y, 0, 0);
            var p = new Rectangle(mP1.X, mP1.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (SwitchElm)Elm;
            int hs1 = (ce.Position == 1) ? 0 : 2;
            int hs2 = (ce.Position == 1) ? OPEN_HS : 2;
            setBbox(mPost1, mPost2, OPEN_HS);
            draw2Leads();
            if (ce.Position == 0) {
                doDots();
            }
            interpLead(ref mP1, 0, hs1);
            interpLead(ref mP2, 1, hs2);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            g.DrawLine(mP1, mP2);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (SwitchElm)Elm;
            arr[0] = (ce.Momentary) ? "push switch (SPST)" : "switch (SPST)";
            if (ce.Position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
                arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (SwitchElm)Elm;
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "モーメンタリ";
                ei.CheckBox.Checked = ce.Momentary;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (SwitchElm)Elm;
            if (n == 0) {
                ce.Momentary = ei.CheckBox.Checked;
            }
        }
    }
}