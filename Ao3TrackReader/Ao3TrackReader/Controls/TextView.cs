/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;


namespace Ao3TrackReader.Controls
{
    [Xamarin.Forms.ContentProperty("TextEx")]
    public class TextView : Xamarin.Forms.Label
    {
        public static readonly Xamarin.Forms.BindableProperty TextExProperty =
          Xamarin.Forms.BindableProperty.Create("TextEx", typeof(Text.TextEx), typeof(TextView), defaultValue: null);

        public Text.TextEx TextEx
        {
            get { return (Text.TextEx)GetValue(TextExProperty); }
            set { SetValue(TextExProperty, value); }
        }

        public TextView()
        {
            FormattedText = "";
        }
    }
}
