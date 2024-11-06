using UnityEngine.UIElements;

namespace GWOO.UIElements
{
    public sealed class CustomButton : Button
    {
        public float Width
        {
            get => GetWidth();
            set => SetWidthPercent(value);
        }

        public CustomButton(params string[] styles) : this(null, styles) {}
        public CustomButton(System.Action clickEvent, params string[] styles) : base(clickEvent)
        {
            name = "CustomButton";
            
            RemoveFromClassList("unity-button");
            AddToClassList("custom-button");
            
            focusable = false;
            this.AddClasses(styles);
        }
        
        private float GetWidth()
        {
            return style.width.value.value; // Return the float type value of the Length
        }

        private void SetWidthPercent(float width)
        {
            style.width = width == 0 ? 
                new StyleLength(Length.Auto()) : 
                new StyleLength(Length.Percent(width));        
        }
    }
}