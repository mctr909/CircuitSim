namespace Circuit {
	internal interface ICircuit {
		double GetCurrentIntoNode(int n);
		void PrepareIteration();
		void IterationFinished();
		void DoIteration();
		void SetCurrent(int vn, double c);
		void SetVoltage(int n, double c);
	}
}
