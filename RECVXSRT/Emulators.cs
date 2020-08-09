using System.Collections.Generic;
using System.Reflection;

namespace RECVXSRT
{
    public static class Emulators
    {
        public const string RPCS3 = "rpcs3";
        public const string PCSX2 = "pcsx2";

        public static List<string> GetList()
        {
            List<string> list = new List<string>();

            foreach (FieldInfo field in typeof(Emulators).GetFields())
                if (field.IsLiteral && !field.IsInitOnly)
                    list.Add((string)field.GetValue(null));

            return list;
        }
    }
}