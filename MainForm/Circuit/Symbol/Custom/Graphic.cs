using Circuit.Elements.Custom;
using Circuit.Elements;

namespace Circuit.Symbol.Custom {
	class Graphic : BaseSymbol {
		public Graphic(Point pos) : base(pos) { }

		public Graphic(Point a, Point b, int flags) : base(a, b, flags) { }

		protected override BaseElement Create() {
			return new ElmGraphic();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVALID; } }
	}
}
