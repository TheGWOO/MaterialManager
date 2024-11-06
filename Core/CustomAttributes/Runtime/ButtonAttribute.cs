using System;

namespace GWOO.Systems.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public string[] Styles { get; }
        public float Width { get; }

        public ButtonAttribute(string label = null, float width = default, params string[] styles)
        {
            Label = label;
            Styles = styles;
            Width = width;
        }
    }
}