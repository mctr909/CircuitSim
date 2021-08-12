using System;
using System.Collections.Generic;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    class Hint {
        public const int HINT_LC = 1;
        public const int HINT_RC = 2;
        public const int HINT_3DB_C = 3;
        public const int HINT_TWINT = 4;
        public const int HINT_3DB_L = 5;

        public static int Type = -1;
        public static int Item1;
        public static int Item2;

        static CircuitElm getElm(List<CircuitElm> elmList, int n) {
            if (n >= elmList.Count) {
                return null;
            }
            return elmList[n];
        }

        public static string getHint(List<CircuitElm> elmList) {
            var c1 = getElm(elmList, Item1);
            var c2 = getElm(elmList, Item2);
            if (c1 == null || c2 == null) {
                return null;
            }

            if (Type == HINT_LC) {
                if (!(c1 is InductorElm)) {
                    return null;
                }
                if (!(c2 is CapacitorElm)) {
                    return null;
                }
                var ie = (InductorElm)c1;
                var ce = (CapacitorElm)c2;
                return "res.f = " + Utils.UnitText(1 / (2 * Math.PI * Math.Sqrt(ie.Inductance * ce.Capacitance)), "Hz");
            }
            if (Type == HINT_RC) {
                if (!(c1 is ResistorElm)) {
                    return null;
                }
                if (!(c2 is CapacitorElm)) {
                    return null;
                }
                var re = (ResistorElm)c1;
                var ce = (CapacitorElm)c2;
                return "RC = " + Utils.UnitText(re.Resistance * ce.Capacitance, "s");
            }
            if (Type == HINT_3DB_C) {
                if (!(c1 is ResistorElm)) {
                    return null;
                }
                if (!(c2 is CapacitorElm)) {
                    return null;
                }
                var re = (ResistorElm)c1;
                var ce = (CapacitorElm)c2;
                return "f.3db = " + Utils.UnitText(1 / (2 * Math.PI * re.Resistance * ce.Capacitance), "Hz");
            }
            if (Type == HINT_3DB_L) {
                if (!(c1 is ResistorElm)) {
                    return null;
                }
                if (!(c2 is InductorElm)) {
                    return null;
                }
                var re = (ResistorElm)c1;
                var ie = (InductorElm)c2;
                return "f.3db = " + Utils.UnitText(re.Resistance / (2 * Math.PI * ie.Inductance), "Hz");
            }
            if (Type == HINT_TWINT) {
                if (!(c1 is ResistorElm)) {
                    return null;
                }
                if (!(c2 is CapacitorElm)) {
                    return null;
                }
                var re = (ResistorElm)c1;
                var ce = (CapacitorElm)c2;
                return "fc = " + Utils.UnitText(1 / (2 * Math.PI * re.Resistance * ce.Capacitance), "Hz");
            }
            return null;
        }
    }
}
