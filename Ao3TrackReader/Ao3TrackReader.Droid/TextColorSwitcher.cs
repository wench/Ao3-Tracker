using Android.Content.Res;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Ao3TrackReader.Droid
{
    public class TextColorSwitcher
    {
        static readonly int[][] s_colorStates = { new[] { global::Android.Resource.Attribute.StateEnabled }, new[] { -global::Android.Resource.Attribute.StateEnabled }};

        readonly int[] _defaultTextColors;
        Color _currentTextColor;
        bool wasActive = false;

        public TextColorSwitcher(ColorStateList textColors)
        {
            int defaultEnabledColor = textColors.GetColorForState(s_colorStates[0], Ao3TrackReader.Resources.Colors.Base.MediumHigh.ToAndroid());
            int defaultDisabledColor = textColors.GetColorForState(s_colorStates[1], Ao3TrackReader.Resources.Colors.Base.Low.ToAndroid());
            _defaultTextColors = new int[] { defaultEnabledColor, defaultDisabledColor };
        }

        public void UpdateTextColor(Android.Widget.TextView control, Color color, bool active)
        {
            if (color == _currentTextColor && active == wasActive)
                return;

            _currentTextColor = color;
            wasActive = active;

            if (active)
            {
                control.SetTextColor(new ColorStateList(s_colorStates, new int[] { Ao3TrackReader.Resources.Colors.Highlight.High.ToAndroid(), Ao3TrackReader.Resources.Colors.Highlight.High.ToAndroid() }));
            }
            else if (color.IsDefault)
            {
                control.SetTextColor(new ColorStateList(s_colorStates, _defaultTextColors));
            }
            else
            {
                // Set the new enabled state color, preserving the default disabled state color
                control.SetTextColor(new ColorStateList(s_colorStates, new int[] { color.ToAndroid().ToArgb(), _defaultTextColors[1], Ao3TrackReader.Resources.Colors.Highlight.High.ToAndroid(), Ao3TrackReader.Resources.Colors.Highlight.High.ToAndroid() }));
            }
        }

    }
}