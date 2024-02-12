using System.Text.RegularExpressions;

namespace Circuit.Symbol.Custom {
	abstract class Composite : BaseSymbol {
		/* need to use escape() instead of converting spaces to _'s so composite elements can be nested */
		protected const int FLAG_ESCAPE = 1;

		protected List<BaseSymbol> CompList = new List<BaseSymbol>();

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

		protected Dictionary<int, CircuitNode> getCompNode(string models) {
			var compNodeHash = new Dictionary<int, CircuitNode>();
			var strModels = new StringTokenizer(models, "\r");
			while (strModels.HasMoreTokens) {
				strModels.nextToken(out string modelLine);
				var strModel = new StringTokenizer(modelLine, " +\t\n\r\f");
				var ceType = strModel.nextTokenEnum(DUMP_ID.INVALID);
				var newce = SymbolMenu.Construct(ceType);
				newce.ReferenceName = "";
				CompList.Add(newce);
				int thisPost = 0;
				while (strModel.HasMoreTokens) {
					var nodeOfThisPost = strModel.nextTokenInt();
					var cnLink = new CircuitNode.LINK() {
						Num = thisPost,
						Elm = newce.Element
					};
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
			return compNodeHash;
		}

		bool useEscape() { return (mFlags & FLAG_ESCAPE) != 0; }

		public override void Delete() {
			for (int i = 0; i < CompList.Count; i++) {
				CompList[i].Delete();
			}
			base.Delete();
		}
	}
}
