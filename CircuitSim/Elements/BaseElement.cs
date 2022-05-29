namespace Circuit.Elements {
    public abstract class BaseElement {
        protected static bool ComparePair(int x1, int x2, int y1, int y2) {
            return (x1 == y1 && x2 == y2) || (x1 == y2 && x2 == y1);
        }

        public BaseElement() {
            AllocNodes();
        }

        protected double mCurrent;
        protected int mVoltSource;

        #region [property]
        public abstract int PostCount { get; }

        public double CurCount { get; set; }

        public int[] Nodes { get; protected set; }

        /// <summary>
        /// voltages at each node
        /// </summary>
        public double[] Volts { get; protected set; }

        /// <summary>
        /// number of voltage sources this element needs
        /// </summary>
        /// <returns></returns>
        public virtual int VoltageSourceCount { get { return 0; } }

        /// <summary>
        /// number of internal nodes (nodes not visible in UI that are needed for implementation)
        /// </summary>
        /// <returns></returns>
        public virtual int InternalNodeCount { get { return 0; } }

        /// <summary>
        /// get number of nodes that can be retrieved by ConnectionNode
        /// </summary>
        /// <returns></returns>
        public virtual int ConnectionNodeCount { get { return PostCount; } }

        /// <summary>
        /// is this a wire or equivalent to a wire?
        /// </summary>
        /// <returns></returns>
        public virtual bool IsWire { get { return false; } }

        public virtual double Current { get { return mCurrent; } }

        public virtual double VoltageDiff { get { return Volts[0] - Volts[1]; } }

        public virtual double Power { get { return VoltageDiff * mCurrent; } }

        public virtual bool NonLinear { get { return false; } }
        #endregion

        #region [method]
        /// <summary>
        /// allocate nodes/volts arrays we need
        /// </summary>
        public void AllocNodes() {
            int n = PostCount + InternalNodeCount;
            /* preserve voltages if possible */
            if (Nodes == null || Nodes.Length != n) {
                Nodes = new int[n];
                Volts = new double[n];
            }
        }

        /// <summary>
        /// handle reset button
        /// </summary>
        public virtual void Reset() {
            for (int i = 0; i != PostCount + InternalNodeCount; i++) {
                Volts[i] = 0;
            }
            CurCount = 0;
        }

        /// <summary>
        /// are n1 and n2 connected by this element?  this is used to determine
        /// unconnected nodes, and look for loops
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public virtual bool GetConnection(int n1, int n2) { return true; }

        public virtual double GetCurrentIntoNode(int n) {
            /* if we take out the getPostCount() == 2 it gives the wrong value for rails */
            if (n == 0 && PostCount == 2) {
                return -mCurrent;
            } else {
                return mCurrent;
            }
        }

        public virtual double GetScopeValue(Scope.VAL x) {
            return VoltageDiff;
        }

        /// <summary>
        /// stamp matrix values for linear elements.
        /// for non-linear elements, use this to stamp values that don't change each iteration,
        /// and call stampRightSide() or stampNonLinear() as needed
        /// </summary>
        public virtual void AnaStamp() { }

        /// <summary>
        /// notify this element that its pth node is n.
        /// This value n can be passed to stampMatrix()
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public virtual void AnaSetNode(int p, int n) {
            if (p < Nodes.Length) {
                Nodes[p] = n;
            }
        }

        /// <summary>
        /// notify this element that its nth voltage source is v.
        /// This value v can be passed to stampVoltageSource(),
        /// etc and will be passed back in calls to setCurrent()
        /// </summary>
        /// <param name="n"></param>
        /// <param name="v"></param>
        public virtual void AnaSetVoltageSource(int n, int v) {
            /* default implementation only makes sense for subclasses with one voltage source.
             * If we have 0 this isn't used, if we have >1 this won't work */
            mVoltSource = v;
        }

        /// <summary>
        /// get nodes that can be passed to getConnection(), to test if this element connects
        /// those two nodes; this is the same as getNode() for all but labeled nodes.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public virtual int AnaGetConnectionNode(int n) { return Nodes[n]; }

        /// <summary>
        /// is n1 connected to ground somehow?
        /// </summary>
        /// <param name="n1"></param>
        /// <returns></returns>
        public virtual bool AnaHasGroundConnection(int n1) { return false; }

        public virtual void AnaShorted() { }

        public virtual void CirStartIteration() { }

        /// <summary>
        /// stamp matrix values for non-linear elements
        /// </summary>
        public virtual void CirDoStep() { }

        public virtual void CirStepFinished() { }

        /// <summary>
        /// set current for voltage source vn to c.
        /// vn will be the same value as in a previous call to setVoltageSource(n, vn)
        /// </summary>
        /// <param name="vn"></param>
        /// <param name="c"></param>
        public virtual void CirSetCurrent(int vn, double c) { mCurrent = c; }

        /// <summary>
        /// set voltage of x'th node, called by simulator logic
        /// </summary>
        /// <param name="n"></param>
        /// <param name="c"></param>
        public virtual void CirSetNodeVoltage(int n, double c) {
            if (Volts.Length <= n) {
                return;
            }
            Volts[n] = c;
        }
        #endregion
    }
}
