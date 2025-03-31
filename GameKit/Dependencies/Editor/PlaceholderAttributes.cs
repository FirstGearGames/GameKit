using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
#if !ODIN_INSPECTOR

    public enum ButtonSizes
    {
        Large
    }

    public class TabGroupAttribute : PropertyAttribute
    {
        public TabGroupAttribute(string name, bool foldEverything = false) { }
    }
    
    public class BoxGroupAttribute : PropertyAttribute
    {
        public BoxGroupAttribute(string name) { }
    }

    public class PropertySpaceAttribute : Attribute
    {
        public PropertySpaceAttribute() { }
    }


    public class IndentAttribute : PropertyAttribute
    {
        public IndentAttribute(int value) { }
    }

    public class ButtonAttribute : Attribute
    {

        public ButtonAttribute(string label, ButtonSizes size) { }
    }
    public class ShowIfAttribute : PropertyAttribute
    {
        public enum DisablingType
        {
            ReadOnly = 2,
            DontDraw = 3
        }

        public ShowIfAttribute(string comparedPropertyName, object comparedValue, DisablingType disablingType = DisablingType.DontDraw) { }
    }

#endif
}