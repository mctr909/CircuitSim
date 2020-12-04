using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class TransistorElm : CircuitElm {
        /* node 0 = base
         * node 1 = collector
         * node 2 = emitter */
        public int pnp { get; private set; }
        double beta;
        double fgain, inv_fgain;
        double gmin;
        readonly int FLAG_FLIP = 1;

        double vcrit;
        double lastvbc;
        double lastvbe;

        double ic;
        double ie;
        double ib;
        double curcount_c;
        double curcount_e;
        double curcount_b;

        Point[] rect;
        Point[] coll;
        Point[] emit;
        Point tbase;

        Point[] rectPoly;
        Point[] arrowPoly;

        const int V_B = 0;
        const int V_C = 1;
        const int V_E = 2;

        const double vt = 0.025865;
        const double leakage = 1e-13; /* 1e-6; */
        const double vdcoef = 1 / vt;
        const double rgain = .5;
        const double inv_rgain = 1 / rgain;

        public TransistorElm(int xx, int yy, bool pnpflag) : base(xx, yy) {
            pnp = pnpflag ? -1 : 1;
            beta = 100;
            setup();
        }

        public TransistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            pnp = st.nextTokenInt();
            beta = 100;
            try {
                lastvbe = st.nextTokenDouble();
                lastvbc = st.nextTokenDouble();
                Volts[V_B] = 0;
                Volts[V_C] = -lastvbe;
                Volts[V_E] = -lastvbc;
                beta = st.nextTokenDouble();
            } catch { }
            setup();
        }

        public override bool CanViewInScope { get { return true; } }

        public override double Power {
            get { return (Volts[V_B] - Volts[V_E]) * ib + (Volts[V_C] - Volts[V_E]) * ic; }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        protected override string dump() {
            return pnp
                + " " + (Volts[V_B] - Volts[V_C])
                + " " + (Volts[V_B] - Volts[V_E])
                + " " + beta;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.TRANSISTOR; }

        public void setBeta(double b) {
            beta = b;
            setup();
        }

        void setup() {
            vcrit = vt * Math.Log(vt / (Math.Sqrt(2) * leakage));
            fgain = beta / (beta + 1);
            inv_fgain = 1 / fgain;
            mNoDiagonal = true;
        }

        double limitStep(double vnew, double vold) {
            double arg;
            double oo = vnew;

            if (vnew > vcrit && Math.Abs(vnew - vold) > (vt + vt)) {
                if (vold > 0) {
                    arg = 1 + (vnew - vold) / vt;
                    if (arg > 0) {
                        vnew = vold + vt * Math.Log(arg);
                    } else {
                        vnew = vcrit;
                    }
                } else {
                    vnew = vt * Math.Log(vnew / vt);
                }
                Cir.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            }
            return vnew;
        }

        public override void reset() {
            Volts[V_B] = Volts[V_C] = Volts[V_E] = 0;
            lastvbc = lastvbe = curcount_c = curcount_e = curcount_b = 0;
        }

        public override void draw(Graphics g) {
            setBbox(mPoint1, mPoint2, 16);
           
            /* draw collector */
            drawThickLine(g, getVoltageColor(Volts[V_C]), coll[0], coll[1]);
            /* draw emitter */
            drawThickLine(g, getVoltageColor(Volts[V_E]), emit[0], emit[1]);
            /* draw arrow */
            fillPolygon(g, getVoltageColor(Volts[V_E]), arrowPoly);
            /* draw base */
            drawThickLine(g, getVoltageColor(Volts[V_B]), mPoint1, tbase);

            /* draw dots */
            curcount_b = updateDotCount(-ib, curcount_b);
            drawDots(g, tbase, mPoint1, curcount_b);
            curcount_c = updateDotCount(-ic, curcount_c);
            drawDots(g, coll[1], coll[0], curcount_c);
            curcount_e = updateDotCount(-ie, curcount_e);
            drawDots(g, emit[1], emit[0], curcount_e);

            /* draw base rectangle */
            fillPolygon(g, getVoltageColor(Volts[V_B]), rectPoly);

            drawPosts(g);
        }

        public override Point getPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? coll[0] : emit[0];
        }

        public override void setPoints() {
            base.setPoints();
            int hs = 16;
            if ((mFlags & FLAG_FLIP) != 0) {
                mDsign = -mDsign;
            }
            int hs2 = hs * mDsign * pnp;

            /* calc collector, emitter posts */
            coll = newPointArray(2);
            emit = newPointArray(2);
            interpPoint(mPoint1, mPoint2, ref coll[0], ref emit[0], 1, hs2);

            /* calc rectangle edges */
            rect = newPointArray(4);
            interpPoint(mPoint1, mPoint2, ref rect[0], ref rect[1], 1 - 16 / mLen, hs);
            interpPoint(mPoint1, mPoint2, ref rect[2], ref rect[3], 1 - 13 / mLen, hs);

            /* calc points where collector/emitter leads contact rectangle */
            interpPoint(mPoint1, mPoint2, ref coll[1], ref emit[1], 1 - 13 / mLen, 6 * mDsign * pnp);

            /* calc point where base lead contacts rectangle */
            interpPoint(mPoint1, mPoint2, ref tbase, 1 - 16 / mLen);

            /* rectangle */
            rectPoly = createPolygon(rect[0], rect[2], rect[3], rect[1]).ToArray();

            /* arrow */
            if (pnp == 1) {
                arrowPoly = calcArrow(emit[1], emit[0], 8, 4).ToArray();
            } else {
                var pt = interpPoint(mPoint1, mPoint2, 1 - 11 / mLen, -5 * mDsign * pnp);
                arrowPoly = calcArrow(emit[0], pt, 8, 4).ToArray();
            }
        }

        public override void stamp() {
            Cir.StampNonLinear(Nodes[V_B]);
            Cir.StampNonLinear(Nodes[V_C]);
            Cir.StampNonLinear(Nodes[V_E]);
        }

        public override void doStep() {
            double vbc = Volts[V_B] - Volts[V_C]; /* typically negative */
            double vbe = Volts[V_B] - Volts[V_E]; /* typically positive */
            if (Math.Abs(vbc - lastvbc) > .01 || /* .01 */
                Math.Abs(vbe - lastvbe) > .01) {
                Cir.Converged = false;
            }
            /* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
            gmin = leakage * 0.01;
            if (Cir.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
                gmin = Math.Exp(-9 * Math.Log(10) * (1 - Cir.SubIterations / 300.0));
                if (gmin > .1) {
                    gmin = .1;
                }
                /*Console.WriteLine("gmin " + gmin + " vbc " + vbc + " vbe " + vbe); */
            }

            /*Console.WriteLine("T " + vbc + " " + vbe + "\n"); */
            vbc = pnp * limitStep(pnp * vbc, pnp * lastvbc);
            vbe = pnp * limitStep(pnp * vbe, pnp * lastvbe);
            lastvbc = vbc;
            lastvbe = vbe;
            double pcoef = vdcoef * pnp;
            double expbc = Math.Exp(vbc * pcoef);
            /*if (expbc > 1e13 || Double.isInfinite(expbc))
             * expbc = 1e13;*/
            double expbe = Math.Exp(vbe * pcoef);
            /*if (expbe > 1e13 || Double.isInfinite(expbe))
             * expbe = 1e13;*/
            ie = pnp * leakage * (-inv_fgain * (expbe - 1) + (expbc - 1));
            ic = pnp * leakage * ((expbe - 1) - inv_rgain * (expbc - 1));
            ib = -(ie + ic);
            /*Console.WriteLine("gain " + ic/ib);
            Console.WriteLine("T " + vbc + " " + vbe + " " + ie + " " + ic + "\n"); */
            double gee = -leakage * vdcoef * expbe * inv_fgain;
            double gec = leakage * vdcoef * expbc;
            double gce = -gee * fgain;
            double gcc = -gec * inv_rgain;

            /* add minimum conductance (gmin) between b,e and b,c */
            gcc -= gmin;
            gee -= gmin;

            /* stamps from page 302 of Pillage.
             * node 0 is the base,
             * node 1 the collector,
             * node 2 the emitter. */
            Cir.StampMatrix(Nodes[V_B], Nodes[V_B], -gee - gec - gce - gcc);
            Cir.StampMatrix(Nodes[V_B], Nodes[V_C], gec + gcc);
            Cir.StampMatrix(Nodes[V_B], Nodes[V_E], gee + gce);
            Cir.StampMatrix(Nodes[V_C], Nodes[V_B], gce + gcc);
            Cir.StampMatrix(Nodes[V_C], Nodes[V_C], -gcc);
            Cir.StampMatrix(Nodes[V_C], Nodes[V_E], -gce);
            Cir.StampMatrix(Nodes[V_E], Nodes[V_B], gee + gec);
            Cir.StampMatrix(Nodes[V_E], Nodes[V_C], -gec);
            Cir.StampMatrix(Nodes[V_E], Nodes[V_E], -gee);

            /* we are solving for v(k+1), not delta v, so we use formula
             * 10.5.13 (from Pillage), multiplying J by v(k) */

            Cir.StampRightSide(Nodes[V_B], -ib - (gec + gcc) * vbc - (gee + gce) * vbe);
            Cir.StampRightSide(Nodes[V_C], -ic + gce * vbe + gcc * vbc);
            Cir.StampRightSide(Nodes[V_E], -ie + gee * vbe + gec * vbc);
        }

        public override string getScopeText(int x) {
            string t = "";
            switch (x) {
            case Scope.VAL_IB: t = "Ib"; break;
            case Scope.VAL_IC: t = "Ic"; break;
            case Scope.VAL_IE: t = "Ie"; break;
            case Scope.VAL_VBE: t = "Vbe"; break;
            case Scope.VAL_VBC: t = "Vbc"; break;
            case Scope.VAL_VCE: t = "Vce"; break;
            case Scope.VAL_POWER: t = "P"; break;
            }
            return "transistor, " + t;
        }

        public override void getInfo(string[] arr) {
            arr[0] = "transistor (" + ((pnp == -1) ? "PNP)" : "NPN)") + " β=" + beta.ToString("0.000");
            double vbc = Volts[V_B] - Volts[V_C];
            double vbe = Volts[V_B] - Volts[V_E];
            double vce = Volts[V_C] - Volts[V_E];
            if (vbc * pnp > .2) {
                arr[1] = vbe * pnp > .2 ? "saturation" : "reverse active";
            } else {
                arr[1] = vbe * pnp > .2 ? "fwd active" : "cutoff";
            }
            arr[1] = arr[1];
            arr[2] = "Ic = " + getCurrentText(ic);
            arr[3] = "Ib = " + getCurrentText(ib);
            arr[4] = "Vbe = " + getVoltageText(vbe);
            arr[5] = "Vbc = " + getVoltageText(vbc);
            arr[6] = "Vce = " + getVoltageText(vce);
            arr[7] = "P = " + getUnitText(Power, "W");
        }

        public override double getScopeValue(int x) {
            switch (x) {
            case Scope.VAL_IB: return ib;
            case Scope.VAL_IC: return ic;
            case Scope.VAL_IE: return ie;
            case Scope.VAL_VBE: return Volts[V_B] - Volts[V_E];
            case Scope.VAL_VBC: return Volts[V_B] - Volts[V_C];
            case Scope.VAL_VCE: return Volts[V_C] - Volts[V_E];
            case Scope.VAL_POWER: return Power;
            }
            return 0;
        }

        public override int getScopeUnits(int x) {
            switch (x) {
            case Scope.VAL_IB:
            case Scope.VAL_IC:
            case Scope.VAL_IE: return Scope.UNITS_A;
            case Scope.VAL_POWER: return Scope.UNITS_W;
            default: return Scope.UNITS_V;
            }
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Beta/hFE", beta, 10, 1000).SetDimensionless();
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Swap E/C";
                ei.CheckBox.Checked = (mFlags & FLAG_FLIP) != 0;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                beta = ei.Value;
                setup();
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP;
                } else {
                    mFlags &= ~FLAG_FLIP;
                }
                setPoints();
            }
        }

        public override void stepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(ic) > 1e12 || Math.Abs(ib) > 1e12) {
                Cir.Stop("max current exceeded", this);
            }
        }

        public override double getCurrentIntoNode(int n) {
            if (n == 0) {
                return -ib;
            }
            if (n == 1) {
                return -ic;
            }
            return -ie;
        }
    }
}
