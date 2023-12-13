using System.Collections.Generic;

namespace Circuit {
    public class BaseLink {
        public virtual int GetGroup(int id) { return 0; }
        public virtual void SetValue(BaseElement element, int linkID, double value) { }
        public virtual void Load(StringTokenizer st) { }
        public virtual void Dump(List<object> optionList) { }
    }
}
