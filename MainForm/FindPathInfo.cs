namespace MainForm;

internal class FindPathInfo {
	public const int INDUCT = 1;
	public const int VOLTAGE = 2;
	public const int SHORT = 3;
	public const int CAP_V = 4;

	bool[] visited;
	int dest;
	ElmBase firstElm;
	int type;

	// State object to help find loops in circuit subject to various conditions
	// (depending on type_)
	// elm_ = source and destination element. dest_ = destination node.
	public FindPathInfo(int type_, ElmBase elm_, int dest_) {
		dest = dest_;
		type = type_;
		firstElm = elm_;
		visited = new bool[CirSim.theSim.nodeList.Count];
	}

	// look through circuit for loop starting at node n1 of firstElm, for a path back to
	// dest node of firstElm
	public bool findPath(int n1) {
		if (n1 == dest)
			return true;

		// depth first search, don't need to revisit already visited nodes!
		if (visited[n1])
			return false;

		visited[n1] = true;
		foreach (var ce in CirSim.theSim.elmList) {
			if (ce == firstElm) {
				continue;
			}
			if (type == INDUCT) {
				// inductors need a path free of current sources
				if (ce is CurrentElm)
					continue;
			}
			if (type == VOLTAGE) {
				// when checking for voltage loops, we only care about voltage
				// sources/wires/ground
				if (!(ce.isWire() || ce is VoltageElm || ce is GroundElm))
					continue;
			}
			// when checking for shorts, just check wires
			if (type == SHORT && !ce.isWire())
				continue;
			if (type == CAP_V) {
				// checking for capacitor/voltage source loops
				if (!(ce.isWire() || ce is CapacitorElm || ce is VoltageElm))
					continue;
			}
			if (n1 == 0) {
				// look for posts which have a ground connection;
				// our path can go through ground
				for (int ix = 0; ix != ce.getConnectionNodeCount(); ix++)
					if (ce.hasGroundConnection(ix) && findPath(ce.getConnectionNode(ix)))
						return true;
			}
			int j;
			for (j = 0; j != ce.getConnectionNodeCount(); j++) {
				if (ce.getConnectionNode(j) == n1)
					break;
			}
			if (j == ce.getConnectionNodeCount())
				continue;
			if (ce.hasGroundConnection(j) && findPath(0)) {
				return true;
			}
			if (type == INDUCT && ce is InductorElm) {
				// inductors can use paths with other inductors of matching current
				var c = ce.getCurrent();
				if (j == 0)
					c = -c;
				if (Math.Abs(c - firstElm.getCurrent()) > 1e-10)
					continue;
			}
			int k;
			for (k = 0; k != ce.getConnectionNodeCount(); k++) {
				if (j == k)
					continue;
				if (ce.getConnection(j, k) && findPath(ce.getConnectionNode(k))) {
					// System.out.println("got findpath " + n1);
					return true;
				}
			}
		}
		return false;
	}
}
