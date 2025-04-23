namespace Circuit.Elements {
	public abstract partial class BaseElement {
		protected static Random Random = new();

		public double[] V = new double[8];
		public double[] I = new double[8];
		public double[] Para = new double[8];
		public int[] State = new int[8];
		public int[] Nodes;
		public int VoltSource;
		public bool Broken = false;

		public virtual int TermCount { get { return 2; } }
		public virtual double VoltageDiff { get { return V[0] - V[1]; } }

		protected virtual void DoIteration() { }
		protected virtual void StartIteration() { }
		protected virtual void FinishIteration() { }

		protected virtual double GetCurrent(int n) { return (n == 0 && TermCount == 2) ? -I[0] : I[0]; }
		protected virtual void SetCurrent(int n, double i) { I[0] = i; }

		public virtual void SetVoltage(int n, double v) { V[n] = v; }
		public virtual void SetVoltageSource(int n, int vs) { VoltSource = vs; }
	}
}
