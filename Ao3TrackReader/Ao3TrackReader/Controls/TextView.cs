using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;


namespace Ao3TrackReader.Controls
{
    public class TextView : Xamarin.Forms.Label
    {
        public static readonly Xamarin.Forms.BindableProperty TextTreeProperty =
          Xamarin.Forms.BindableProperty.Create("TextTree", typeof(Models.TextTree), typeof(TextView), defaultValue: null);

        public Models.TextTree TextTree
        {
            get { return (Models.TextTree)GetValue(TextTreeProperty); }
            set { SetValue(TextTreeProperty, value); }
        }

        public new string Text
        {
            get { return ((Models.TextTree)GetValue(TextTreeProperty)).ToString(); }
            set { SetValue(TextTreeProperty, (Models.TextTree) value); }
        }

        public TextView()
        {
            FormattedText = "";
        }
    }
}
