using System.Collections.Generic;
using System.Drawing;

using Circuit.UI;

namespace Circuit.Common {
    public class ScopePlot {
        public enum E_COLOR { INVALID }

        public BaseUI UI;
        public List<ScopeWave> Waves = new List<ScopeWave>();
        public string Text;

        public int Index { get; set; }
        public int StackCount { get; set; }
        public int SelectedWave { get; set; }
        public int Speed { get; set; }
        public double ScaleValue { get; set; }
        public int MouseCursorX { get; set; }
        public int MouseCursorY { get; set; }
        public Rectangle BoundingBox { get; set; }
        public int Right { get { return 0; } }

        public bool CanMenu { get; set; }
        public bool NeedToRemove { get; set; }
        public bool ManualScale { get; set; }
        public bool ShowScale { get; set; }
        public bool ShowMax { get; set; }
        public bool ShowMin { get; set; }
        public bool ShowRMS { get; set; }
        public bool ShowFreq { get; set; }
        public bool ShowVoltage { get; set; }
        public bool ShowFFT { get; set; }
        public bool LogSpectrum { get; set; }

        public string Dump() { return ""; }

        public void Undump(StringTokenizer st) { }

        public void SetUI(BaseUI ui) { }

        public void SetRect(Rectangle rect) { }

        public int CalcGridStepX() { return 0; }

        public void Properties(int x, int y) { }

        public void Combine(ScopePlot plot) { }

        public void SpeedUp() { }

        public void SlowDown() { }

        public void MaxScale() { }

        public void ResetGraph(bool f = false) { }

        public void TimeStep() { }

        public void Draw(CustomGraphics g, bool f = false) { }
    }
}
