namespace Circuit.Elements {
    internal class BaseElement {
        public static Circuit mCir;

        public static void InitClass(Circuit c) {
            mCir = c;
        }

        public BaseElement() {
            cirAllocNodes();
        }

        #region [variable]
        public double mCirCurrent { get; protected set; }
        public double mCirCurCount;
        protected int mCirVoltSource;
        #endregion

        #region [property]
        public int[] CirNodes { get; protected set; }

        /// <summary>
        /// voltages at each node
        /// </summary>
        public double[] CirVolts { get; protected set; }

        /// <summary>
        /// is this a wire or equivalent to a wire?
        /// </summary>
        /// <returns></returns>
        public virtual bool CirIsWire { get { return false; } }

        public virtual double CirCurrent { get { return mCirCurrent; } }

        public virtual double CirVoltageDiff { get { return CirVolts[0] - CirVolts[1]; } }

        public virtual double CirPower { get { return CirVoltageDiff * mCirCurrent; } }

        /// <summary>
        /// number of voltage sources this element needs
        /// </summary>
        /// <returns></returns>
        public virtual int CirVoltageSourceCount { get { return 0; } }

        /// <summary>
        /// number of internal nodes (nodes not visible in UI that are needed for implementation)
        /// </summary>
        /// <returns></returns>
        public virtual int CirInternalNodeCount { get { return 0; } }

        public virtual bool CirNonLinear { get { return false; } }

        public virtual int CirPostCount { get { return 2; } }

        /// <summary>
        /// get number of nodes that can be retrieved by ConnectionNode
        /// </summary>
        /// <returns></returns>
        public virtual int CirConnectionNodeCount { get { return CirPostCount; } }
        #endregion

        #region [method]
        /// <summary>
        /// allocate nodes/volts arrays we need
        /// </summary>
        public void cirAllocNodes() {
            int n = CirPostCount + CirInternalNodeCount;
            /* preserve voltages if possible */
            if (CirNodes == null || CirNodes.Length != n) {
                CirNodes = new int[n];
                CirVolts = new double[n];
            }
        }

        /// <summary>
        /// update dot positions (curcount) for drawing current (simple case for single current)
        /// </summary>
        public void cirUpdateDotCount() {
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
        }

        /// <summary>
        ///  update dot positions (curcount) for drawing current (general case for multiple currents)
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="cc"></param>
        /// <returns></returns>
        public double cirUpdateDotCount(double cur, double cc) {
            if (!CirSim.Sim.IsRunning) {
                return cc;
            }
            double cadd = cur * CirSim.CurrentMult;
            cadd %= 8;
            return cc + cadd;
        }

        /// <summary>
        /// calculate current in response to node voltages changing
        /// </summary>
        protected virtual void cirCalculateCurrent() { }

        /// <summary>
        /// handle reset button
        /// </summary>
        public virtual void CirReset() {
            for (int i = 0; i != CirPostCount + CirInternalNodeCount; i++) {
                CirVolts[i] = 0;
            }
            mCirCurCount = 0;
        }

        public virtual void CirShorted() { }

        /// <summary>
        /// stamp matrix values for linear elements.
        /// for non-linear elements, use this to stamp values that don't change each iteration,
        /// and call stampRightSide() or stampNonLinear() as needed
        /// </summary>
        public virtual void CirStamp() { }

        /// <summary>
        /// stamp matrix values for non-linear elements
        /// </summary>
        public virtual void CirDoStep() { }

        public virtual void CirStartIteration() { }

        public virtual void CirStepFinished() { }

        /// <summary>
        /// set current for voltage source vn to c.
        /// vn will be the same value as in a previous call to setVoltageSource(n, vn)
        /// </summary>
        /// <param name="vn"></param>
        /// <param name="c"></param>
        public virtual void CirSetCurrent(int vn, double c) { mCirCurrent = c; }

        /// <summary>
        /// notify this element that its pth node is n.
        /// This value n can be passed to stampMatrix()
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public virtual void CirSetNode(int p, int n) {
            if (p < CirNodes.Length) {
                CirNodes[p] = n;
            }
        }

        /// <summary>
        /// notify this element that its nth voltage source is v.
        /// This value v can be passed to stampVoltageSource(),
        /// etc and will be passed back in calls to setCurrent()
        /// </summary>
        /// <param name="n"></param>
        /// <param name="v"></param>
        public virtual void CirSetVoltageSource(int n, int v) {
            /* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
            mCirVoltSource = v;
        }

        /// <summary>
        /// set voltage of x'th node, called by simulator logic
        /// </summary>
        /// <param name="n"></param>
        /// <param name="c"></param>
        public virtual void CirSetNodeVoltage(int n, double c) {
            CirVolts[n] = c;
            cirCalculateCurrent();
        }

        public virtual double CirGetCurrentIntoNode(int n) {
            /* if we take out the getPostCount() == 2 it gives the wrong value for rails */
            if (n == 0 && CirPostCount == 2) {
                return -mCirCurrent;
            } else {
                return mCirCurrent;
            }
        }

        /// <summary>
        /// get nodes that can be passed to getConnection(), to test if this element connects
        /// those two nodes; this is the same as getNode() for all but labeled nodes.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual int CirGetConnectionNode(int n) { return CirNodes[n]; }

        /// <summary>
        /// is n1 connected to ground somehow?
        /// </summary>
        /// <param name="n1"></param>
        /// <returns></returns>
        public virtual bool CirHasGroundConnection(int n1) { return false; }

        public virtual double CirGetScopeValue(Scope.VAL x) {
            return CirVoltageDiff;
        }
        #endregion
    }
}
