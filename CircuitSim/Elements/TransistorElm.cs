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

        static readonly double vt = 0.025865;
        static readonly double leakage = 1e-13; /* 1e-6; */
        static readonly double vdcoef = 1 / vt;
        static readonly double rgain = .5;
        static readonly double inv_rgain = 1 / rgain;

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
                Volts[0] = 0;
                Volts[1] = -lastvbe;
                Volts[2] = -lastvbc;
                beta = st.nextTokenDouble();
            } catch { }
            setup();
        }

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
                cir.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            }
            return vnew;
        }

        public override bool nonLinear() { return true; }

        public override void reset() {
            Volts[0] = Volts[1] = Volts[2] = 0;
            lastvbc = lastvbe = curcount_c = curcount_e = curcount_b = 0;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.TRANSISTOR; }

        public override string dump() {
            return base.dump()
                + " " + pnp
                + " " + (Volts[0] - Volts[1])
                + " " + (Volts[0] - Volts[2])
                + " " + beta;
        }

        public override void draw(Graphics g) {
            setBbox(mPoint1, mPoint2, 16);

            /* draw collector */
            drawThickLine(g, getVoltageColor(Volts[1]), coll[0], coll[1]);
            /* draw emitter */
            drawThickLine(g, getVoltageColor(Volts[2]), emit[0], emit[1]);
            /* draw arrow */
            fillPolygon(g, getVoltageColor(Volts[2]), arrowPoly);
            /* draw base */
            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, tbase);

            /* draw dots */
            curcount_b = updateDotCount(-ib, curcount_b);
            drawDots(g, tbase, mPoint1, curcount_b);
            curcount_c = updateDotCount(-ic, curcount_c);
            drawDots(g, coll[1], coll[0], curcount_c);
            curcount_e = updateDotCount(-ie, curcount_e);
            drawDots(g, emit[1], emit[0], curcount_e);

            /* draw base rectangle */
            fillPolygon(g, getVoltageColor(Volts[0]), rectPoly);

            if ((needsHighlight() || sim.dragElm == this) && mDy == 0) {
                /* IES */
                int ds = Math.Sign(mDx);
                g.DrawString("B", FONT_TERM_NAME, BRUSH_TERM_NAME, tbase.X - 10 * ds, tbase.Y - 5);
                g.DrawString("C", FONT_TERM_NAME, BRUSH_TERM_NAME, coll[0].X - 3 + 9 * ds, coll[0].Y + 4); /* x+6 if ds=1, -12 if -1 */
                g.DrawString("E", FONT_TERM_NAME, BRUSH_TERM_NAME, emit[0].X - 3 + 9 * ds, emit[0].Y + 4);
            }
            drawPosts(g);
        }

        public override Point getPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? coll[0] : emit[0];
        }

        public override int getPostCount() { return 3; }

        public override double getPower() {
            return (Volts[0] - Volts[2]) * ib + (Volts[1] - Volts[2]) * ic;
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
            interpPoint(mPoint1, mPoint2, ref rect[0], ref rect[1], 1 - 16 / mElmLen, hs);
            interpPoint(mPoint1, mPoint2, ref rect[2], ref rect[3], 1 - 13 / mElmLen, hs);

            /* calc points where collector/emitter leads contact rectangle */
            interpPoint(mPoint1, mPoint2, ref coll[1], ref emit[1], 1 - 13 / mElmLen, 6 * mDsign * pnp);

            /* calc point where base lead contacts rectangle */
            tbase = new Point();
            interpPoint(mPoint1, mPoint2, ref tbase, 1 - 16 / mElmLen);

            /* rectangle */
            rectPoly = createPolygon(rect[0], rect[2], rect[3], rect[1]).ToArray();

            /* arrow */
            if (pnp == 1) {
                arrowPoly = calcArrow(emit[1], emit[0], 8, 4).ToArray();
            } else {
                var pt = interpPoint(mPoint1, mPoint2, 1 - 11 / mElmLen, -5 * mDsign * pnp);
                arrowPoly = calcArrow(emit[0], pt, 8, 4).ToArray();
            }
        }

        public override void stamp() {
            cir.StampNonLinear(Nodes[0]);
            cir.StampNonLinear(Nodes[1]);
            cir.StampNonLinear(Nodes[2]);
        }

        public override void doStep() {
            double vbc = Volts[0] - Volts[1]; /* typically negative */
            double vbe = Volts[0] - Volts[2]; /* typically positive */
            if (Math.Abs(vbc - lastvbc) > .01 || /* .01 */
                Math.Abs(vbe - lastvbe) > .01) {
                cir.Converged = false;
            }
            /* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
            gmin = leakage * 0.01;
            if (cir.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
                gmin = Math.Exp(-9 * Math.Log(10) * (1 - cir.SubIterations / 300.0));
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
            cir.StampMatrix(Nodes[0], Nodes[0], -gee - gec - gce - gcc);
            cir.StampMatrix(Nodes[0], Nodes[1], gec + gcc);
            cir.StampMatrix(Nodes[0], Nodes[2], gee + gce);
            cir.StampMatrix(Nodes[1], Nodes[0], gce + gcc);
            cir.StampMatrix(Nodes[1], Nodes[1], -gcc);
            cir.StampMatrix(Nodes[1], Nodes[2], -gce);
            cir.StampMatrix(Nodes[2], Nodes[0], gee + gec);
            cir.StampMatrix(Nodes[2], Nodes[1], -gec);
            cir.StampMatrix(Nodes[2], Nodes[2], -gee);

            /* we are solving for v(k+1), not delta v, so we use formula
             * 10.5.13 (from Pillage), multiplying J by v(k) */

            cir.StampRightSide(Nodes[0], -ib - (gec + gcc) * vbc - (gee + gce) * vbe);
            cir.StampRightSide(Nodes[1], -ic + gce * vbe + gcc * vbc);
            cir.StampRightSide(Nodes[2], -ie + gee * vbe + gec * vbc);
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
            double vbc = Volts[0] - Volts[1];
            double vbe = Volts[0] - Volts[2];
            double vce = Volts[1] - Volts[2];
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
            arr[7] = "P = " + getUnitText(getPower(), "W");
        }

        public override double getScopeValue(int x) {
            switch (x) {
            case Scope.VAL_IB: return ib;
            case Scope.VAL_IC: return ic;
            case Scope.VAL_IE: return ie;
            case Scope.VAL_VBE: return Volts[0] - Volts[2];
            case Scope.VAL_VBC: return Volts[0] - Volts[1];
            case Scope.VAL_VCE: return Volts[1] - Volts[2];
            case Scope.VAL_POWER: return getPower();
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
                return new EditInfo("Beta/hFE", beta, 10, 1000).setDimensionless();
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Swap E/C";
                ei.checkbox.Checked = (mFlags & FLAG_FLIP) != 0;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                beta = ei.value;
                setup();
            }
            if (n == 1) {
                if (ei.checkbox.Checked) {
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
                cir.Stop("max current exceeded", this);
            }
        }

        public override bool canViewInScope() { return true; }

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
