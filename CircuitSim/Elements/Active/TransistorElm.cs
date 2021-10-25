using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class TransistorElm : CircuitElm {
        const int FLAG_FLIP = 1;
        const int V_B = 0;
        const int V_C = 1;
        const int V_E = 2;

        const double VT = 0.025865;
        const double LEAKAGE = 1e-13; /* 1e-6; */
        const double VD_COEF = 1 / VT;
        const double R_GAIN = .5;
        const double INV_R_GAIN = 1 / R_GAIN;

        const int BODY_LEN = 16;
        const int HS = 16;

        double mHfe;
        double mFgain;
        double mInv_fgain;
        double mGmin;

        double mVcrit;
        double mLastVbc;
        double mLastVbe;

        double mIc;
        double mIe;
        double mIb;
        double mCurCount_c;
        double mCurCount_e;
        double mCurCount_b;

        Point[] mColl;
        Point[] mEmit;
        Point mTbase;

        Point[] mRectPoly;
        Point[] mArrowPoly;

        public TransistorElm(Point pos, bool pnpflag) : base(pos) {
            NPN = pnpflag ? -1 : 1;
            mHfe = 100;
            ReferenceName = "Tr";
            setup();
        }

        public TransistorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            NPN = st.nextTokenInt();
            mHfe = 100;
            try {
                mLastVbe = st.nextTokenDouble();
                mLastVbc = st.nextTokenDouble();
                Volts[V_B] = 0;
                Volts[V_C] = -mLastVbe;
                Volts[V_E] = -mLastVbc;
                mHfe = st.nextTokenDouble();
                ReferenceName = st.nextToken();
            } catch { }
            setup();
        }

        ///<summary>1 = NPN, -1 = PNP</summary>  
        public int NPN { get; private set; }

        public override bool CanViewInScope { get { return true; } }

        public override double Power {
            get { return (Volts[V_B] - Volts[V_E]) * mIb + (Volts[V_C] - Volts[V_E]) * mIc; }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSISTOR; } }

        protected override string dump() {
            return NPN
                + " " + (Volts[V_B] - Volts[V_C])
                + " " + (Volts[V_B] - Volts[V_E])
                + " " + mHfe
                + " " + ReferenceName;
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mColl[0] : mEmit[0];
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mIb;
            }
            if (n == 1) {
                return -mIc;
            }
            return -mIe;
        }

        public void SetHfe(double hfe) {
            mHfe = hfe;
            setup();
        }

        void setup() {
            mVcrit = VT * Math.Log(VT / (Math.Sqrt(2) * LEAKAGE));
            mFgain = mHfe / (mHfe + 1);
            mInv_fgain = 1 / mFgain;
            mNoDiagonal = true;
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[V_B]);
            mCir.StampNonLinear(Nodes[V_C]);
            mCir.StampNonLinear(Nodes[V_E]);
        }

        public override void DoStep() {
            double vbc = Volts[V_B] - Volts[V_C]; /* typically negative */
            double vbe = Volts[V_B] - Volts[V_E]; /* typically positive */
            if (Math.Abs(vbc - mLastVbc) > .01 || /* .01 */
                Math.Abs(vbe - mLastVbe) > .01) {
                mCir.Converged = false;
            }
            /* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
            mGmin = LEAKAGE * 0.01;
            if (mCir.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
                mGmin = Math.Exp(-9 * Math.Log(10) * (1 - mCir.SubIterations / 300.0));
                if (mGmin > .1) {
                    mGmin = .1;
                }
                /*Console.WriteLine("gmin " + gmin + " vbc " + vbc + " vbe " + vbe); */
            }

            /*Console.WriteLine("T " + vbc + " " + vbe + "\n"); */
            vbc = NPN * limitStep(NPN * vbc, NPN * mLastVbc);
            vbe = NPN * limitStep(NPN * vbe, NPN * mLastVbe);
            mLastVbc = vbc;
            mLastVbe = vbe;
            double pcoef = VD_COEF * NPN;
            double expbc = Math.Exp(vbc * pcoef);
            /*if (expbc > 1e13 || Double.isInfinite(expbc))
             * expbc = 1e13;*/
            double expbe = Math.Exp(vbe * pcoef);
            /*if (expbe > 1e13 || Double.isInfinite(expbe))
             * expbe = 1e13;*/
            mIe = NPN * LEAKAGE * (-mInv_fgain * (expbe - 1) + (expbc - 1));
            mIc = NPN * LEAKAGE * ((expbe - 1) - INV_R_GAIN * (expbc - 1));
            mIb = -(mIe + mIc);
            /*Console.WriteLine("gain " + ic/ib);
            Console.WriteLine("T " + vbc + " " + vbe + " " + ie + " " + ic + "\n"); */
            double gee = -LEAKAGE * VD_COEF * expbe * mInv_fgain;
            double gec = LEAKAGE * VD_COEF * expbc;
            double gce = -gee * mFgain;
            double gcc = -gec * INV_R_GAIN;

            /* add minimum conductance (gmin) between b,e and b,c */
            gcc -= mGmin;
            gee -= mGmin;

            /* stamps from page 302 of Pillage.
             * node 0 is the base,
             * node 1 the collector,
             * node 2 the emitter. */
            mCir.StampMatrix(Nodes[V_B], Nodes[V_B], -gee - gec - gce - gcc);
            mCir.StampMatrix(Nodes[V_B], Nodes[V_C], gec + gcc);
            mCir.StampMatrix(Nodes[V_B], Nodes[V_E], gee + gce);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_B], gce + gcc);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_C], -gcc);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_E], -gce);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_B], gee + gec);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_C], -gec);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_E], -gee);

            /* we are solving for v(k+1), not delta v, so we use formula
             * 10.5.13 (from Pillage), multiplying J by v(k) */

            mCir.StampRightSide(Nodes[V_B], -mIb - (gec + gcc) * vbc - (gee + gce) * vbe);
            mCir.StampRightSide(Nodes[V_C], -mIc + gce * vbe + gcc * vbc);
            mCir.StampRightSide(Nodes[V_E], -mIe + gee * vbe + gec * vbc);
        }

        double limitStep(double vnew, double vold) {
            double arg;
            double oo = vnew;

            if (vnew > mVcrit && Math.Abs(vnew - vold) > (VT + VT)) {
                if (vold > 0) {
                    arg = 1 + (vnew - vold) / VT;
                    if (arg > 0) {
                        vnew = vold + VT * Math.Log(arg);
                    } else {
                        vnew = mVcrit;
                    }
                } else {
                    vnew = VT * Math.Log(vnew / VT);
                }
                mCir.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            }
            return vnew;
        }

        public override void StepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mIc) > 1e12 || Math.Abs(mIb) > 1e12) {
                mCir.Stop("max current exceeded", this);
            }
        }

        public override void Reset() {
            Volts[V_B] = Volts[V_C] = Volts[V_E] = 0;
            mLastVbc = mLastVbe = mCurCount_c = mCurCount_e = mCurCount_b = 0;
        }

        public override void SetPoints() {
            base.SetPoints();

            if ((mFlags & FLAG_FLIP) != 0) {
                mDsign = -mDsign;
            }
            int hs2 = HS * mDsign * NPN;

            /* calc collector, emitter posts */
            mColl = new Point[2];
            mEmit = new Point[2];
            interpPointAB(ref mColl[0], ref mEmit[0], 1, hs2);

            /* calc rectangle edges */
            var rect = new Point[4];
            interpPointAB(ref rect[0], ref rect[1], 1 - BODY_LEN / mLen, HS);
            interpPointAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - 3) / mLen, HS);

            /* calc points where collector/emitter leads contact rectangle */
            interpPointAB(ref mColl[1], ref mEmit[1], 1 - (BODY_LEN - 3) / mLen, 6 * mDsign * NPN);

            /* calc point where base lead contacts rectangle */
            interpPoint(ref mTbase, 1 - BODY_LEN / mLen);

            /* rectangle */
            mRectPoly = new Point[] { rect[0], rect[2], rect[3], rect[1] };

            /* arrow */
            if (NPN == 1) {
                Utils.CreateArrow(mEmit[1], mEmit[0], out mArrowPoly, 8, 3);
            } else {
                var pt = new Point();
                interpPoint(ref pt, 1 - (BODY_LEN - 2) / mLen, -5 * mDsign * NPN);
                Utils.CreateArrow(mEmit[0], pt, out mArrowPoly, 8, 3);
            }

            setTextPos();
        }

        void setTextPos() {
            var txtW = Context.GetTextSize(ReferenceName).Width;
            var swap = 0 < (mFlags & FLAG_FLIP) ? -1 : 1;
            mNameV = mPoint1.Y == mPoint2.Y;
            if (mNameV) {
                if (0 < mDsign * swap) {
                    mNamePos = new Point(mPoint2.X - 1, mPoint2.Y);
                } else {
                    mNamePos = new Point(mPoint2.X - 17, mPoint2.Y);
                }
            } else if (mPoint1.X == mPoint2.X) {
                mNamePos = new Point(mPoint2.X - (int)(txtW / 2), mPoint2.Y + HS * swap * mDsign * 2 / 3);
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, 16);

            /* draw collector */
            drawVoltage(V_C, mColl[0], mColl[1]);
            /* draw emitter */
            drawVoltage(V_E, mEmit[0], mEmit[1]);
            /* draw arrow */
            fillVoltage(V_E, mArrowPoly);
            /* draw base */
            drawVoltage(V_B, mPoint1, mTbase);

            /* draw dots */
            mCurCount_b = updateDotCount(-mIb, mCurCount_b);
            drawDots(mTbase, mPoint1, mCurCount_b);
            mCurCount_c = updateDotCount(-mIc, mCurCount_c);
            drawDots(mColl[1], mColl[0], mCurCount_c);
            mCurCount_e = updateDotCount(-mIe, mCurCount_e);
            drawDots(mEmit[1], mEmit[0], mCurCount_e);

            /* draw base rectangle */
            fillVoltage(V_B, mRectPoly);

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                if (mNameV) {
                    g.DrawCenteredVText(ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawLeftText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override string GetScopeText(Scope.VAL x) {
            string t = "";
            switch (x) {
            case Scope.VAL.VBE: t = "Vbe"; break;
            case Scope.VAL.VBC: t = "Vbc"; break;
            case Scope.VAL.VCE: t = "Vce"; break;
            }
            return "transistor, " + t;
        }

        public override double GetScopeValue(Scope.VAL x) {
            switch (x) {
            case Scope.VAL.VBE:
                return Volts[V_B] - Volts[V_E];
            case Scope.VAL.VBC:
                return Volts[V_B] - Volts[V_C];
            case Scope.VAL.VCE:
                return Volts[V_C] - Volts[V_E];
            }
            return 0;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "transistor (" + ((NPN == -1) ? "PNP)" : "NPN)") + " hfe=" + mHfe.ToString("0.000");
            double vbc = Volts[V_B] - Volts[V_C];
            double vbe = Volts[V_B] - Volts[V_E];
            double vce = Volts[V_C] - Volts[V_E];
            if (vbc * NPN > .2) {
                arr[1] = vbe * NPN > .2 ? "saturation" : "reverse active";
            } else {
                arr[1] = vbe * NPN > .2 ? "fwd active" : "cutoff";
            }
            arr[1] = arr[1];
            arr[2] = "Ic = " + Utils.CurrentText(mIc);
            arr[3] = "Ib = " + Utils.CurrentText(mIb);
            arr[4] = "Vbe = " + Utils.VoltageText(vbe);
            arr[5] = "Vbc = " + Utils.VoltageText(vbc);
            arr[6] = "Vce = " + Utils.VoltageText(vce);
            arr[7] = "P = " + Utils.UnitText(Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("hfe", mHfe, 10, 1000).SetDimensionless();
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "エミッタ/コレクタ 入れ替え";
                ei.CheckBox.Checked = (mFlags & FLAG_FLIP) != 0;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 1) {
                mHfe = ei.Value;
                setup();
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP;
                } else {
                    mFlags &= ~FLAG_FLIP;
                }
                SetPoints();
            }
        }
    }
}
