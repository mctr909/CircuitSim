using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Switch : BaseUI {
        const int OPEN_HS = 12;
        const int BODY_LEN = 24;

        public Switch(Point pos, int dummy) : base(pos) { }

        public Switch(Point pos, bool momentary = false, bool isNo = false) : base(pos) {
            var elm = new ElmSwitch();
            Elm = elm;
            elm.Momentary = momentary;
            elm.Position = isNo ? 1 : 0;
        }

        public Switch(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public Switch(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmSwitch();
            Elm = elm;
            st.nextTokenInt(out elm.Position);
            st.nextTokenBool(out elm.Momentary, false);
            st.nextTokenInt(out elm.Link);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.SWITCH; } }
        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmSwitch)Elm;
            optionList.Add(ce.Position);
            optionList.Add(ce.Momentary);
            optionList.Add(ce.Link);
        }

        public void MouseUp() {
            var ce = (ElmSwitch)Elm;
            if (ce.Momentary) {
                Toggle();
            }
        }

        public void Toggle() {
            var ce = (ElmSwitch)Elm;
            ce.Position++;
            if (ce.PosCount <= ce.Position) {
                ce.Position = 0;
            }
            if (ce.Link != 0) {
                int i;
                for (i = 0; i != CirSimForm.UICount; i++) {
                    var ui2 = CirSimForm.GetUI(i);
                    if (ui2 == this) {
                        continue;
                    }
                    if (this is SwitchMulti) {
                        if (ui2 is SwitchMulti) {
                            var s2 = (ElmSwitchMulti)ui2.Elm;
                            if (s2.Link == ce.Link) {
                                if (ce.Position < s2.ThrowCount) {
                                    s2.Position = ce.Position;
                                }
                            }
                        }
                    } else {
                        if (ui2.Elm is ElmSwitch) {
                            var s2 = (ElmSwitch)ui2.Elm;
                            if (s2.Link == ce.Link) {
                                s2.Position = s2.Position == 0 ? 1 : 0;
                            }
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
            var ce = (ElmSwitch)Elm;
            setBbox(OPEN_HS);
            draw2Leads();
            var fillColorBackup = g.FillColor;
            g.FillColor = CustomGraphics.PostColor;
            g.FillCircle(mLead1.X, mLead1.Y, 2.5f);
            g.FillCircle(mLead2.X, mLead2.Y, 2.5f);
            g.FillColor = fillColorBackup;
            /* draw switch */
            var p2 = new Point();
            if (ce.Position == 0) {
                interpLead(ref p2, 1, 2);
                doDots();
            } else {
                interpLead(ref p2, (OPEN_HS - 2.0) / OPEN_HS, OPEN_HS);
            }
            g.DrawLine(mLead1, p2);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmSwitch)Elm;
            arr[0] = (ce.Momentary) ? "push switch" : "switch";
            if (ce.Position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + Utils.VoltageAbsText(ce.GetVoltageDiff());
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
                arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmSwitch)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("連動グループ", ce.Link);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmSwitch)Elm;
            if (n == 0) {
                ce.Link = (int)ei.Value;
            }
        }
    }
}
