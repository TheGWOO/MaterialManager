using System.Reflection;
using GWOO.Systems.Attributes;

namespace GWOO.Editor.Attributes
{
    public class MethodButton
    {
        public readonly MethodInfo Method;
        public readonly ButtonAttribute ButtonAttribute;

        public MethodButton(MethodInfo method, ButtonAttribute buttonAttribute)
        {
            Method = method;
            ButtonAttribute = buttonAttribute;
        }
    }
}