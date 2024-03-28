namespace MainForm;

public class Inductor {
	public const int FLAG_BACK_EULER = 2;

	double inductance;
	double compResistance, current;
	double curSourceValue;
	int flags;
	readonly int[] nodes = new int[2];
	readonly CirSim sim;

	public Inductor(CirSim s) {
		sim = s;
	}

	public bool nonLinear() {
		return false;
	}

	public bool isTrapezoidal() {
		return (flags & FLAG_BACK_EULER) == 0;
	}

	public void setup(double ic, double cr, int f) {
		inductance = ic;
		current = cr;
		flags = f;
	}

	public void reset() {
		// need to set curSourceValue here in case one of inductor nodes is node 0. In
		// that case
		// calculateCurrent() may get called (from setNodeVoltage()) when analyzing
		// circuit, before
		// startIteration() gets called
		current = curSourceValue = 0;
	}

	public void stamp(int n0, int n1) {
		// inductor companion model using trapezoidal or backward euler
		// approximations (Norton equivalent) consists of a current
		// source in parallel with a resistor. Trapezoidal is more
		// accurate than backward euler but can cause oscillatory behavior.
		// The oscillation is a real problem in circuits with switches.
		nodes[0] = n0;
		nodes[1] = n1;
		if (isTrapezoidal())
			compResistance = 2 * inductance / sim.timeStep;
		else // backward euler
			compResistance = inductance / sim.timeStep;
		sim.stampResistor(nodes[0], nodes[1], compResistance);
		sim.stampRightSide(nodes[0]);
		sim.stampRightSide(nodes[1]);
	}

	public void startIteration(double voltdiff) {
		if (isTrapezoidal())
			curSourceValue = voltdiff / compResistance + current;
		else // backward euler
			curSourceValue = current;
	}

	public double calculateCurrent(double voltdiff) {
		// we check compResistance because this might get called
		// before stamp(), which sets compResistance, causing
		// infinite current
		if (compResistance > 0)
			current = voltdiff / compResistance + curSourceValue;
		return current;
	}

	public void doStep(double voltdiff) {
		sim.stampCurrentSource(nodes[0], nodes[1], curSourceValue);
	}
}
