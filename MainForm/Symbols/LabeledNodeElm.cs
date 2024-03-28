namespace MainForm;

internal class LabeledNodeElm : ElmBase {
	const int FLAG_ESCAPE = 4;
	const int FLAG_INTERNAL = 1;

	public LabeledNodeElm(int xx, int yy) : base(xx, yy) {
		text = "label";
	}

	public LabeledNodeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		text = st.nextToken();
		if ((flags & FLAG_ESCAPE) == 0) {
			// old-style dump before escape/unescape
			while (st.HasMoreTokens)
				text += ' ' + st.nextToken();
		} else {
			// new-style dump
			text = CustomLogicModel.unescape(text);
		}
	}

	public override string dump() {
		flags |= FLAG_ESCAPE;
		return base.dump() + " " + CustomLogicModel.escape(text);
	}

	string text;
	static Dictionary<string, int?> nodeList;
	int? nodeNumber;

	bool isInternal() { return (flags & FLAG_INTERNAL) != 0; }

	public static void resetNodeList() {
		nodeList = [];
	}

	const int circleSize = 17;

	public override void setPoints() {
		base.setPoints();
		lead1 = interpPoint(point1, point2, 1 - circleSize / dn);
	}

	public override void setNode(int p, int n) {
		base.setNode(p, n);
		if (p == 1) {
			// assign new node
			nodeList.Add(text, n);
			nodeNumber = n;
		}
	}

	public override int getDumpType() { return 207; }

	public override int getPostCount() { return 1; }

	// this is basically a wire, since it just connects two nodes together
	public override bool isWire() { return true; }

	// get connection node (which is the same as regular nodes for all elements but this one).
	// node 0 is the terminal, node 1 is the internal node shared by all nodes with same name
	public override int getConnectionNode(int n) {
		if (n == 0)
			return nodes[0];
		return (int)nodeNumber;
	}

	public override int getConnectionNodeCount() { return 2; }

	public override int getInternalNodeCount() {
		// this can happen at startup
		if (nodeList == null)
			return 0;

		var nn = nodeList[text];

		// node assigned already?
		if (nn != null) {
			nodeNumber = nn;
			return 0;
		}

		// allocate a new one
		return 1;
	}

	public override void draw(CustomGraphics g) {
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		g.setColor(needsHighlight() ? selectColor : whiteColor);
		setPowerColor(g, false);
		var str = text;
		var lineOver = false;
		if (str.IndexOf("/") == 0) {
			lineOver = true;
			str = str.Substring(1);
		}
		drawCenteredText(g, str, x2, y2, true);
		if (lineOver) {
			var asc = (int)g.currentFontSize;
			if (lineOver) {
				var ya = y2 - asc;
				var sw = (int)g.measureText(str).Width;
				g.drawLine(x2 - sw / 2, ya, x2 + sw / 2, ya);
			}
		}
		curcount = updateDotCount(current, curcount);
		drawDots(g, point1, lead1, curcount);
		interpPoint(point1, point2, ref ps2, 1 + 11.0 / dn);
		setBbox(point1, ps2, circleSize);
		drawPosts(g);
	}

	public override double getCurrentIntoNode(int n) { return -current; }

	public override void setCurrent(int x, double c) { current = -c; }

	public override void stamp() {
		sim.stampVoltageSource((int)nodeNumber, nodes[0], voltSource, 0);
	}

	public override double getVoltageDiff() { return volts[0]; }

	public override int getVoltageSourceCount() { return 1; }

	public override void getInfo(string[] arr) {
		arr[0] = text;
		arr[1] = "I = " + getCurrentText(getCurrent());
		arr[2] = "V = " + getVoltageText(volts[0]);
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			var ei = new EditInfo("Text", 0, -1, -1);
			ei.text = text;
			return ei;
		}
		if (n == 1) {
			return new EditInfo("Internal Node", isInternal());
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			text = ei.textf.Text;
		if (n == 1)
			flags = ei.changeFlag(flags, FLAG_INTERNAL);
	}
}
