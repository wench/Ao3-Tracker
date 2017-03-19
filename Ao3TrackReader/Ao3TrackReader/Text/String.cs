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
    public partial class String : TextEx
    {
        public String()
        {
        }
        public String(string text)
        {
            Text = text;
        }
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }

        public override ICollection<String> Flatten(StateNode state)
        {
            String res = (String) this.MemberwiseClone();
            res.ApplyState(state);
            return new[] { res };
        }

        public override bool IsEmpty
        {
            get { return string.IsNullOrEmpty(Text); }
        }
    }    
}
