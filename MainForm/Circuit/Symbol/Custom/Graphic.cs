using Circuit.Elements.Custom;

namespace Circuit.Symbol.Custom {
	class Graphic : BaseSymbol {
		ElmGraphic mElm;

		public override BaseElement Element { get { return mElm; } }

		public Graphic(Point pos) : base(pos) {
			mElm = new ElmGraphic();
		}

		public Graphic(Point a, Point b, int flags) : base(a, b, flags) {
			mElm = new ElmGraphic();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVALID; } }
	}
}
