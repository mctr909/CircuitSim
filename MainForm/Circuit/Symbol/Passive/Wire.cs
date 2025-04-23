using Circuit.Elements.Passive;
using Circuit.Elements;

namespace Circuit.Symbol.Passive {
	public class Wire : BaseSymbol {
		public override bool IsWire { get { return true; } }

		public bool HasWireInfo; /* used in CirSim to calculate wire currents */

		public Wire(Point pos) : base(pos) { }

		public Wire(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) { }

		protected override BaseElement Create() {
			return new ElmWire();
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
			arr[1] = "電流：" + TextUtils.CurrentAbs(Element.I[0]);
			arr[2] = "電位：" + TextUtils.Voltage(Element.VoltageDiff);
		}
	}
}
