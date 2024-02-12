using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Wire : BaseSymbol {
		ElmWire mElm;

		public override BaseElement Element { get { return mElm; } }

		public Wire(Point pos) : base(pos) {
			mElm = new ElmWire();
		}

		public Wire(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmWire();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.WIRE; } }

		public override void SetPoints() {
			base.SetPoints();
		}

		public override void Draw(CustomGraphics g) {
			DrawLine(Post.A, Post.B);
			DoDots();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ワイヤ";
			arr[1] = "電流：" + Utils.CurrentAbsText(mElm.Current);
			arr[2] = "電位：" + Utils.VoltageText(mElm.VoltageDiff);
		}
	}
}
