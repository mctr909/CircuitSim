using Circuit.Elements.Passive;
using System.Drawing;

namespace Circuit.UI.Passive {
    class SwitchPushC : SwitchMulti {
        public SwitchPushC(Point pos) : base(pos) {
            var elm = (ElmSwitchMulti)Elm;
            elm.Position = 1;
            elm.Momentary = true;
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmSwitch)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("連動グループ", ce.Link, true);
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
