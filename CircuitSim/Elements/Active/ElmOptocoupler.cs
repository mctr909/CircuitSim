using Circuit.Elements.Input;
using Circuit.Elements.Custom;

namespace Circuit.Elements.Active {
    class ElmOptocoupler : ElmComposite {
        public int mCspc;
        public int mCspc2;
        public double[] mCurCounts;

        public ElmDiode Diode;
        public ElmTransistor Transistor;

        protected override void Init(string expr) {
            mCspc = 8 * 2;
            mCspc2 = mCspc * 2;
            Diode = (ElmDiode)CompList[0];

            var cccs = (ElmCCCS)CompList[1];
            cccs.SetExpr(expr);
            Transistor = (ElmTransistor)CompList[2];
            Transistor.SetHfe(700);
            mCurCounts = new double[4];
        }

        public override void Reset() {
            base.Reset();
            mCurCounts = new double[4];
        }

        public override bool AnaGetConnection(int n1, int n2) {
            return n1 / 2 == n2 / 2;
        }
    }
}
