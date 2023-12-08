using System.Drawing;

namespace Circuit {
	internal interface ICircuit {
		int GetNodeAtPoint(Point p);
		double GetCurrentIntoNode(int n);
		void PrepareIteration();
		void IterationFinished();
		void DoIteration();
		void SetCurrent(int vn, double c);
		void SetVoltage(int n, double c);
	}
}
