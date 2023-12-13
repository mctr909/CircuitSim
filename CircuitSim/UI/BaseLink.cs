using System.Collections.Generic;

using Circuit.Elements;

namespace Circuit.UI {
    public class BaseLink {
        public virtual int GetGroup(int id) { return 0; }
        public virtual void SetValue(IElement element, int linkID, double value) { }
        public virtual void Load(StringTokenizer st) { }
        public virtual void Dump(List<object> optionList) { }
    }
}
