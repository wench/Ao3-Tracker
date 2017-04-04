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
    public partial class Block : Span
    {
        public Block() : base()
        {
        }
        public Block(IEnumerable<TextEx> from) : base(from)
        {
        }

        public override string ToString()
        {
            bool lastisblock = Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() == typeof(Block);
            return base.ToString() + (!lastisblock?"\n\n":"");
        }

        public override ICollection<String> Flatten(StateNode state)
        {
            var res = base.Flatten(state) as List<String>;
            if (Nodes.Count > 0 && Nodes[Nodes.Count - 1].GetType() != typeof(Block))
            {
                var linebreaks = new String { Text = "\n\n" };
                linebreaks.ApplyState(this);
                linebreaks.ApplyState(state);
                res.Add(linebreaks);
            }
            return res;
        }

    }
}
