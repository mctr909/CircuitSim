﻿using Circuit.Elements.Input;
using Circuit.Elements.Custom;

namespace Circuit.Elements.Active {
    class OptocouplerElm : CompositeElm {
        static readonly int[] EXTERNAL_NODES = { 6, 2, 4, 5 };
        static readonly string MODEL_STRING
            = ELEMENTS.DIODE + " 6 1\r"
            + ELEMENTS.CCCS +" 1 2 3 4\r"
            + ELEMENTS.TRANSISTOR_N + " 3 4 5";

        public int mCspc;
        public int mCspc2;
        public double[] mCurCounts;

        public DiodeUI mDiode;
        public TransistorUI mTransistor;

        public OptocouplerElm() : base(MODEL_STRING, EXTERNAL_NODES) {
            initOptocoupler();
        }

        public OptocouplerElm(StringTokenizer st) : base(st, MODEL_STRING, EXTERNAL_NODES) {
            initOptocoupler();
        }

        public override bool GetConnection(int n1, int n2) {
            return n1 / 2 == n2 / 2;
        }

        void initOptocoupler() {
            mCspc = 8 * 2;
            mCspc2 = mCspc * 2;
            mDiode = (DiodeUI)compElmList[0];

            var cccs = (CCCSElm)compElmList[1].Elm;
            cccs.SetExpr(@"max(0,
                min(.0001,
                    select {i-0.003,
                        ( -80000000000*i^5 +800000000*i^4 -3000000*i^3 +5177.20*i^2 +0.2453*i -0.00005 )*1.040/700,
                        (      9000000*i^5    -998113*i^4   +42174*i^3  -861.32*i^2 +9.0836*i -0.00780 )*0.945/700
                    }
                )
            )");

            mTransistor = (TransistorUI)compElmList[2];
            ((TransistorElm)mTransistor.Elm).SetHfe(700);
            mCurCounts = new double[4];
            mDiode.ReferenceName = "";
            mTransistor.ReferenceName = "";
        }

        public override void Reset() {
            base.Reset();
            mCurCounts = new double[4];
        }
    }
}
