using System;
using System.Collections.Generic;

namespace Circuit.Elements {
    class ExtListEntry {
        public string name;
        public int node;
        public int pos;
        public int side;

        public ExtListEntry(string s, int n) {
            name = s;
            node = n;
            side = ChipElm.SIDE_W;
        }

        public ExtListEntry(string s, int n, int p, int sd) {
            name = s;
            node = n;
            pos = p;
            side = sd;
        }
    }

    class CustomCompositeModel : IComparable<CustomCompositeModel> {
        static Dictionary<string, CustomCompositeModel> modelMap;

        int flags;

        public List<ExtListEntry> ExtList { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public string Name { get; private set; }
        public string NodeList { get; set; }
        public string ElmDump { get; set; }
        public bool Dumped { get; private set; }

        public void setName(string n) {
            modelMap.Remove(Name);
            Name = n;
            modelMap.Add(Name, this);
        }

        public static CustomCompositeModel getModelWithName(string name) {
            if (modelMap == null) {
                modelMap = new Dictionary<string, CustomCompositeModel>();

                /* create default stub model */
                var extList = new List<ExtListEntry>();
                extList.Add(new ExtListEntry("gnd", 1));
                var d = createModel("default", "0", ELEMENTS.GROUND + " 1", extList);
                d.SizeX = d.SizeY = 1;
                modelMap.Add(d.Name, d);
            }
            var lm = modelMap[name];
            return lm;
        }

        static CustomCompositeModel createModel(string name, string elmDump, string nodeList, List<ExtListEntry> extList) {
            var lm = new CustomCompositeModel();
            lm.Name = name;
            lm.ElmDump = elmDump;
            lm.NodeList = nodeList;
            lm.ExtList = extList;
            modelMap.Add(name, lm);
            return lm;
        }

        public static void clearDumpedFlags() {
            if (modelMap == null) {
                return;
            }
            foreach (var key in modelMap.Keys) {
                modelMap[key].Dumped = false;
            }
        }

        public static List<CustomCompositeModel> getModelList() {
            var vector = new List<CustomCompositeModel>();
            foreach (var key in modelMap.Keys) {
                var dm = modelMap[key];
                vector.Add(dm);
            }
            vector.Sort();
            return vector;
        }

        public int CompareTo(CustomCompositeModel dm) {
            return Name.CompareTo(dm.Name);
        }

        public static void undumpModel(StringTokenizer st) {
            string name = CustomLogicModel.unescape(st.nextToken());
            CustomCompositeElm.lastModelName = name;
            var model = getModelWithName(name);
            if (model == null) {
                model = new CustomCompositeModel();
                model.Name = name;
                modelMap.Add(name, model);
            }
            model.undump(st);
        }

        void undump(StringTokenizer st) {
            flags = st.nextTokenInt();
            SizeX = st.nextTokenInt();
            SizeY = st.nextTokenInt();
            int extCount = st.nextTokenInt();
            int i;
            ExtList = new List<ExtListEntry>();
            for (i = 0; i != extCount; i++) {
                string s = CustomLogicModel.unescape(st.nextToken());
                int n = st.nextTokenInt();
                int p = st.nextTokenInt();
                int sd = st.nextTokenInt();
                ExtList.Add(new ExtListEntry(s, n, p, sd));
            }
            NodeList = CustomLogicModel.unescape(st.nextToken());
            ElmDump = CustomLogicModel.unescape(st.nextToken());
        }

        string arrayToList(string[] arr) {
            if (arr == null) {
                return "";
            }
            if (arr.Length == 0) {
                return "";
            }
            string x = arr[0];
            int i;
            for (i = 1; i < arr.Length; i++) {
                x += "," + arr[i];
            }
            return x;
        }

        string[] listToArray(string arr) {
            return arr.Split(',');
        }

        public string dump() {
            Dumped = true;
            string str = ". " + CustomLogicModel.escape(Name) + " 0 " + SizeX + " " + SizeY + " " + ExtList.Count + " ";
            int i;
            for (i = 0; i != ExtList.Count; i++) {
                var ent = ExtList[i];
                if (i > 0) {
                    str += " ";
                }
                str += CustomLogicModel.escape(ent.name) + " " + ent.node + " " + ent.pos + " " + ent.side;
            }
            str += " " + CustomLogicModel.escape(NodeList) + " " + CustomLogicModel.escape(ElmDump);
            return str;
        }
    }
}
