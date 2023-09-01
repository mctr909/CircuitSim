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
            elm.Position = st.nextTokenInt();
            elm.Momentary = st.nextTokenBool(false);
            elm.Link = st.nextTokenInt();
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH; } }

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

        public virtual RectangleF GetSwitchRect() {
            var p1 = new PointF();
            interpLead(ref p1, 0, 24);
            var l1 = new RectangleF(_Lead1.X, _Lead1.Y, 0, 0);
            var l2 = new RectangleF(_Lead2.X, _Lead2.Y, 0, 0);
            var p = new RectangleF(p1.X, p1.Y, 0, 0);
            return RectangleF.Union(l1, RectangleF.Union(l2, p));
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(OPEN_HS);
            setLeads(BODY_LEN);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmSwitch)Elm;
            draw2Leads();
            g.DrawPost(_Lead1);
            g.DrawPost(_Lead2);
            /* draw switch */
            var p2 = new PointF();
            if (ce.Position == 0) {
                interpLead(ref p2, 1, 2);
                doDots();
            } else {
                interpLead(ref p2, (OPEN_HS - 2.0) / OPEN_HS, OPEN_HS);
            }
            drawLine(_Lead1, p2);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmSwitch)Elm;
            arr[0] = ce.Momentary ? "プッシュスイッチ(" : "スイッチ(";
            if (ce.Position == 1) {
                arr[0] += "OFF)";
                arr[1] = "電位差：" + Utils.VoltageAbsText(ce.GetVoltageDiff());
            } else {
                arr[0] += "ON)";
                arr[1] = "電位：" + Utils.VoltageText(ce.Volts[0]);
                arr[2] = "電流：" + Utils.CurrentAbsText(ce.Current);
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
