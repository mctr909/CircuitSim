﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class MosfetElm : CircuitElm {
        const int FLAG_PNP = 1;
        const int FLAG_SHOWVT = 2;
        const int FLAG_DIGITAL = 4;
        const int FLAG_FLIP = 8;
        const int FLAG_HIDE_BULK = 16;
        const int FLAG_BODY_DIODE = 32;
        const int FLAG_BODY_TERMINAL = 64;
        const int FLAGS_GLOBAL = (FLAG_HIDE_BULK | FLAG_DIGITAL);

        const int V_G = 0;
        const int V_S = 1;
        const int V_D = 2;

        const int hs = 16;

        int pnp;
        int bodyTerminal;

        double vt;
        /* beta = 1/(RdsON*(Vgs-Vt)) */
        double beta;
        static int globalFlags;
        Diode diodeB1;
        Diode diodeB2;
        double diodeCurrent1;
        double diodeCurrent2;
        double bodyCurrent;
        double curcount_body1;
        double curcount_body2;
        static double lastBeta;

        int pcircler;

        /* points for source and drain (these are swapped on PNP mosfets) */
        Point[] src;
        Point[] drn;

        /* points for gate, body, and the little circle on PNP mosfets */
        Point[] gate;
        Point[] body;
        Point pcircle;
        Point[] arrowPoly;

        Point ps1;
        Point ps2;

        double lastv0;
        double lastv1;
        double lastv2;
        double ids;
        int mode = 0;
        double gm = 0;

        public MosfetElm(int xx, int yy, bool pnpflag) : base(xx, yy) {
            pnp = pnpflag ? -1 : 1;
            mFlags = pnpflag ? FLAG_PNP : 0;
            mFlags |= FLAG_BODY_DIODE;
            mNoDiagonal = true;
            setupDiodes();
            beta = DefaultBeta;
            vt = DefaultThreshold;
        }

        public MosfetElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            pnp = ((f & FLAG_PNP) != 0) ? -1 : 1;
            mNoDiagonal = true;
            setupDiodes();
            vt = DefaultThreshold;
            beta = BackwardCompatibilityBeta;
            try {
                vt = st.nextTokenDouble();
                beta = st.nextTokenDouble();
            } catch { }
            globalFlags = mFlags & (FLAGS_GLOBAL);
            allocNodes(); /* make sure volts[] has the right number of elements when hasBodyTerminal() is true */
        }

        public override double Current { get { return ids; } }

        public override double VoltageDiff { get { return Volts[V_D] - Volts[V_S]; } }

        public override double Power {
            get {
                return ids * (Volts[V_D] - Volts[V_S])
                    - diodeCurrent1 * (Volts[V_S] - Volts[bodyTerminal])
                    - diodeCurrent2 * (Volts[V_D] - Volts[bodyTerminal]);
            }
        }

        public override bool CanViewInScope { get { return true; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return HasBodyTerminal ? 4 : 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.MOSFET; } }

        protected override string dump() {
            return vt + " " + beta;
        }

        double DefaultThreshold { get { return 1.5; } }

        /* default beta for new elements */
        double DefaultBeta {
            get { return lastBeta == 0 ? BackwardCompatibilityBeta : lastBeta; }
        }

        /* default for elements in old files with no configurable beta.
         * JfetElm overrides this.
         * Not sure where this value came from, but the ZVP3306A has a beta of about .027.
         * Power MOSFETs have much higher betas (like 80 or more) */
        double BackwardCompatibilityBeta { get { return .02; } }

        bool DrawDigital { get { return (mFlags & FLAG_DIGITAL) != 0; } }

        bool ShowBulk { get { return (mFlags & (FLAG_DIGITAL | FLAG_HIDE_BULK)) == 0; } }

        bool HasBodyTerminal { get { return (mFlags & FLAG_BODY_TERMINAL) != 0; } }

        bool DoBodyDiode { get { return (mFlags & FLAG_BODY_DIODE) != 0 && ShowBulk; } }

        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            diodeB1 = new Diode(Sim, mCir);
            diodeB1.setupForDefaultModel();
            /* diode from node 2 to body terminal */
            diodeB2 = new Diode(Sim, mCir);
            diodeB2.setupForDefaultModel();
        }

        public override void Reset() {
            lastv1 = lastv2 = 0;
            Volts[V_G] = Volts[V_S] = Volts[V_D] = 0;
            mCurCount = 0;
            diodeB1.reset();
            diodeB2.reset();
        }

        public override void Draw(CustomGraphics g) {
            /* pick up global flags changes */
            if ((mFlags & FLAGS_GLOBAL) != globalFlags) {
                SetPoints();
            }

            setBbox(mPoint1, mPoint2, hs);

            /* draw source/drain terminals */
            g.DrawThickLine(getVoltageColor(Volts[V_S]), src[0], src[1]);
            g.DrawThickLine(getVoltageColor(Volts[V_D]), drn[0], drn[1]);

            /* draw line connecting source and drain */
            int segments = 6;
            int i;
            double segf = 1.0/ segments;
            bool enhancement = vt > 0 && ShowBulk;
            for (i = 0; i != segments; i++) {
                if ((i == 1 || i == 4) && enhancement) {
                    continue;
                }
                Utils.InterpPoint(src[1], drn[1], ref ps1, i * segf);
                Utils.InterpPoint(src[1], drn[1], ref ps2, (i + 1) * segf);
                double v = Volts[V_S] + (Volts[V_D] - Volts[V_S]) * i / segments;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickLine(ps1, ps2);
            }

            /* draw little extensions of that line */
            g.DrawThickLine(getVoltageColor(Volts[V_S]), src[1], src[2]);
            g.DrawThickLine(getVoltageColor(Volts[V_D]), drn[1], drn[2]);

            /* draw bulk connection */
            if (ShowBulk) {
                g.ThickLineColor = getVoltageColor(Volts[bodyTerminal]);
                if (!HasBodyTerminal) {
                    g.DrawThickLine(pnp == -1 ? drn[0] : src[0], body[0]);
                }
                g.DrawThickLine(body[0], body[1]);
            }

            /* draw arrow */
            if (!DrawDigital) {
                g.FillPolygon(getVoltageColor(Volts[bodyTerminal]), arrowPoly);
            }

            /* draw gate */
            g.ThickLineColor = getVoltageColor(Volts[V_G]);
            g.DrawThickLine(mPoint1, gate[1]);
            g.DrawThickLine(gate[0], gate[2]);
            if (DrawDigital && pnp == -1) {
                g.DrawThickCircle(pcircle, pcircler);
            }

            if ((mFlags & FLAG_SHOWVT) != 0) {
                string s = "" + (vt * pnp);
                drawCenteredLText(g, s, X2 + 2, Y2, false);
            }
            mCurCount = updateDotCount(-ids, mCurCount);
            drawDots(g, src[0], src[1], mCurCount);
            drawDots(g, drn[1], drn[0], mCurCount);
            drawDots(g, src[1], drn[1], mCurCount);

            if (ShowBulk) {
                curcount_body1 = updateDotCount(diodeCurrent1, curcount_body1);
                curcount_body2 = updateDotCount(diodeCurrent2, curcount_body2);
                drawDots(g, src[0], body[0], -curcount_body1);
                drawDots(g, body[0], drn[0], curcount_body2);
            }

            drawPosts(g);
        }

        /* post 0 = gate,
         * 1 = source for NPN,
         * 2 = drain for NPN,
         * 3 = body (if present)
         * for PNP, 1 is drain, 2 is source */
        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? src[0] : (n == 2) ? drn[0] : body[0];
        }

        public override void SetPoints() {
            base.SetPoints();

            /* these two flags apply to all mosfets */
            mFlags &= ~FLAGS_GLOBAL;
            mFlags |= globalFlags;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            int hs2 = hs * mDsign;
            if ((mFlags & FLAG_FLIP) != 0) {
                hs2 = -hs2;
            }
            src = new Point[3];
            drn = new Point[3];
            Utils.InterpPoint(mPoint1, mPoint2, ref src[0], ref drn[0], 1, -hs2);
            Utils.InterpPoint(mPoint1, mPoint2, ref src[1], ref drn[1], 1 - 18 / mLen, -hs2);
            Utils.InterpPoint(mPoint1, mPoint2, ref src[2], ref drn[2], 1 - 18 / mLen, -hs2 * 4 / 3);

            gate = new Point[3];
            Utils.InterpPoint(mPoint1, mPoint2, ref gate[0], ref gate[2], 1 - 24 / mLen, hs2 / 2);
            Utils.InterpPoint(gate[0], gate[2], ref gate[1], .5);

            if (ShowBulk) {
                body = new Point[2];
                Utils.InterpPoint(src[0], drn[0], ref body[0], .5);
                Utils.InterpPoint(src[1], drn[1], ref body[1], .5);
            }

            if (!DrawDigital) {
                if (pnp == 1) {
                    if (ShowBulk) {
                        arrowPoly = Utils.CreateArrow(body[0], body[1], 10, 4);
                    } else {
                        arrowPoly = Utils.CreateArrow(src[1], src[0], 10, 4);
                    }
                } else {
                    if (ShowBulk) {
                        arrowPoly = Utils.CreateArrow(body[1], body[0], 10, 4);
                    } else {
                        arrowPoly = Utils.CreateArrow(drn[0], drn[1], 10, 4);
                    }
                }
            } else if (pnp == -1) {
                Utils.InterpPoint(mPoint1, mPoint2, ref gate[1], 1 - 36 / mLen);
                int dist = (mDsign < 0) ? 32 : 31;
                pcircle = Utils.InterpPoint(mPoint1, mPoint2, 1 - dist / mLen);
                pcircler = 3;
            }
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[1]);
            mCir.StampNonLinear(Nodes[2]);

            if (HasBodyTerminal) {
                bodyTerminal = 3;
            } else {
                bodyTerminal = (pnp == -1) ? 2 : 1;
            }

            if (DoBodyDiode) {
                if (pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    diodeB1.stamp(Nodes[1], Nodes[bodyTerminal]);
                    diodeB2.stamp(Nodes[2], Nodes[bodyTerminal]);
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    diodeB1.stamp(Nodes[bodyTerminal], Nodes[1]);
                    diodeB2.stamp(Nodes[bodyTerminal], Nodes[2]);
                }
            }
        }

        bool nonConvergence(double last, double now) {
            double diff = Math.Abs(last - now);

            /* high beta MOSFETs are more sensitive to small differences,
             * so we are more strict about convergence testing */
            if (beta > 1) {
                diff *= 100;
            }

            /* difference of less than 10mV is fine */
            if (diff < .01) {
                return false;
            }
            /* larger differences are fine if value is large */
            if (mCir.SubIterations > 10 && diff < Math.Abs(now) * .001) {
                return false;
            }
            /* if we're having trouble converging, get more lenient */
            if (mCir.SubIterations > 100 && diff < .01 + (mCir.SubIterations - 100) * .0001) {
                return false;
            }
            return true;
        }

        public override void StepFinished() {
            calculate(true);

            /* fix current if body is connected to source or drain */
            if (bodyTerminal == 1) {
                diodeCurrent1 = -diodeCurrent2;
            }
            if (bodyTerminal == 2) {
                diodeCurrent2 = -diodeCurrent1;
            }
        }

        public override void DoStep() {
            calculate(false);
        }

        /* this is called in doStep to stamp the matrix,
         * and also called in stepFinished() to calculate the current */
        void calculate(bool finished) {
            double[] vs;
            if (finished) {
                vs = Volts;
            } else {
                /* limit voltage changes to .5V */
                vs = new double[3];
                vs[0] = Volts[V_G];
                vs[1] = Volts[V_S];
                vs[2] = Volts[V_D];
                if (vs[1] > lastv1 + .5) {
                    vs[1] = lastv1 + .5;
                }
                if (vs[1] < lastv1 - .5) {
                    vs[1] = lastv1 - .5;
                }
                if (vs[2] > lastv2 + .5) {
                    vs[2] = lastv2 + .5;
                }
                if (vs[2] < lastv2 - .5) {
                    vs[2] = lastv2 - .5;
                }
            }

            int source = 1;
            int drain = 2;

            /* if source voltage > drain (for NPN), swap source and drain
             * (opposite for PNP) */
            if (pnp * vs[1] > pnp * vs[2]) {
                source = 2;
                drain = 1;
            }
            int gate = 0;
            double vgs = vs[gate] - vs[source];
            double vds = vs[drain] - vs[source];
            if (!finished && (nonConvergence(lastv1, vs[1]) || nonConvergence(lastv2, vs[2]) || nonConvergence(lastv0, vs[0]))) {
                mCir.Converged = false;
            }
            lastv0 = vs[0];
            lastv1 = vs[1];
            lastv2 = vs[2];
            double realvgs = vgs;
            double realvds = vds;
            vgs *= pnp;
            vds *= pnp;
            ids = 0;
            gm = 0;
            double Gds = 0;
            if (vgs < vt) {
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                ids = vds * Gds;
                mode = 0;
            } else if (vds < vgs - vt) {
                /* linear */
                ids = beta * ((vgs - vt) * vds - vds * vds * .5);
                gm = beta * vds;
                Gds = beta * (vgs - vds - vt);
                mode = 1;
            } else {
                /* saturation; Gds = 0 */
                gm = beta * (vgs - vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                ids = .5 * beta * (vgs - vt) * (vgs - vt) + (vds - (vgs - vt)) * Gds;
                mode = 2;
            }

            if (DoBodyDiode) {
                diodeB1.doStep(pnp * (Volts[bodyTerminal] - Volts[V_S]));
                diodeCurrent1 = diodeB1.calculateCurrent(pnp * (Volts[bodyTerminal] - Volts[V_S])) * pnp;
                diodeB2.doStep(pnp * (Volts[bodyTerminal] - Volts[V_D]));
                diodeCurrent2 = diodeB2.calculateCurrent(pnp * (Volts[bodyTerminal] - Volts[V_D])) * pnp;
            } else {
                diodeCurrent1 = diodeCurrent2 = 0;
            }

            double ids0 = ids;

            /* flip ids if we swapped source and drain above */
            if (source == 2 && pnp == 1 || source == 1 && pnp == -1) {
                ids = -ids;
            }

            if (finished) {
                return;
            }

            double rs = -pnp * ids0 + Gds * realvds + gm * realvgs;
            mCir.StampMatrix(Nodes[drain], Nodes[drain], Gds);
            mCir.StampMatrix(Nodes[drain], Nodes[source], -Gds - gm);
            mCir.StampMatrix(Nodes[drain], Nodes[gate], gm);

            mCir.StampMatrix(Nodes[source], Nodes[drain], -Gds);
            mCir.StampMatrix(Nodes[source], Nodes[source], Gds + gm);
            mCir.StampMatrix(Nodes[source], Nodes[gate], -gm);

            mCir.StampRightSide(Nodes[drain], rs);
            mCir.StampRightSide(Nodes[source], -rs);
        }

        void getFetInfo(string[] arr, string n) {
            arr[0] = ((pnp == -1) ? "p-" : "n-") + n;
            arr[0] += " (Vt=" + Utils.VoltageText(pnp * vt);
            arr[0] += ", \u03b2=" + beta + ")";
            arr[1] = ((pnp == 1) ? "Ids = " : "Isd = ") + Utils.CurrentText(ids);
            arr[2] = "Vgs = " + Utils.VoltageText(Volts[V_G] - Volts[pnp == -1 ? V_D : V_S]);
            arr[3] = ((pnp == 1) ? "Vds = " : "Vsd = ") + Utils.VoltageText(Volts[V_D] - Volts[V_S]);
            arr[4] = (mode == 0) ? "off" : (mode == 1) ? "linear" : "saturation";
            arr[5] = "gm = " + Utils.UnitText(gm, "A/V");
            arr[6] = "P = " + Utils.UnitText(Power, "W");
            if (ShowBulk) {
                arr[7] = "Ib = " + Utils.UnitText(bodyTerminal == 1 ? -diodeCurrent1 : bodyTerminal == 2 ? diodeCurrent2 : -pnp * (diodeCurrent1 + diodeCurrent2), "A");
            }
        }

        public override void GetInfo(string[] arr) {
            getFetInfo(arr, "MOSFET");
        }

        public override string GetScopeText(Scope.VAL v) {
            return ((pnp == -1) ? "p-" : "n-") + "MOSFET";
        }

        public override bool GetConnection(int n1, int n2) {
            return !(n1 == 0 || n2 == 0);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Threshold Voltage", pnp * vt, .01, 5);
            }
            if (n == 1) {
                return new ElementInfo(ElementInfo.MakeLink("mosfet-beta.html", "Beta"), beta, .01, 5);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Show Bulk",
                    Checked = ShowBulk
                };
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Swap D/S",
                    Checked = (mFlags & FLAG_FLIP) != 0
                };
                return ei;
            }
            if (n == 4 && !ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Digital Symbol",
                    Checked = DrawDigital
                };
                return ei;
            }
            if (n == 4 && ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Simulate Body Diode",
                    Checked = (mFlags & FLAG_BODY_DIODE) != 0
                };
                return ei;
            }
            if (n == 5 && DoBodyDiode) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "Body Terminal",
                    Checked = (mFlags & FLAG_BODY_TERMINAL) != 0
                };
                return ei;
            }

            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                vt = pnp * ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                beta = lastBeta = ei.Value;
            }
            if (n == 2) {
                globalFlags = (!ei.CheckBox.Checked)
                    ? (globalFlags | FLAG_HIDE_BULK) : (globalFlags & ~(FLAG_HIDE_BULK | FLAG_DIGITAL));
                SetPoints();
                ei.NewDialog = true;
            }
            if (n == 3) {
                mFlags = ei.CheckBox.Checked
                    ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
                SetPoints();
            }
            if (n == 4 && !ShowBulk) {
                globalFlags = ei.CheckBox.Checked
                    ? (globalFlags | FLAG_DIGITAL) : (globalFlags & ~FLAG_DIGITAL);
                SetPoints();
            }
            if (n == 4 && ShowBulk) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_DIODE);
                ei.NewDialog = true;
            }
            if (n == 5) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_TERMINAL);
                allocNodes();
                SetPoints();
            }
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return 0;
            }
            if (n == 3) {
                return -diodeCurrent1 - diodeCurrent2;
            }
            if (n == 1) {
                return ids + diodeCurrent1;
            }
            return -ids + diodeCurrent2;
        }
    }
}
