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
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public class ContentView<T> : ContentView
        where T : View
    {
        new public T Content {
            get => (T)base.Content;
            set => base.Content = value;
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Content")
            {
                if (!(base.Content is null || base.Content is T))
                {
                    throw new InvalidCastException("Content must be of type " + typeof(T).Name);
                }
            }
        }
    }
}
