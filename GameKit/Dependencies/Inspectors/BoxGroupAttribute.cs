using System;

namespace GameKit.Dependencies.Inspectors
{

#if !ODIN_INSPECTOR
    public class BoxGroupAttribute : Attribute
    {
        public BoxGroupAttribute(string name) { }
        public BoxGroupAttribute(string name, int index) { }
    }
#endif


}
