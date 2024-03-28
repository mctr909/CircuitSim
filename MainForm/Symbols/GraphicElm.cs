namespace MainForm;

internal class GraphicElm : ElmBase {
	public GraphicElm(int xx, int yy) : base(xx, yy) {
	}

	public GraphicElm(int xa, int ya, int xb, int yb, int flags) : base(xa, ya, xb, yb, flags) {
	}

	public override int getPostCount() {
		return 0;
	}
}
