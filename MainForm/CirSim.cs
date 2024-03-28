namespace MainForm;

public class CirSim {
	public const int MODE_ADD_ELM = 0;
	public const int MODE_DRAG_ALL = 1;
	public const int MODE_DRAG_ROW = 2;
	public const int MODE_DRAG_COLUMN = 3;
	public const int MODE_DRAG_SELECTED = 4;
	public const int MODE_DRAG_POST = 5;
	public const int MODE_SELECT = 6;
	public const int MODE_DRAG_SPLITTER = 7;
	public int mouseMode = MODE_SELECT;
	private int tempMouseMode = MODE_SELECT;

	public CheckBox voltsCheckItem;
	public CheckBox dotsCheckItem;
	public CheckBox powerCheckItem;
	public CheckBox euroResistorCheckItem;
	public CheckBox showValuesCheckItem;
	public CheckBox conventionCheckItem;

	public Forms.EditDialog? editDialog;
	public List<ElmBase> elmList = [];
	public ElmBase plotYElm;
	public ElmBase dragElm;

	public int gridSize;

	public bool analyzeFlag;
	public bool dcAnalysisFlag;
	public double timeStep;

	public TrackBar findAdjustable(ElmBase elm, int i) { return null; }

	public void deleteSliders(ElmBase elm) { }

	public void needAnalyze() { }

	public ElmBase getElm(int i) { return null; }

	public int snapGrid(int x) { return 0; }

	public bool simIsRunning() { return false; }

	public void setiFrameHeight() { }

	public void addWidgetToVerticalPanel(Control w) { }

	public void removeWidgetFromVerticalPanel(Control w) { }

	public void stampResistor(int n1, int n2, double resistance) { }

	// indicate that the value on the right side of row i changes in doStep()
	public void stampRightSide(int i) { }

	public void stampCurrentSource(int n1, int n2, double i) { }
}
