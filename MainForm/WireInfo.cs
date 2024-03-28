namespace MainForm;

internal class WireInfo {
	public WireElm wire;
	public List<ElmBase> neighbors;
	public int post;

	public WireInfo(WireElm w) {
		wire = w;
	}
}
