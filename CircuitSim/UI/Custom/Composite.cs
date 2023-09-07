using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

using Circuit.Elements.Custom;

namespace Circuit.UI.Custom {
    abstract class Composite : BaseUI {
        /* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
        protected const int FLAG_ESCAPE = 1;

        public Composite(Point pos) : base(pos) { }

        public Composite(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public override bool CanViewInScope { get { return false; } }

        public override abstract DUMP_ID DumpId { get; }

        protected override void dump(List<object> optionList) {
            var ce = (ElmComposite)Elm;
            for (int i = 0; i < ce.CompElmList.Count; i++) {
                string tstring = ce.CompElmList[i].Dump();
                var rg = new Regex("[A-Za-z0-9]+ [0-9]+ [0-9]+ [0-9]+ [0-9]+ [0-9]+ ");
                var rgString = rg.Replace(tstring, "", 1).Replace(" ", "_"); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                var escString = Utils.Escape(rgString);
                optionList.Add(escString);
            }
        }

        /* dump subset of elements
         * (some of them may not have any state, and/or may be very long, so we avoid dumping them for brevity) */
        protected string dumpWithMask(int mask) {
            return dumpElements(mask);
        }

        protected string dumpElements(int mask) {
            var ce = (ElmComposite)Elm;
            string dumpStr = "";
            for (int i = 0; i < ce.CompElmList.Count; i++) {
                if ((mask & (1 << i)) == 0) {
                    continue;
                }
                string tstring = ce.CompElmList[i].Dump();
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1).Replace(" ", "_"); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                if ("" == dumpStr) {
                    dumpStr = Utils.Escape(tstring);
                } else {
                    dumpStr = string.Join(" ", dumpStr, Utils.Escape(tstring));
                }
            }
            return dumpStr;
        }

        bool useEscape() { return (_Flags & FLAG_ESCAPE) != 0; }

        public override void Delete() {
            var ce = (ElmComposite)Elm;
            for (int i = 0; i < ce.CompElmList.Count; i++) {
                ce.CompElmList[i].Delete();
            }
            base.Delete();
        }
    }
}
