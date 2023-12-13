using Circuit.Elements.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Circuit.UI.Custom {
    abstract class Composite : BaseUI {
        /* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
        protected const int FLAG_ESCAPE = 1;

        protected List<IUI> CompList = new List<IUI>();

        public Composite(Point pos) : base(pos) { }

        public Composite(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public override bool CanViewInScope { get { return false; } }

        public override abstract DUMP_ID DumpId { get; }

        protected override void dump(List<object> optionList) {
            for (int i = 0; i < CompList.Count; i++) {
                string tstring = CompList[i].Dump();
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
            string dumpStr = "";
            for (int i = 0; i < CompList.Count; i++) {
                if ((mask & (1 << i)) == 0) {
                    continue;
                }
                string tstring = CompList[i].Dump();
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

        bool useEscape() { return (mFlags & FLAG_ESCAPE) != 0; }

        public override void Delete() {
            for (int i = 0; i < CompList.Count; i++) {
                CompList[i].Delete();
            }
            base.Delete();
        }

        protected void loadComposite(StringTokenizer stIn, string model, int[] externalNodes, string expr) {
            var compNodeHash = new Dictionary<int, CircuitNode>();
            var modelLinet = new StringTokenizer(model, "\r");

            /* Build compUIList and compNodeHash from input string */
            while (modelLinet.HasMoreTokens) {
                string line;
                modelLinet.nextToken(out line);
                var stModel = new StringTokenizer(line, " +\t\n\r\f");
                var ceType = stModel.nextTokenEnum(DUMP_ID.INVALID);
                var newce = MenuItems.ConstructElement(ceType);
                if (stIn != null) {
                    var tint = newce.DumpId;
                    string dumpedCe;
                    stIn.nextToken(out dumpedCe);
                    dumpedCe = Utils.Unescape(dumpedCe);
                    var stCe = new StringTokenizer(dumpedCe, "_");
                    // TODO: CompositeElm loadComposite
                    //int flags = stCe.nextTokenInt();
                    int flags = 0;
                    newce = MenuItems.CreateCe(tint, new Point(), new Point(), flags, stCe);
                }
                newce.ReferenceName = "";
                CompList.Add(newce);

                int thisPost = 0;
                while (stModel.HasMoreTokens) {
                    var nodeOfThisPost = stModel.nextTokenInt();
                    var cnLink = new CircuitNode.LINK();
                    cnLink.Num = thisPost;
                    cnLink.Elm = newce.Elm;
                    if (!compNodeHash.ContainsKey(nodeOfThisPost)) {
                        var cn = new CircuitNode();
                        cn.Links.Add(cnLink);
                        compNodeHash.Add(nodeOfThisPost, cn);
                    } else {
                        var cn = compNodeHash[nodeOfThisPost];
                        cn.Links.Add(cnLink);
                    }
                    thisPost++;
                }
            }
            ((ElmComposite)Elm).SetComposite(compNodeHash, CompList, externalNodes, expr);
        }

    }
}
