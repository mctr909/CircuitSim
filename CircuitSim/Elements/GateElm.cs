using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    abstract class GateElm : CircuitElm {
        const int FLAG_SMALL = 1;
        const int FLAG_SCHMITT = 2;
        protected int inputCount = 2;
        bool lastOutput;
        double highVoltage;
        public static double lastHighVoltage = 5;
        static bool lastSchmitt = false;

        int gsize;
        int gwidth;
        int gwidth2;
        int gheight;
        protected int hs2;

        Point[] inPosts;
        Point[] inGates;

        bool[] inputStates;

        int oscillationCount;

        protected int ww;

        protected Point[] gatePolyEuro;
        protected Point[] gatePolyAnsi;

        Point[] schmittPoly;

        protected Point pcircle;
        protected Point[] linePoints;

        public GateElm(int xx, int yy) : base(xx, yy) {
            mNoDiagonal = true;
            inputCount = 2;

            /* copy defaults from last gate edited */
            highVoltage = lastHighVoltage;
            if (lastSchmitt) {
                mFlags |= FLAG_SCHMITT;
            }

            setSize(Sim.chkSmallGridCheckItem.Checked ? 1 : 2);
        }

        public GateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            inputCount = st.nextTokenInt();
            double lastOutputVoltage = st.nextTokenDouble();
            mNoDiagonal = true;
            try {
                highVoltage = st.nextTokenDouble();
            } catch {
                highVoltage = 5;
            }
            lastOutput = lastOutputVoltage > highVoltage * .5;
            setSize((f & FLAG_SMALL) != 0 ? 1 : 2);
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return inputCount + 1; } }

        protected virtual bool isInverting() { return false; }

        void setSize(int s) {
            gsize = s;
            gwidth = 7 * s;
            gwidth2 = 14 * s;
            gheight = 8 * s;
            mFlags &= ~FLAG_SMALL;
            mFlags |= (s == 1) ? FLAG_SMALL : 0;
        }

        protected override string dump() {
            return inputCount + " " + Volts[inputCount] + " " + highVoltage;
        }

        public override void SetPoints() {
            base.SetPoints();
            inputStates = new bool[inputCount];
            if (mLen > 150 && this == Sim.dragElm) {
                setSize(2);
            }
            int hs = gheight;
            int i;
            ww = gwidth2;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            if (isInverting() && ww + 8 > mLen / 2) {
                ww = (int)(mLen / 2 - 8);
            }
            calcLeads(ww * 2);
            inPosts = new Point[inputCount];
            inGates = new Point[inputCount];
            allocNodes();
            int i0 = -inputCount / 2;
            for (i = 0; i != inputCount; i++, i0++) {
                if (i0 == 0 && (inputCount & 1) == 0) {
                    i0++;
                }
                inPosts[i] = interpPoint(mPoint1, mPoint2, 0, hs * i0);
                inGates[i] = interpPoint(mLead1, mLead2, 0, hs * i0);
                Volts[i] = (lastOutput ^ isInverting()) ? 5 : 0;
            }
            hs2 = gwidth * (inputCount / 2 + 1);
            setBbox(mPoint1, mPoint2, hs2);
            if (hasSchmittInputs()) {
                schmittPoly = getSchmittPolygon(gsize, .47f).ToArray();
            }
        }

        protected void createEuroGatePolygon() {
            var pts = new Point[4];
            interpPoint(mLead1, mLead2, ref pts[0], ref pts[1], 0, hs2);
            interpPoint(mLead1, mLead2, ref pts[3], ref pts[2], 1, hs2);
            gatePolyEuro = createPolygon(pts).ToArray();
        }

        protected virtual string getGateText() { return null; }

        public static bool useAnsiGates() { return Sim.chkAnsiResistorCheckItem.Checked; }

        public override void Draw(Graphics g) {
            int i;
            for (i = 0; i != inputCount; i++) {
                drawThickLine(g, getVoltageColor(Volts[i]), inPosts[i], inGates[i]);
            }
            drawThickLine(g, getVoltageColor(Volts[inputCount]), mLead2, mPoint2);
            PenThickLine.Color = needsHighlight() ? SelectColor : LightGrayColor;
            if (useAnsiGates()) {
                drawThickPolygon(g, gatePolyAnsi);
            } else {
                drawThickPolygon(g, gatePolyEuro);
                var center = interpPoint(mPoint1, mPoint2, .5);
                drawCenteredLText(g, getGateText(), center.X, center.Y - 6 * gsize, true);
            }
            if (hasSchmittInputs()) {
                PenLine.Color = WhiteColor;
                drawPolygon(g, schmittPoly);
            }
            if (linePoints != null && useAnsiGates()) {
                for (i = 0; i != linePoints.Length - 1; i++) {
                    drawThickLine(g, linePoints[i], linePoints[i + 1]);
                }
            }
            if (isInverting()) {
                drawThickCircle(g, pcircle.X, pcircle.Y, 9);
            }
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
            drawPosts(g);
        }

        public override Point GetPost(int n) {
            if (n == inputCount) {
                return mPoint2;
            }
            return inPosts[n];
        }

        protected abstract string getGateName();

        public override void GetInfo(string[] arr) {
            arr[0] = getGateName();
            arr[1] = "Vout = " + getVoltageText(Volts[inputCount]);
            arr[2] = "Iout = " + getCurrentText(mCurrent);
        }

        public override void Stamp() {
            Cir.StampVoltageSource(0, Nodes[inputCount], mVoltSource);
        }

        bool hasSchmittInputs() { return (mFlags & FLAG_SCHMITT) != 0; }

        protected bool getInput(int x) {
            if (!hasSchmittInputs()) {
                return Volts[x] > highVoltage * .5;
            }
            bool res = Volts[x] > highVoltage * (inputStates[x] ? .35 : .55);
            inputStates[x] = res;
            return res;
        }

        protected abstract bool calcFunction();

        public override void DoStep() {
            bool f = calcFunction();
            if (isInverting()) {
                f = !f;
            }

            /* detect oscillation (using same strategy as Atanua) */
            if (lastOutput == !f) {
                if (oscillationCount++ > 50) {
                    /* output is oscillating too much, randomly leave output the same */
                    oscillationCount = 0;
                    if (CirSim.random.Next(10) > 5) {
                        f = lastOutput;
                    }
                }
            } else {
                oscillationCount = 0;
            }
            lastOutput = f;
            double res = f ? highVoltage : 0;
            Cir.UpdateVoltageSource(0, Nodes[inputCount], mVoltSource, res);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("# of Inputs", inputCount, 1, 8).SetDimensionless();
            }
            if (n == 1) {
                return new EditInfo("High Voltage (V)", highVoltage, 1, 10);
            }
            if (n == 2) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Schmitt Inputs",
                    Checked = hasSchmittInputs()
                };
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value >= 1) {
                inputCount = (int)ei.Value;
                SetPoints();
            }
            if (n == 1) {
                highVoltage = lastHighVoltage = ei.Value;
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SCHMITT;
                } else {
                    mFlags &= ~FLAG_SCHMITT;
                }
                lastSchmitt = hasSchmittInputs();
                SetPoints();
            }
        }

        /* there is no current path through the gate inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) {
            return (n1 == inputCount);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == inputCount) {
                return mCurrent;
            }
            return 0;
        }
    }
}
