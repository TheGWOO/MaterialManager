using UnityEngine.UIElements;

namespace GWOO.UIElements
{
    public class CustomToggle : Toggle
    {
        public CustomToggle(params string[] style)
        {
            name = "CustomToggle";
            
            RemoveFromClassList("unity-toggle");
            this.AddClasses(style);
        }
    }
}