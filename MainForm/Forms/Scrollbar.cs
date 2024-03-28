namespace MainForm.Forms;

public class Scrollbar : HScrollBar {
	public const int HORIZONTAL = 1;
	public const int VERTICAL = 2;
	public const int HMARGIN = 2;
	public const int SCROLLHEIGHT = 14;
	public const int BARWIDTH = 3;
	public const int BARMARGIN = 3;

	public new double Value;

	public Scrollbar(int orientation, int value, int visible, int minimum, int maximum) { }

	public Scrollbar(int orientation, int value, int visible, int minimum, int maximum, object cmd, ElmBase e) { }

	public void onMouseWheel(MouseEventArgs e) { }

	public void draw() { }
}
