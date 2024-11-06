using System.Collections.Generic;
using System.Reflection;
using GWOO.Systems.Attributes;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using GWOO.UIElements;

namespace GWOO.Editor.Attributes
{
    public class MethodButtonsDrawer
    {
        private readonly List<MethodButton> _buttons;

        public MethodButtonsDrawer(Object target)
        {
            _buttons = new List<MethodButton>();

            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | 
                BindingFlags.Static | 
                BindingFlags.Public | 
                BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();

                if (buttonAttribute == null)
                {
                    continue;
                }

                _buttons.Add(new MethodButton(method, buttonAttribute));
            }
        }

        public void DrawButtons(Object target, VisualElement root)
        {
            foreach (MethodButton button in _buttons)
            {
                ButtonAttribute buttonAttribute = button.ButtonAttribute;
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.Label) ? button.Method.Name : buttonAttribute.Label;

                CustomButton uiButton = new(() => button.Method.Invoke(target, null))
                {
                    text = buttonLabel,
                    Width = buttonAttribute.Width
                };
                
                uiButton.AddClasses(buttonAttribute.Styles);

                root.Add(uiButton);
            }
        }
    }
}