using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class BackgroundSwitch : BindableObject
    {
        public static readonly BindableProperty ColorFalseProperty =
             BindableProperty.CreateAttached("ColorFalse", typeof(Color),
                                      typeof(BackgroundSwitch),
                                      Color.Default, propertyChanged: onPropertyChanged);

        public static readonly BindableProperty ColorTrueProperty =
             BindableProperty.CreateAttached("ColorTrue", typeof(Color),
                                      typeof(BackgroundSwitch),
                                      Color.Default, propertyChanged: onPropertyChanged);

        public static readonly BindableProperty ConditionProperty =
             BindableProperty.CreateAttached("Condition", typeof(bool),
                                      typeof(BackgroundSwitch
                 ),
                                      false, propertyChanged: onPropertyChanged);
        
        static void onPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            var elem = bindable as VisualElement;
            if (elem != null)
            {
                elem.BackgroundColor = GetCondition(elem) ? GetColorTrue(elem) : GetColorFalse (elem);
            }
        }

        static bool GetCondition(VisualElement obj) { return (bool)obj.GetValue(ConditionProperty); }
        static void SetCondition(VisualElement obj, bool value) { obj.SetValue(ConditionProperty, value); }
        static Color GetColorTrue(VisualElement obj) { return (Color)obj.GetValue(ColorTrueProperty); }
        static void SetColorTrue (VisualElement obj, Color value) { obj.SetValue(ColorTrueProperty, value); }
        static Color GetColorFalse(VisualElement obj) { return (Color)obj.GetValue(ColorFalseProperty); }
        static void SetColorFalse(VisualElement obj, Color value) { obj.SetValue(ColorFalseProperty, value); }
    }
}
