using System.Drawing;
using System.Text.RegularExpressions;

namespace Circuit.Elements.Custom {
    abstract class CompositeUI : BaseUI {
        /* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
        protected const int FLAG_ESCAPE = 1;

        protected Point[] mPosts;

        public CompositeUI(Point pos) : base(pos) { }

        public CompositeUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public override bool CanViewInScope { get { return false; } }

        public override abstract DUMP_ID DumpType { get; }

        protected override string dump() {
            return dumpElements();
        }

        protected string dumpElements() {
            var ce = (CompositeElm)CirElm;
            string dumpStr = "";
            for (int i = 0; i < ce.compElmList.Count; i++) {
                string tstring = ce.compElmList[i].Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1).Replace(" ", "_"); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                if ("" == dumpStr) {
                    dumpStr = CustomLogicModel.escape(tstring);
                } else {
                    dumpStr = string.Join(" ", dumpStr, CustomLogicModel.escape(tstring));
                }
            }
            return dumpStr;
        }

        /* dump subset of elements
         * (some of them may not have any state, and/or may be very long, so we avoid dumping them for brevity) */
        protected string dumpWithMask(int mask) {
            return dumpElements(mask);
        }

        protected string dumpElements(int mask) {
            var ce = (CompositeElm)CirElm;
            string dumpStr = "";
            for (int i = 0; i < ce.compElmList.Count; i++) {
                if ((mask & (1 << i)) == 0) {
                    continue;
                }
                string tstring = ce.compElmList[i].Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1); /* remove unused tint x1 y1 x2 y2 coords for internal components */
                dumpStr += " " + CustomLogicModel.escape(tstring);
            }
            return dumpStr;
        }

        bool useEscape() { return (mFlags & FLAG_ESCAPE) != 0; }

        /* are n1 and n2 connected internally somehow? */
        public override bool GetConnection(int n1, int n2) {
            var ce = (CompositeElm)CirElm;
            var cnLinks1 = ce.compNodeList[n1].Links;
            var cnLinks2 = ce.compNodeList[n2].Links;

            /* see if any elements are connected to both n1 and n2, then call getConnection() on those */
            for (int i = 0; i < cnLinks1.Count; i++) {
                CircuitNodeLink link1 = cnLinks1[i];
                for (int j = 0; j < cnLinks2.Count; j++) {
                    CircuitNodeLink link2 = cnLinks2[j];
                    if (link1.Elm == link2.Elm && link1.Elm.GetConnection(link1.Num, link2.Num)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public override Point GetPost(int n) {
            return mPosts[n];
        }

        protected void setPost(int n, Point p) {
            mPosts[n] = p;
        }

        void setPost(int n, int x, int y) {
            mPosts[n].X = x;
            mPosts[n].Y = y;
        }

        public override void Delete() {
            var ce = (CompositeElm)CirElm;
            for (int i = 0; i < ce.compElmList.Count; i++) {
                ce.compElmList[i].Delete();
            }
            base.Delete();
        }
    }
}
