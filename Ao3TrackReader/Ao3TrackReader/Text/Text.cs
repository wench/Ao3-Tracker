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
using System.Text;

namespace Ao3TrackReader.Text
{
    public abstract partial class Text
    {
        public double? FontSize { get; set; }
        public bool? Bold { get; set; }
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }
        public bool? Strike { get; set; }
        public bool? Sub { get; set; }
        public bool? Super { get; set; }
        public Xamarin.Forms.Color? Foreground { get; set; }

        public static implicit operator Text(string str)
        {
            return new String { Text = str };
        }
        public void ApplyState(Text state)
        {
            if (FontSize == null) FontSize = state.FontSize;
            if (Bold == null) Bold = state.Bold;
            if (Italic == null) Italic = state.Italic;
            if (Strike == null) Strike = state.Strike;
            if (Sub == null) Sub = state.Sub;
            if (Super == null) Super = state.Super;
            if (Foreground == null) Foreground = state.Foreground;
        }

        public abstract override string ToString();

        public abstract ICollection<String> Flatten(StateNode state);
        public Span FlattenToSpan(StateNode state = null)
        {
            var flat = Flatten(state ?? new StateNode());
            return new Span(flat.TrimNewLines());
        }

        public abstract bool IsEmpty { get; }
    }
}
