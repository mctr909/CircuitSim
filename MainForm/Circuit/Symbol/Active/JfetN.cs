using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class JfetN : FET {
		private ElmJFET mElm;

		protected JfetN(Point pos, bool isNch) : base(pos, isNch, false) { }

		public JfetN(Point pos) : base(pos, true, false) { }

		public override void Reset() {
			base.Reset();
			mElm.mDiodeLastVdiff = 0.0;
			mElm.mGateCurrent = 0.0;
		}

		public override void Stamp() {
			base.Stamp();
			if (mElm.Nch < 0) {
				mElm.mDiodeNodesA = mElm.Nodes[ElmFET.IdxS];
				mElm.mDiodeNodesB = mElm.Nodes[ElmFET.IdxG];
			} else {
				mElm.mDiodeNodesA = mElm.Nodes[ElmFET.IdxG];
				mElm.mDiodeNodesB = mElm.Nodes[ElmFET.IdxS];
			}
			StampNonLinear(mElm.mDiodeNodesA);
			StampNonLinear(mElm.mDiodeNodesB);
		}
	}
}
