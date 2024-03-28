using System.Drawing.Drawing2D;
using MainForm.Forms;

namespace MainForm {
	public class CirSim {
		public const int MODE_ADD_ELM = 0;
		public const int MODE_DRAG_ALL = 1;
		public const int MODE_DRAG_ROW = 2;
		public const int MODE_DRAG_COLUMN = 3;
		public const int MODE_DRAG_SELECTED = 4;
		public const int MODE_DRAG_POST = 5;
		public const int MODE_SELECT = 6;
		public const int MODE_DRAG_SPLITTER = 7;

		const int HINT_LC = 1;
		const int HINT_RC = 2;
		const int HINT_3DB_C = 3;
		const int HINT_TWINT = 4;
		const int HINT_3DB_L = 5;

		public int mouseMode = MODE_SELECT;
		private int tempMouseMode = MODE_SELECT;

		public static CirSim theSim;
		public static Form dialogShowing;
		public static Form diodeModelEditDialog;

		public CheckBox voltsCheckItem;
		public CheckBox dotsCheckItem;
		public CheckBox powerCheckItem;
		public CheckBox euroResistorCheckItem;
		public CheckBox showValuesCheckItem;
		public CheckBox conventionCheckItem;
		public CheckBox showResistanceInVoltageSources;
		public CheckBox printableCheckItem;
		public CheckBox noEditCheckItem;
		public CheckBox crossHairCheckItem;
		public Button runStopButton;

		public EditDialog? editDialog;
		public ElmBase plotXElm;
		public ElmBase plotYElm;
		public ElmBase dragElm;
		public ElmBase menuElm;

		public int scopeSelected = -1;
		public int mouseCursorX = -1;
		public int mouseCursorY = -1;
		private bool mouseWasOverSplitter;
		private ElmBase mouseElm = null;
		private int mousePost = -1;
		private int hintType = -1;
		private int hintItem1;
		private int hintItem2;
		private float[] transform;

		private Bitmap cvcontext;
		private Bitmap backcontext;
		private Scrollbar speedBar;
		private Scrollbar currentBar;
		private Scrollbar powerBar;
		private Rectangle circuitArea;
		private Rectangle selectedArea;

		private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
		{
			Interval = 1,
			Enabled = true
		};

		long myframes = 1;
		long mytime = 0;
		long myruntime = 0;
		long mydrawtime = 0;

		public int gridSize;
		int gridRound;
		int gridMask;

		public bool analyzeFlag;
		public bool dcAnalysisFlag;

		long lastTime = 0, secTime = 0;
		int frames = 0;
		int framerate = 0, steprate = 0;

		#region Circuit
		const int infoWidth = 120;

		public List<ElmBase> elmList = [];
		public ElmBase? stopElm;

		internal List<CircuitNode> nodeList = [];
		Dictionary<Point, NodeMapEntry> nodeMap = [];
		Dictionary<Point, int?> postCountMap = [];
		public List<Point> postDrawList = [];
		public List<Point> badConnectionList = [];
		List<int> unconnectedNodes = [];
		List<WireInfo> wireInfoList = [];

		int voltageSourceCount;
		ElmBase[] voltageSources;
		int circuitMatrixSize;
		int circuitMatrixFullSize;
		RowInfo[] circuitRowInfo;
		public double[,]? circuitMatrix;
		double[,] origMatrix;
		double[] circuitRightSide;
		double[] origRightSide;
		double[] lastNodeVoltages;
		double[] nodeVoltages;
		int[] circuitPermute;

		public int scopeCount;
		int[] scopeColCount;
		public Scope[] scopes;

		string? stopMessage;

		public bool converged;
		bool simRunning;
		bool circuitNonLinear;
		bool circuitNeedsMap;
		bool adjustTimeStep;
		bool dumpMatrix;

		public int subIterations;
		public double t;
		public double timeStep;
		public double maxTimeStep;
		public double minTimeStep;
		public double timeStepAccum;
		public int timeStepCount;

		public int steps = 0;
		public long lastFrameTime;
		public long lastIterTime;
		#endregion

		public TrackBar findAdjustable(ElmBase elm, int i) { return null; }

		public void deleteSliders(ElmBase elm) { }

		public void needAnalyze() { }

		public ElmBase getElm(int i) { return null; }

		public int locateElm(ElmBase elm) { return -1; }

		public bool dialogIsShowing() { return false; }

		public bool simIsRunning() {
			return simRunning;
		}

		public void setSimRunning(bool s) {
			if (s) {
				if (stopMessage != null) {
					return;
				}
				simRunning = true;
				runStopButton.Text = "RUN";
				timer.Start();
			} else {
				simRunning = false;
				runStopButton.Text = "STOP";
				timer.Stop();
				repaint();
			}
		}

		public void setiFrameHeight() { }

		public void addWidgetToVerticalPanel(Control w) { }

		public void removeWidgetFromVerticalPanel(Control w) { }

		public void updateModels() { }

		public int snapGrid(int x) {
			return (x + gridRound) & gridMask;
		}

		// convert grid coordinates to screen coordinates
		public int transformX(double x) {
			return (int)((x * transform[0]) + transform[4]);
		}

		public int transformY(double y) {
			return (int)((y * transform[3]) + transform[5]);
		}

		// convert screen coordinates to grid coordinates by inverting circuit transform
		public int inverseTransformX(double x) {
			return (int)((x - transform[4]) / transform[0]);
		}

		public int inverseTransformY(double y) {
			return (int)((y - transform[5]) / transform[3]);
		}

		public void stop(string s, ElmBase? ce = null) {
			stopMessage = s;
			circuitMatrix = null; // causes an exception
			stopElm = ce;
			setSimRunning(false);
			analyzeFlag = false;
		}

		double getIterCount() {
			if (speedBar.Value == 0)
				return 0;
			return .1 * Math.Exp((speedBar.Value - 61) / 24.0);
		}

		string? getHint() {
			var c1 = getElm(hintItem1);
			var c2 = getElm(hintItem2);
			if (c1 == null || c2 == null) {
				return null;
			}
			switch (hintType) {
			case HINT_LC: {
				if (c1 is not InductorElm ei)
					return null;
				if (c2 is not CapacitorElm ec)
					return null;
				return $"res.f = {ElmBase.getUnitText(1 / (2 * Math.PI * Math.Sqrt(ei.inductance * ec.capacitance)), "Hz")}";
			}
			case HINT_RC: {
				if (c1 is not ResistorElm er)
					return null;
				if (c2 is not CapacitorElm ec)
					return null;
				return $"RC = {ElmBase.getUnitText(er.resistance * ec.capacitance, "s")}";
			}
			case HINT_3DB_C: {
				if (c1 is not ResistorElm er)
					return null;
				if (c2 is not CapacitorElm ec)
					return null;
				return $"f.3db = {ElmBase.getUnitText(1 / (2 * Math.PI * er.resistance * ec.capacitance), "Hz")}";
			}
			case HINT_3DB_L: {
				if (c1 is not ResistorElm er)
					return null;
				if (c2 is not InductorElm ei)
					return null;
				return $"f.3db = {ElmBase.getUnitText(er.resistance / (2 * Math.PI * ei.inductance), "Hz")}";
			}
			case HINT_TWINT: {
				if (c1 is not ResistorElm er)
					return null;
				if (c2 is not CapacitorElm ec)
					return null;
				return $"fc = {ElmBase.getUnitText(1 / (2 * Math.PI * er.resistance * ec.capacitance), "Hz")}";
			}
			default:
				return null;
			}
		}

		bool needsRepaint;
		void repaint() {
			if (!needsRepaint) {
				needsRepaint = true;
				new Task(() => {
					updateCircuit();
					needsRepaint = false;
				}).Start();
			}
		}

		void setupScopes(int width, int height, int circuitHeight) {
			int i;

			// check scopes to make sure the elements still exist, and remove
			// unused scopes/columns
			int pos = -1;
			for (i = 0; i < scopeCount; i++) {
				if (scopes[i].needToRemove()) {
					for (int j = i; j != scopeCount; j++) {
						scopes[j] = scopes[j + 1];
					}
					scopeCount--;
					i--;
					continue;
				}
				if (scopes[i].position > pos + 1) {
					scopes[i].position = pos + 1;
				}
				pos = scopes[i].position;
			}
			while (scopeCount > 0 && scopes[scopeCount - 1].getElm() == null) {
				scopeCount--;
			}

			var h = height - circuitHeight;
			pos = 0;
			for (i = 0; i != scopeCount; i++) {
				scopeColCount[i] = 0;
			}
			for (i = 0; i != scopeCount; i++) {
				pos = Math.Max(scopes[i].position, pos);
				scopeColCount[scopes[i].position]++;
			}
			var colct = pos + 1;
			var iw = infoWidth;
			if (colct <= 2) {
				iw = iw * 3 / 2;
			}
			var w = (width - iw) / colct;
			int marg = 10;
			if (w < marg * 2) {
				w = marg * 2;
			}
			pos = -1;
			int colh = 0;
			int row = 0;
			int speed = 0;
			for (i = 0; i != scopeCount; i++) {
				var s = scopes[i];
				if (s.position > pos) {
					pos = s.position;
					colh = h / scopeColCount[pos];
					row = 0;
					speed = s.speed;
				}
				s.stackCount = scopeColCount[pos];
				if (s.speed != speed) {
					s.speed = speed;
					s.resetGraph();
				}
				var r = new Rectangle(pos * w, height - h + colh * row, w - marg, colh);
				row++;
				if (!r.Equals(s.rect)) {
					s.setRect(r);
				}
			}
		}

		public void updateCircuit() {
			long mystarttime;
			long myrunstarttime;
			long mydrawstarttime;
			// if (winSize == null || winSize.width == 0)
			// return;
			mystarttime = ElmBase.sw.ElapsedMilliseconds;
			var didAnalyze = analyzeFlag;
			if (analyzeFlag || dcAnalysisFlag) {
				analyzeCircuit();
				analyzeFlag = false;
			}
			// if (editDialog != null && editDialog.elm instanceof CircuitElm)
			// mouseElm = (CircuitElm) (editDialog.elm);
			if (stopElm != null && stopElm != mouseElm) {
				stopElm.setMouseElm(true);
			}
			setupScopes(cvcontext.Width, cvcontext.Height, circuitArea.Height);

			var g = new CustomGraphics(backcontext);

			ElmBase.selectColor = Color.Cyan;
			if (printableCheckItem.Checked) {
				ElmBase.whiteColor = Color.Black;
				ElmBase.lightGrayColor = Color.Black;
				g.setColor(Color.White);
			} else {
				ElmBase.whiteColor = Color.White;
				ElmBase.lightGrayColor = Color.LightGray;
				g.setColor(Color.Black);
			}
			g.fillRect(0, 0, g.getWidth(), g.getHeight());

			myrunstarttime = ElmBase.sw.ElapsedMilliseconds;
			if (simRunning) {
				try {
					RunCircuit(didAnalyze, getIterCount());
				} catch (Exception e) {
					Console.WriteLine($"exception in runCircuit {e.Message}\r\n{e.StackTrace}");
					return;
				}
				myruntime += ElmBase.sw.ElapsedMilliseconds - myrunstarttime;
			}

			var sysTime = ElmBase.sw.ElapsedMilliseconds;
			if (simRunning) {
				if (lastTime != 0) {
					var inc = (int) (sysTime - lastTime);
					var c = currentBar.Value;
					c = Math.Exp(c / 3.5 - 14.2);
					ElmBase.currentMult = 1.7 * inc * c;
					if (!conventionCheckItem.Checked)
						ElmBase.currentMult = -ElmBase.currentMult;
				}
				lastTime = sysTime;
			} else {
				lastTime = 0;
			}

			if (sysTime - secTime >= 1000) {
				framerate = frames;
				steprate = steps;
				frames = 0;
				steps = 0;
				secTime = sysTime;
			}
			ElmBase.powerMult = Math.Exp(powerBar.Value / 4.762 - 7);

			int i;
			// Font oldfont = g.getFont();
			var oldfont = ElmBase.unitsFont;
			g.setFont(oldfont);

			// this causes bad behavior on Chrome 55
			// g.clipRect(0, 0, circuitArea.width, circuitArea.height);

			mydrawstarttime = ElmBase.sw.ElapsedMilliseconds;

			g.setLineCap(LineCap.Round);

			if (noEditCheckItem.Checked) {
				g.drawLock(20, 30);
			}
			g.setColor(Color.White);
			// draw elements
			g.transform(
				transform[0], transform[1],
				transform[2], transform[3],
				transform[4], transform[5]
			);
			for (i = 0; i != elmList.Count; i++) {
				if (powerCheckItem.Checked) {
					g.setColor(Color.Gray);
				}
				/*
				 * else if (conductanceCheckItem.getState())
				 * g.setColor(Color.white);
				 */
				getElm(i).draw(g);
			}
			mydrawtime += ElmBase.sw.ElapsedMilliseconds - mydrawstarttime;

			// draw posts normally
			if (mouseMode != MODE_DRAG_ROW && mouseMode != MODE_DRAG_COLUMN) {
				foreach (var p in postDrawList) {
					ElmBase.drawPost(g, p);
				}
			}

			// for some mouse modes, what matters is not the posts but the endpoints (which
			// are only
			// the same for 2-terminal elements). We draw those now if needed
			if (tempMouseMode == MODE_DRAG_ROW || tempMouseMode == MODE_DRAG_COLUMN ||
					tempMouseMode == MODE_DRAG_POST || tempMouseMode == MODE_DRAG_SELECTED) {
				foreach (var ce in elmList) {
					// ce.drawPost(g, ce.x , ce.y );
					// ce.drawPost(g, ce.x2, ce.y2);
					if (ce != mouseElm || tempMouseMode != MODE_DRAG_POST) {
						g.setColor(Color.Gray);
						g.fillOval(ce.x - 3, ce.y - 3, 7, 7);
						g.fillOval(ce.x2 - 3, ce.y2 - 3, 7, 7);
					} else {
						ce.drawHandles(g, Color.Cyan);
					}
				}
			}
			// draw handles for elm we're creating
			if (tempMouseMode == MODE_SELECT && mouseElm != null) {
				mouseElm.drawHandles(g, Color.Cyan);
			}

			// draw handles for elm we're dragging
			if (dragElm != null &&
					(dragElm.x != dragElm.x2 || dragElm.y != dragElm.y2)) {
				dragElm.draw(g);
				dragElm.drawHandles(g, Color.Cyan);
			}

			// draw bad connections. do this last so they will not be overdrawn.
			foreach (var cn in badConnectionList) {
				g.setColor(Color.Red);
				g.fillOval(cn.X - 3, cn.Y - 3, 7, 7);
			}

			if (selectedArea != Rectangle.Empty) {
				g.setColor(ElmBase.selectColor);
				g.drawRect(selectedArea.X, selectedArea.Y, selectedArea.Width, selectedArea.Height);
			}

			if (crossHairCheckItem.Checked && mouseCursorX >= 0
					&& mouseCursorX <= circuitArea.Width && mouseCursorY <= circuitArea.Height) {
				g.setColor(Color.Gray);
				var x = snapGrid(inverseTransformX(mouseCursorX));
				var y = snapGrid(inverseTransformY(mouseCursorY));
				g.drawLine(x, inverseTransformY(0), x, inverseTransformY(circuitArea.Height));
				g.drawLine(inverseTransformX(0), y, inverseTransformX(circuitArea.Width), y);
			}

			g.transform(1, 0, 0, 1, 0, 0);

			g.setColor(printableCheckItem.Checked ? Color.White : Color.Black);
			g.fillRect(0, circuitArea.Height, circuitArea.Width, g.getHeight() - circuitArea.Height);
			// g.restore();
			g.setFont(oldfont);
			var ct = scopeCount;
			if (stopMessage != null) {
				ct = 0;
			}
			for (i = 0; i != ct; i++) {
				scopes[i].draw(g);
			}
			if (mouseWasOverSplitter) {
				g.setColor(Color.Cyan);
				g.setLineWidth(4.0f);
				g.drawLine(0, circuitArea.Height - 2, circuitArea.Width, circuitArea.Height - 2);
				g.setLineWidth(1.0f);
			}
			g.setColor(ElmBase.whiteColor);

			if (stopMessage != null) {
				g.drawString(stopMessage, 10, circuitArea.Height - 10);
			} else {
				var info = new string[10];
				if (mouseElm != null) {
					if (mousePost == -1) {
						mouseElm.getInfo(info);
					} else {
						info[0] = "V = " +
								ElmBase.getUnitText(mouseElm.getPostVoltage(mousePost), "V");
					}
					// /* //shownodes
					// for (i = 0; i != mouseElm.getPostCount(); i++)
					// info[0] += " " + mouseElm.nodes[i];
					// if (mouseElm.getVoltageSourceCount() > 0)
					// info[0] += ";" + (mouseElm.getVoltageSource()+nodeList.size());
					// */
				} else {
					info[0] = $"t = {ElmBase.getTimeText(t)}";
					info[1] = $"time step = {ElmBase.getTimeText(timeStep)}";
				}
				if (hintType != -1) {
					for (i = 0; info[i] != null; i++)
						;
					var s = getHint();
					if (s == null) {
						hintType = -1;
					} else {
						info[i] = s;
					}
				}
				int x = 0;
				if (ct != 0) {
					x = scopes[ct - 1].rightEdge() + 20;
				}
				x = Math.Max(x, g.getWidth() * 2 / 3);

				// count lines of data
				for (i = 0; info[i] != null; i++)
					;
				var badnodes = badConnectionList.Count;
				if (badnodes > 0) {
					info[i++] = badnodes + ((badnodes == 1) ? " bad connection" : " bad connections");
				}

				var ybase = circuitArea.Height;
				for (i = 0; info[i] != null; i++) {
					g.drawString(info[i], x, ybase + 15 * (i + 1));
				}
			}
			if (stopElm != null && stopElm != mouseElm) {
				stopElm.setMouseElm(false);
			}
			frames++;

			g.setColor(Color.White);
			// g.drawString("Framerate: " + CircuitElm.showFormat.format(framerate), 10,
			// 10);
			// g.drawString("Steprate: " + CircuitElm.showFormat.format(steprate), 10, 30);
			// g.drawString("Steprate/iter: " +
			// CircuitElm.showFormat.format(steprate/getIterCount()), 10, 50);
			// g.drawString("iterc: " + CircuitElm.showFormat.format(getIterCount()), 10,
			// 70);
			// g.drawString("Frames: "+ frames,10,90);
			// g.drawString("ms per frame (other): "+
			// CircuitElm.showFormat.format((mytime-myruntime-mydrawtime)/myframes),10,110);
			// g.drawString("ms per frame (sim): "+
			// CircuitElm.showFormat.format((myruntime)/myframes),10,130);
			// g.drawString("ms per frame (draw): "+
			// CircuitElm.showFormat.format((mydrawtime)/myframes),10,150);

			Graphics.FromImage(cvcontext).DrawImage(backcontext, 0, 0);

			// if we did DC analysis, we need to re-analyze the circuit with that flag
			// cleared.
			if (dcAnalysisFlag) {
				dcAnalysisFlag = false;
				analyzeFlag = true;
			}

			lastFrameTime = lastTime;
			mytime += ElmBase.sw.ElapsedMilliseconds - mystarttime;
			myframes++;
		}

		#region Circuit
		// analyze the circuit when something changes, so it can be simulated
		void analyzeCircuit() {
			if (elmList.Count == 0) {
				postDrawList.Clear();
				badConnectionList.Clear();
				return;
			}
			stopMessage = null;
			stopElm = null;
			nodeList.Clear();
			postCountMap.Clear();

			calculateWireClosure();
			setGroundNode();

			// allocate nodes and voltage sources
			LabeledNodeElm.resetNodeList();
			makeNodeList();

			makePostDrawList();
			if (!calcWireInfo()) {
				return;
			}
			nodeMap.Clear();

			int vscount = 0;
			circuitNonLinear = false;

			// determine if circuit is nonlinear. also set voltage sources
			foreach (var ce in elmList) {
				if (ce.nonLinear()) {
					circuitNonLinear = true;
				}
				var ivs = ce.getVoltageSourceCount();
				for (int j = 0; j != ivs; j++) {
					voltageSources[vscount] = ce;
					ce.setVoltageSource(j, vscount++);
				}
			}
			voltageSourceCount = vscount;

			// show resistance in voltage sources if there's only one.
			// can't use voltageSourceCount here since that counts internal voltage sources,
			// like the one in GroundElm
			var gotVoltageSource = false;
			showResistanceInVoltageSources.Checked = true;
			foreach (var ce in elmList) {
				if (ce is VoltageElm) {
					if (gotVoltageSource)
						showResistanceInVoltageSources.Checked = false;
					else
						gotVoltageSource = true;
				}
			}

			findUnconnectedNodes();
			if (!validateCircuit()) {
				return;
			}

			timeStep = maxTimeStep;
			try {
				stampCircuit();
			} catch (Exception e) {
				stop("Exception in stampCircuit()");
			}
		}

		// find groups of nodes connected by wires and map them to the same node. this speeds things
		// up considerably by reducing the size of the matrix
		void calculateWireClosure() {
			nodeMap = [];
			// int mergeCount = 0;
			wireInfoList.Clear();
			foreach (var ce in elmList) {
				if (ce is not WireElm we) {
					continue;
				}
				we.hasWireInfo = false;
				wireInfoList.Add(new WireInfo(we));
				var cn = nodeMap[ce.getPost(0)];
				var cn2 = nodeMap[ce.getPost(1)];
				if (cn != null && cn2 != null) {
					// merge nodes; go through map and change all keys pointing to cn2 to point to cn
					foreach (var ent in nodeMap) {
						if (ent.Value == cn2)
							nodeMap[ent.Key] = cn;
					}
					// mergeCount++;
					continue;
				}
				if (cn != null) {
					nodeMap.Add(ce.getPost(1), cn);
					continue;
				}
				if (cn2 != null) {
					nodeMap.Add(ce.getPost(0), cn2);
					continue;
				}
				// new entry
				cn = new NodeMapEntry();
				nodeMap.Add(ce.getPost(0), cn);
				nodeMap.Add(ce.getPost(1), cn);
			}

			// console("got " + (groupCount-mergeCount) + " groups with " + nodeMap.size() +
			// " nodes " + mergeCount);
		}

		// find or allocate ground node
		void setGroundNode() {
			var gotGround = false;
			var gotRail = false;
			ElmBase? volt = null;

			// look for voltage or ground element
			foreach (var ce in elmList) {
				if (ce is GroundElm) {
					gotGround = true;
					break;
				}
				if (ce is RailElm) {
					gotRail = true;
				}
				if (volt == null && ce is VoltageElm) {
					volt = ce;
				}
			}

			// if no ground, and no rails, then the voltage elm's first terminal is ground
			if (!gotGround && volt != null && !gotRail) {
				var cn = new CircuitNode();
				var pt = volt.getPost(0);
				nodeList.Add(cn);

				// update node map
				var cln = nodeMap[pt];
				if (cln != null) {
					cln.node = 0;
				} else {
					nodeMap.Add(pt, new NodeMapEntry(0));
				}
			} else {
				// otherwise allocate extra node for ground
				var cn = new CircuitNode();
				nodeList.Add(cn);
			}
		}

		// make list of nodes
		void makeNodeList() {
			int j;
			int vscount = 0;
			foreach (var ce in elmList) {
				var inodes = ce.getInternalNodeCount();
				var ivs = ce.getVoltageSourceCount();
				var posts = ce.getPostCount();

				// allocate a node for each post and match posts to nodes
				for (j = 0; j != posts; j++) {
					var pt = ce.getPost(j);
					var g = postCountMap[pt];
					postCountMap.Add(pt, g == null ? 1 : g + 1);
					var cln = nodeMap[pt];

					// is this node not in map yet? or is the node number unallocated?
					// (we don't allocate nodes before this because changing the allocation order
					// of nodes changes circuit behavior and breaks backward compatibility;
					// the code below to connect unconnected nodes may connect a different node to
					// ground)
					if (cln == null || cln.node == -1) {
						var cn = new CircuitNode();
						var cnl = new CircuitNodeLink
						{
							num = j,
							elm = ce
						};
						cn.links.Add(cnl);
						ce.setNode(j, nodeList.Count);
						if (cln != null) {
							cln.node = nodeList.Count;
						} else {
							nodeMap.Add(pt, new NodeMapEntry(nodeList.Count));
						}
						nodeList.Add(cn);
					} else {
						var n = cln.node;
						var cnl = new CircuitNodeLink
						{
							num = j,
							elm = ce
						};
						getCircuitNode(n).links.Add(cnl);
						ce.setNode(j, n);
						// if it's the ground node, make sure the node voltage is 0,
						// cause it may not get set later
						if (n == 0) {
							ce.setNodeVoltage(j, 0);
						}
					}
				}
				for (j = 0; j != inodes; j++) {
					var cn = new CircuitNode
					{
						@internal = true
					};
					var cnl = new CircuitNodeLink
					{
						num = j + posts,
						elm = ce
					};
					cn.links.Add(cnl);
					ce.setNode(cnl.num, nodeList.Count);
					nodeList.Add(cn);
				}

				// also count voltage sources so we can allocate array
				vscount += ivs;
			}

			voltageSources = new ElmBase[vscount];
		}

		// make list of posts we need to draw. posts shared by 2 elements should be hidden, all
		// others should be drawn. We can't use the node list for this purpose anymore because wires
		// have the same node number at both ends.
		void makePostDrawList() {
			postDrawList.Clear();
			badConnectionList.Clear();
			foreach (var entry in postCountMap) {
				if (entry.Value != 2) {
					postDrawList.Add(entry.Key);
				}
				// look for bad connections, posts not connected to other elements which
				// intersect
				// other elements' bounding boxes
				if (entry.Value == 1) {
					var bad = false;
					var cn = entry.Key;
					foreach (var ce in elmList) {
						if (ce is GraphicElm) {
							continue;
						}
						// does this post intersect elm's bounding box?
						if (!ce.boundingBox.Contains(cn.X, cn.Y)) {
							continue;
						}
						// does this post belong to the elm?
						int k;
						var pc = ce.getPostCount();
						for (k = 0; k != pc; k++) {
							if (ce.getPost(k).Equals(cn)) {
								break;
							}
						}
						if (k == pc) {
							bad = true;
						}
					}
					if (bad) {
						badConnectionList.Add(cn);
					}
				}
			}
			postCountMap.Clear();
		}

		// generate info we need to calculate wire currents. Most other elements
		// calculate currents using
		// the voltage on their terminal nodes. But wires have the same voltage at both
		// ends, so we need
		// to use the neighbors' currents instead. We used to treat wires as zero
		// voltage sources to make
		// this easier, but this is very inefficient, since it makes the matrix 2 rows
		// bigger for each wire.
		// We create a list of WireInfo objects instead to help us calculate the wire
		// currents instead,
		// so we make the matrix less complex, and we only calculate the wire currents
		// when we need them
		// (once per frame, not once per subiteration)
		bool calcWireInfo() {
			int i;
			int moved = 0;
			for (i = 0; i != wireInfoList.Count; i++) {
				var wi = wireInfoList[i];
				var wire = wi.wire;
				var cn1 = nodeList[wire.getNode(0)]; // both ends of wire have same node #
				int j;

				var neighbors0 = new List<ElmBase>();
				var neighbors1 = new List<ElmBase>();
				var isReady0 = true;
				var isReady1 = true;

				// go through elements sharing a node with this wire (may be connected
				// indirectly
				// by other wires, but at least it's faster than going through all elements)
				for (j = 0; j != cn1.links.Count; j++) {
					var cnl = cn1.links[j];
					var ce = cnl.elm;
					if (ce == wire) {
						continue;
					}
					var pt = cnl.elm.getPost(cnl.num);

					// is this a wire that doesn't have wire info yet? If so we can't use it.
					// That would create a circular dependency
					var notReady = (ce is WireElm && !((WireElm)ce).hasWireInfo);

					// which post does this element connect to, if any?
					if (pt.X == wire.x && pt.Y == wire.y) {
						neighbors0.Add(ce);
						if (notReady) {
							isReady0 = false;
						}
					} else if (pt.X == wire.x2 && pt.Y == wire.y2) {
						neighbors1.Add(ce);
						if (notReady) {
							isReady1 = false;
						}
					}
				}

				// does one of the posts have all information necessary to calculate current
				if (isReady0) {
					wi.neighbors = neighbors0;
					wi.post = 0;
					wire.hasWireInfo = true;
					moved = 0;
				} else if (isReady1) {
					wi.neighbors = neighbors1;
					wi.post = 1;
					wire.hasWireInfo = true;
					moved = 0;
				} else {
					// move to the end of the list and try again later
					var tmp = wireInfoList[i];
					wireInfoList.Add(tmp);
					wireInfoList.RemoveAt(i--);
					moved++;
					if (moved > wireInfoList.Count * 2) {
						stop("wire loop detected", wire);
						return false;
					}
				}
			}

			return true;
		}

		void findUnconnectedNodes() {
			int i, j;

			// determine nodes that are not connected indirectly to ground.
			// all nodes must be connected to ground somehow, or else we
			// will get a matrix error.
			var closure = new bool[nodeList.Count];
			var changed = true;
			unconnectedNodes.Clear();
			closure[0] = true;
			while (changed) {
				changed = false;
				foreach (var ce in elmList) {
					if (ce is WireElm) {
						continue;
					}
					// loop through all ce's nodes to see if they are connected
					// to other nodes not in closure
					for (j = 0; j < ce.getConnectionNodeCount(); j++) {
						if (!closure[ce.getConnectionNode(j)]) {
							if (ce.hasGroundConnection(j))
								closure[ce.getConnectionNode(j)] = changed = true;
							continue;
						}
						for (int k = 0; k != ce.getConnectionNodeCount(); k++) {
							if (j == k) {
								continue;
							}
							var kn = ce.getConnectionNode(k);
							if (ce.getConnection(j, k) && !closure[kn]) {
								closure[kn] = true;
								changed = true;
							}
						}
					}
				}
				if (changed) {
					continue;
				}

				// connect one of the unconnected nodes to ground with a big resistor, then try
				// again
				for (i = 0; i != nodeList.Count; i++) {
					if (!closure[i] && !getCircuitNode(i).@internal) {
						unconnectedNodes.Add(i);
						Console.WriteLine("node " + i + " unconnected");
						// stampResistor(0, i, 1e8); // do this later in connectUnconnectedNodes()
						closure[i] = true;
						changed = true;
						break;
					}
				}
			}
		}

		bool validateCircuit() {
			int j;

			foreach (var ce in elmList) {
				// look for inductors with no current path
				if (ce is InductorElm) {
					var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.getNode(1));
					if (!fpi.findPath(ce.getNode(0))) {
						// console(ce + " no path");
						ce.reset();
					}
				}
				// look for current sources with no current path
				if (ce is CurrentElm cur) {
					var fpi = new FindPathInfo(FindPathInfo.INDUCT, ce, ce.getNode(1));
					cur.setBroken(!fpi.findPath(ce.getNode(0)));
				}
				/// TODO:VCCSElm
				//if (ce is VCCSElm) {
				//	var cur = (VCCSElm)ce;
				//	var fpi = new FindPathInfo(this, FindPathInfo.INDUCT, ce, cur.getOutputNode(0));
				//	if (cur.hasCurrentOutput() && !fpi.findPath(cur.getOutputNode(1))) {
				//		cur.broken = true;
				//	} else
				//		cur.broken = false;
				//}

				// look for voltage source or wire loops. we do this for voltage sources or
				// wire-like elements (not actual wires
				// because those are optimized out, so the findPath won't work)
				if (ce.getPostCount() == 2) {
					if (ce is VoltageElm || (ce.isWire() && ce is not WireElm)) {
						var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(1));
						if (fpi.findPath(ce.getNode(0))) {
							stop("Voltage source/wire loop with no resistance!", ce);
							return false;
						}
					}
				} else if (ce is Switch2Elm) {
					// for Switch2Elms we need to do extra work to look for wire loops
					var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(0));
					for (j = 1; j < ce.getPostCount(); j++) {
						if (ce.getConnection(0, j) && fpi.findPath(ce.getNode(j))) {
							stop("Voltage source/wire loop with no resistance!", ce);
							return false;
						}
					}
				}

				// look for path from rail to ground
				if (ce is RailElm || ce is LogicInputElm) {
					var fpi = new FindPathInfo(FindPathInfo.VOLTAGE, ce, ce.getNode(0));
					if (fpi.findPath(0)) {
						stop("Path to ground with no resistance!", ce);
						return false;
					}
				}

				// look for shorted caps, or caps w/ voltage but no R
				if (ce is CapacitorElm) {
					var fpi = new FindPathInfo(FindPathInfo.SHORT, ce, ce.getNode(1));
					if (fpi.findPath(ce.getNode(0))) {
						Console.WriteLine(ce + " shorted");
						((CapacitorElm)ce).shorted();
					} else {
						// a capacitor loop used to cause a matrix error. but we changed the capacitor
						// model
						// so it works fine now. The only issue is if a capacitor is added in parallel
						// with
						// another capacitor with a nonzero voltage; in that case we will get
						// oscillation unless
						// we reset both capacitors to have the same voltage. Rather than check for
						// that, we just
						// give an error.
						fpi = new FindPathInfo(FindPathInfo.CAP_V, ce, ce.getNode(1));
						if (fpi.findPath(ce.getNode(0))) {
							stop("Capacitor loop with no resistance!", ce);
							return false;
						}
					}
				}
			}
			return true;
		}

		// stamp the matrix, meaning populate the matrix as required to simulate the
		// circuit (for all linear elements, at least)
		void stampCircuit() {
			var matrixSize = nodeList.Count - 1 + voltageSourceCount;
			circuitMatrix = new double[matrixSize, matrixSize];
			circuitRightSide = new double[matrixSize];
			nodeVoltages = new double[nodeList.Count - 1];
			if (lastNodeVoltages == null || lastNodeVoltages.Length != nodeVoltages.Length) {
				lastNodeVoltages = new double[nodeList.Count - 1];
			}
			origMatrix = new double[matrixSize, matrixSize];
			origRightSide = new double[matrixSize];
			circuitMatrixSize = circuitMatrixFullSize = matrixSize;
			circuitRowInfo = new RowInfo[matrixSize];
			circuitPermute = new int[matrixSize];
			for (int i = 0; i != matrixSize; i++) {
				circuitRowInfo[i] = new RowInfo();
			}
			circuitNeedsMap = false;

			connectUnconnectedNodes();

			// stamp linear circuit elements
			foreach (var ce in elmList) {
				ce.setParentList(elmList);
				ce.stamp();
			}

			if (!simplifyMatrix(matrixSize)) {
				return;
			}

			// check if we called stop()
			if (circuitMatrix == null) {
				return;
			}

			// if a matrix is linear, we can do the lu_factor here instead of
			// needing to do it every frame
			if (!circuitNonLinear) {
				if (!lu_factor(circuitMatrix, circuitMatrixSize, circuitPermute)) {
					stop("Singular matrix!");
					return;
				}
			}
		}

		// take list of unconnected nodes, which we identified earlier, and connect them to ground
		// with a big resistor. otherwise we will get matrix errors
		void connectUnconnectedNodes() {
			foreach (var n in unconnectedNodes) {
				stampResistor(0, n, 1e8);
			}
		}

		// simplify the matrix; this speeds things up quite a bit, especially for digital circuits.
		// or at least it did before we added wire removal
		bool simplifyMatrix(int matrixSize) {
			int i, j;
			for (i = 0; i != matrixSize; i++) {
				var re = circuitRowInfo[i];

				// if (qp != -100) continue; // uncomment this line to disable matrix
				// simplification for debugging purposes

				if (re.lsChanges || re.dropRow || re.rsChanges) {
					continue;
				}

				// look for rows that can be removed
				var rsadd = 0.0;
				int qp = -1;
				var qv = 0.0;
				for (j = 0; j != matrixSize; j++) {
					var q = circuitMatrix[i, j];
					if (circuitRowInfo[j].type == RowInfo.ROW_CONST) {
						// keep a running total of const values that have been
						// removed already
						rsadd -= circuitRowInfo[j].value * q;
						continue;
					}
					// ignore zeroes
					if (q == 0) {
						continue;
					}
					// keep track of first nonzero element that is not ROW_CONST
					if (qp == -1) {
						qp = j;
						qv = q;
						continue;
					}
					// more than one nonzero element? give up
					break;
				}
				if (j == matrixSize) {
					if (qp == -1) {
						// probably a singular matrix, try disabling matrix simplification above to
						// check this
						stop("Matrix error");
						return false;
					}
					var elt = circuitRowInfo[qp];
					// we found a row with only one nonzero nonconst entry; that value
					// is a constant
					if (elt.type != RowInfo.ROW_NORMAL) {
						Console.WriteLine("type already " + elt.type + " for " + qp + "!");
						continue;
					}
					elt.type = RowInfo.ROW_CONST;
					// console("ROW_CONST " + i + " " + rsadd);
					elt.value = (circuitRightSide[i] + rsadd) / qv;
					circuitRowInfo[i].dropRow = true;
					i = -1; // start over from scratch
				}
			}
			// System.out.println("ac7");

			// find size of new matrix
			int nn = 0;
			for (i = 0; i != matrixSize; i++) {
				var elt = circuitRowInfo[i];
				if (elt.type == RowInfo.ROW_NORMAL) {
					elt.mapCol = nn++;
					// System.out.println("col " + i + " maps to " + elt.mapCol);
					continue;
				}
				if (elt.type == RowInfo.ROW_CONST) {
					elt.mapCol = -1;
				}
			}

			// make the new, simplified matrix
			var newsize = nn;
			var newmatx = new double[newsize, newsize];
			var newrs = new double[newsize];
			int ii = 0;
			for (i = 0; i != matrixSize; i++) {
				var rri = circuitRowInfo[i];
				if (rri.dropRow) {
					rri.mapRow = -1;
					continue;
				}
				newrs[ii] = circuitRightSide[i];
				rri.mapRow = ii;
				// System.out.println("Row " + i + " maps to " + ii);
				for (j = 0; j != matrixSize; j++) {
					var ri = circuitRowInfo[j];
					if (ri.type == RowInfo.ROW_CONST) {
						newrs[ii] -= ri.value * circuitMatrix[i, j];
					} else {
						newmatx[ii, ri.mapCol] += circuitMatrix[i, j];
					}
				}
				ii++;
			}

			// console("old size = " + matrixSize + " new size = " + newsize);

			circuitMatrixSize = newsize;
			circuitMatrix = newmatx;
			for (i = 0; i != newsize; i++) {
				for (j = 0; j != newsize; j++) {
					origMatrix[i, j] = circuitMatrix[i, j];
				}
			}
			circuitRightSide = newrs;
			Buffer.BlockCopy(circuitRightSide, 0, origRightSide, 0, newsize * sizeof(double));

			circuitNeedsMap = true;
			return true;
		}

		CircuitNode getCircuitNode(int n) {
			if (n >= nodeList.Count) {
				throw new Exception($"node {n} out of range");
			}
			return nodeList[n];
		}

		void RunCircuit(bool didAnalyze, double iterCount) {
			if (circuitMatrix == null || elmList.Count == 0) {
				circuitMatrix = null;
				return;
			}

			var debugprint = dumpMatrix;
			dumpMatrix = false;
			var steprate = (long)(160 * iterCount);
			var tm = ElmBase.sw.ElapsedMilliseconds;
			var lit = lastIterTime;
			if (lit == 0) {
				lastIterTime = tm;
				return;
			}

			// Check if we don't need to run simulation (for very slow simulation speeds).
			// If the circuit changed, do at least one iteration to make sure everything is
			// consistent.
			if (1000 >= steprate * (tm - lastIterTime) && !didAnalyze) {
				return;
			}

			var delayWireProcessing = CanDelayWireProcessing();

			var timeStepCountAtFrameStart = timeStepCount;

			// keep track of iterations completed without convergence issues
			var goodIteration = true;
			int goodIterations = 100;
			int iter;
			for (iter = 1; ; iter++) {
				if (goodIterations >= 3 && timeStep < maxTimeStep && goodIteration) {
					// things are going well, double the time step
					timeStep = Math.Min(timeStep * 2, maxTimeStep);
					Console.WriteLine("timestep up = " + timeStep + " at " + t);
					stampCircuit();
					goodIterations = 0;
				}

				foreach (var ce in elmList) {
					ce.startIteration();
				}

				steps++;
				int i, j;
				int subiter;
				int subiterCount = (adjustTimeStep && timeStep / 2 > minTimeStep) ? 100 : 5000;
				for (subiter = 0; subiter != subiterCount; subiter++) {
					converged = true;
					subIterations = subiter;
					// if (t % .030 < .002 && timeStep > 1e-6) // force nonconvergence for debugging
					// converged = false;
					Buffer.BlockCopy(origRightSide, 0, circuitRightSide, 0, circuitRightSide.Length * sizeof(double));
					if (circuitNonLinear) {
						for (i = 0; i != circuitMatrixSize; i++) {
							for (j = 0; j != circuitMatrixSize; j++) {
								circuitMatrix[i, j] = origMatrix[i, j];
							}
						}
					}

					foreach (var ce in elmList) {
						ce.doStep();
					}
					if (stopMessage != null) {
						return;
					}

					var printit = debugprint;
					debugprint = false;
					for (j = 0; j != circuitMatrixSize; j++) {
						for (i = 0; i != circuitMatrixSize; i++) {
							var x = circuitMatrix[i, j];
							if (double.IsNaN(x) || double.IsInfinity(x)) {
								stop("nan/infinite matrix!");
								Console.WriteLine("circuitMatrix " + i + " " + j + " is " + x);
								return;
							}
						}
					}

					if (printit) {
						for (j = 0; j != circuitMatrixSize; j++) {
							var x = "";
							for (i = 0; i != circuitMatrixSize; i++) {
								x += circuitMatrix[j, i] + ",";
							}
							x += "\n";
							Console.WriteLine(x);
						}
						Console.WriteLine("done");
					}

					if (circuitNonLinear) {
						// stop if converged (elements check for convergence in doStep())
						if (converged && subiter > 0) {
							break;
						}
						if (!lu_factor(circuitMatrix, circuitMatrixSize, circuitPermute)) {
							stop("Singular matrix!");
							return;
						}
					}

					lu_solve(circuitMatrix, circuitMatrixSize, circuitPermute, circuitRightSide);
					ApplySolvedRightSide(circuitRightSide);

					if (!circuitNonLinear) {
						break;
					}
				}

				if (subiter == subiterCount) {
					// convergence failed
					goodIterations = 0;
					if (adjustTimeStep) {
						timeStep /= 2;
						Console.WriteLine("timestep down to " + timeStep + " at " + t);
					}
					if (timeStep < minTimeStep || !adjustTimeStep) {
						Console.WriteLine("convergence failed after " + subiter + " iterations");
						stop("Convergence failed!");
						break;
					}
					// we reduced the timestep. reset circuit state to the way it was at start of iteration
					SetNodeVoltages(lastNodeVoltages);
					stampCircuit();
					continue;
				}

				if (subiter > 5 || timeStep < maxTimeStep) {
					Console.WriteLine($"converged after {subiter} iterations, timeStep = {timeStep}");
				}

				if (subiter < 3 && goodIteration) {
					goodIterations++;
				} else {
					goodIterations = 0;
				}

				t += timeStep;
				timeStepAccum += timeStep;
				if (timeStepAccum >= maxTimeStep) {
					timeStepAccum -= maxTimeStep;
					timeStepCount++;
				}
				goodIteration = true;

				foreach (var ce in elmList) {
					ce.stepFinished();
				}
				if (!delayWireProcessing) {
					CalcWireCurrents();
				}

				for (i = 0; i != scopeCount; i++) {
					scopes[i].timeStep();
				}
				foreach (var ce in elmList) {
					if (ce is ScopeElm elm) {
						elm.stepScope();
					}
				}

				// save last node voltages so we can restart the next iteration if necessary
				Buffer.BlockCopy(nodeVoltages, 0, lastNodeVoltages, 0, nodeVoltages.Length * sizeof(double));
				// console("set lastrightside at " + t + " " + lastNodeVoltages);

				tm = ElmBase.sw.ElapsedMilliseconds;
				lit = tm;
				// Check whether enough time has elapsed to perform an *additional* iteration after those we have already completed.
				if ((timeStepCount - timeStepCountAtFrameStart) * 1000 >= steprate * (tm - lastIterTime) || (tm - lastFrameTime > 500)) {
					break;
				}
				if (!simRunning) {
					break;
				}
			} // for (iter = 1; ; iter++)
			lastIterTime = lit;
			if (delayWireProcessing) {
				CalcWireCurrents();
			}
			// System.out.println((System.currentTimeMillis()-lastFrameTime)/(double) iter);
		}

		// we need to calculate wire currents for every iteration if someone is viewing a wire in the scope.
		// Otherwise we can do it only once per frame.
		bool CanDelayWireProcessing() {
			for (int i = 0; i != scopeCount; i++) {
				if (scopes[i].viewingWire()) {
					return false;
				}
			}
			foreach (var ce in elmList) {
				if (ce is ScopeElm elm && elm.elmScope.viewingWire()) {
					return false;
				}
			}
			return true;
		}

		// set node voltages given right side found by solving matrix
		void ApplySolvedRightSide(double[] rs) {
			for (int j = 0; j != circuitMatrixFullSize; j++) {
				var ri = circuitRowInfo[j];
				var res = (ri.type == RowInfo.ROW_CONST) ? ri.value : rs[ri.mapCol];
				if (double.IsNaN(res)) {
					converged = false;
					break;
				}
				if (j < nodeList.Count - 1) {
					nodeVoltages[j] = res;
				} else {
					var ji = j - (nodeList.Count - 1);
					voltageSources[ji].setCurrent(ji, res);
				}
			}
			SetNodeVoltages(nodeVoltages);
		}

		// set node voltages in each element given an array of node voltages
		void SetNodeVoltages(double[] nv) {
			for (int j = 0; j != nv.Length; j++) {
				var res = nv[j];
				var cn = nodeList[j + 1];
				for (int k = 0; k != cn.links.Count; k++) {
					var cnl = cn.links[k];
					cnl.elm.setNodeVoltage(cnl.num, res);
				}
			}
		}

		// we removed wires from the matrix to speed things up. in order to display wire currents,
		// we need to calculate them now.
		void CalcWireCurrents() {
			foreach (var wi in wireInfoList) {
				var cur = 0.0;
				var p = wi.wire.getPost(wi.post);
				foreach (var ce in wi.neighbors) {
					var n = ce.getNodeAtPoint(p.X, p.Y);
					cur += ce.getCurrentIntoNode(n);
				}
				wi.wire.setCurrent(-1, wi.post == 0 ? cur : -cur);
			}
		}

		public void updateVoltageSource(int n1, int n2, int vs, double v) {
			var vn = nodeList.Count + vs;
			stampRightSide(vn, v);
		}

		public void stampVoltageSource(int n1, int n2, int vs, double v) {
			var vn = nodeList.Count + vs;
			stampMatrix(vn, n1, -1);
			stampMatrix(vn, n2, 1);
			stampRightSide(vn, v);
			stampMatrix(n1, vn, 1);
			stampMatrix(n2, vn, -1);
		}

		public void stampVoltageSource(int n1, int n2, int vs) {
			var vn = nodeList.Count + vs;
			stampMatrix(vn, n1, -1);
			stampMatrix(vn, n2, 1);
			stampRightSide(vn);
			stampMatrix(n1, vn, 1);
			stampMatrix(n2, vn, -1);
		}

		public void stampCurrentSource(int n1, int n2, double i) {
			stampRightSide(n1, -i);
			stampRightSide(n2, i);
		}

		public void stampResistor(int n1, int n2, double r) {
			var g = 1.0 / r;
			if (double.IsNaN(g) || double.IsInfinity(g)) {
				Console.WriteLine($"bad resistance {r} {g}");
				g = 1e-8;
			}
			stampMatrix(n1, n1, g);
			stampMatrix(n2, n2, g);
			stampMatrix(n1, n2, -g);
			stampMatrix(n2, n1, -g);
		}

		public void stampMatrix(int i, int j, double x) {
			if (double.IsInfinity(x)) {
				Console.WriteLine($"bad matrix[{i},{j}] = {x}");
			}
			if (i > 0 && j > 0) {
				if (circuitNeedsMap) {
					i = circuitRowInfo[i - 1].mapRow;
					var ri = circuitRowInfo[j - 1];
					if (ri.type == RowInfo.ROW_CONST) {
						// System.out.println("Stamping constant " + i + " " + j + " " + x);
						circuitRightSide[i] -= x * ri.value;
						return;
					}
					j = ri.mapCol;
					// System.out.println("stamping " + i + " " + j + " " + x);
				} else {
					i--;
					j--;
				}
				circuitMatrix[i, j] += x;
			}
		}

		public void stampNonLinear(int i) {
			if (i > 0) {
				circuitRowInfo[i - 1].lsChanges = true;
			}
		}

		public void stampRightSide(int i, double x) {
			if (i > 0) {
				if (circuitNeedsMap) {
					i = circuitRowInfo[i - 1].mapRow;
				} else {
					i--;
				}
				circuitRightSide[i] += x;
			}
		}

		// indicate that the value on the right side of row i changes in doStep()
		public void stampRightSide(int i) {
			if (i > 0) {
				circuitRowInfo[i - 1].rsChanges = true;
			}
		}

		// factors a matrix into upper and lower triangular matrices by
		// gaussian elimination. On entry, a[0..n-1][0..n-1] is the
		// matrix to be factored. ipvt[] returns an integer vector of pivot
		// indices, used in the lu_solve() routine.
		public static bool lu_factor(double[,] a, int n, int[] ipvt) {
			int i, j, k;

			// check for a possible singular matrix by scanning for rows that are all zeroes
			for (i = 0; i != n; i++) {
				var row_all_zeros = true;
				for (j = 0; j != n; j++) {
					if (a[i, j] != 0) {
						row_all_zeros = false;
						break;
					}
				}
				// if all zeros, it's a singular matrix
				if (row_all_zeros) {
					return false;
				}
			}

			// use Crout's method; loop through the columns
			for (j = 0; j != n; j++) {

				// calculate upper triangular elements for this column
				for (i = 0; i != j; i++) {
					var q = a[i, j];
					for (k = 0; k != i; k++) {
						q -= a[i, k] * a[k, j];
					}
					a[i, j] = q;
				}

				// calculate lower triangular elements for this column
				var largest = 0.0;
				int largestRow = -1;
				for (i = j; i != n; i++) {
					var q = a[i, j];
					for (k = 0; k != j; k++) {
						q -= a[i, k] * a[k, j];
					}
					a[i, j] = q;
					var x = Math.Abs(q);
					if (x >= largest) {
						largest = x;
						largestRow = i;
					}
				}

				// pivoting
				if (j != largestRow) {
					for (k = 0; k != n; k++) {
						(a[j, k], a[largestRow, k]) = (a[largestRow, k], a[j, k]);
					}
				}

				// keep track of row interchanges
				ipvt[j] = largestRow;

				// avoid zeros
				if (a[j, j] == 0.0) {
					Console.WriteLine("avoided zero");
					a[j, j] = 1e-18;
				}

				if (j != n - 1) {
					var mult = 1.0 / a[j, j];
					for (i = j + 1; i != n; i++) {
						a[i, j] *= mult;
					}
				}
			}
			return true;
		}

		// Solves the set of n linear equations using a LU factorization
		// previously performed by lu_factor. On input, b[0..n-1] is the right
		// hand side of the equations, and on output, contains the solution.
		public static void lu_solve(double[,] a, int n, int[] ipvt, double[] b) {
			int i, j;

			// find first nonzero b element
			for (i = 0; i != n; i++) {
				var row = ipvt[i];
				var swap = b[row];
				b[row] = b[i];
				b[i] = swap;
				if (swap != 0) {
					break;
				}
			}

			int bi = i++;
			for (; i < n; i++) {
				var row = ipvt[i];
				var tot = b[row];
				b[row] = b[i];
				// forward substitution using the lower triangular matrix
				for (j = bi; j < i; j++) {
					tot -= a[i, j] * b[j];
				}
				b[i] = tot;
			}
			for (i = n - 1; i >= 0; i--) {
				var tot = b[i];
				// back-substitution using the upper triangular matrix
				for (j = i + 1; j != n; j++) {
					tot -= a[i, j] * b[j];
				}
				b[i] = tot / a[i, i];
			}
		}
		#endregion
	}
}
