using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class SwitchUI : BaseUI {
        const int OPEN_HS = 16;
        const int BODY_LEN = 28;

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

        protected override void dump(List<object> optionList) {
            var ce = (SwitchElm)Elm;
            optionList.Add(ce.Position);
            optionList.Add(ce.Momentary);
            optionList.Add(ce.Link);
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
            if (ce.Link != 0) {
                int i;
                for (i = 0; i != CirSimForm.Sim.ElmCount; i++) {
                    var o = CirSimForm.Sim.GetElm(i).Elm;
                    if (o is SwitchElm) {
                        var s2 = (SwitchElm)o;
                        if (s2.Link == ce.Link) {
                            s2.Position = ce.Position;
                        }
                    }
                }
            }
        }

        public virtual Rectangle GetSwitchRect() {
            var p1 = new Point();
            interpLead(ref p1, 0, 24);
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var l2 = new Rectangle(mLead2.X, mLead2.Y, 0, 0);
            var p = new Rectangle(p1.X, p1.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (SwitchElm)Elm;
            setBbox(mPost1, mPost2, OPEN_HS);
            draw2Leads();
            g.FillCircle(mLead1.X, mLead1.Y, 2);
            g.FillCircle(mLead2.X, mLead2.Y, 2);
            var p2 = new Point();
            if (ce.Position == 0) {
                interpLead(ref p2, 1, 2);
                doDots();
            } else {
                interpLead(ref p2, (OPEN_HS - 2.0) / OPEN_HS, OPEN_HS);
            }
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, p2);
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
            if (n == 1) {
                return new ElementInfo("グループ", ce.Link, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (SwitchElm)Elm;
            if (n == 0) {
                ce.Momentary = ei.CheckBox.Checked;
            }
            if (n == 1) {
                ce.Link = (int)ei.Value;
            }
        }
    }
}
