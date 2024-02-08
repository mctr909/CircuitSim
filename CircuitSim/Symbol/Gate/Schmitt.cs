using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.Symbol.Gate {
	class Schmitt : InvertingSchmitt {
		ElmSchmitt mElm;

		public Schmitt(Point pos) : base(pos) {
			mElm = new ElmSchmitt();
			Elm = mElm;
		}

		public Schmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmSchmitt(st);
			Elm = mElm;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SCHMITT; } }

		public override void Draw(CustomGraphics g) {
			draw2Leads();
			drawPolygon(gatePoly);
			drawPolygon(symbolPoly);
			updateDotCount(mElm.Current, ref mCurCount);
			drawCurrentB(mCurCount);
		}

		public override void SetPoints() {
			base.SetPoints();
			int hs = 10;
			int ww = 12;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			setLead1(0.5 - ww / Post.Len);
			setLead2(0.5 + (ww - 4) / Post.Len);
			gatePoly = new PointF[3];
			interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
			interpPost(ref gatePoly[2], 0.5 + (ww - 2) / Post.Len);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "Schmitt Trigger~"; // ~ is for localization
		}
	}
}
