using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Wire : BaseSymbol {
		public Wire(Point pos) : base(pos) {
			Elm = new ElmWire();
		}

		public Wire(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			Elm = new ElmWire();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.WIRE; } }

		public override void SetPoints() {
			base.SetPoints();
		}

		public override void Draw(CustomGraphics g) {
			drawLine(Post.A, Post.B);
			doDots();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ワイヤ";
			arr[1] = "電流：" + Utils.CurrentAbsText(Elm.Current);
			arr[2] = "電位：" + Utils.VoltageText(Elm.VoltageDiff);
		}
	}
}
