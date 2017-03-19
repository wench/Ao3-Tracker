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
    [Xamarin.Forms.ContentProperty("Nodes")]
    public partial class Span : Text
    {
        public Span()
        {
            Nodes = new List<Text>();
        }
        public Span(IEnumerable<Text> from)
        {
            Nodes = new List<Text>(from);
        }

        public IList<Text> Nodes { get; private set; }

        public override string ToString()
        {
            return string.Join("", Nodes);
        }
        public override ICollection<String> Flatten(StateNode state)
        {
            var newstate = new StateNode();
            newstate.ApplyState(this);
            newstate.ApplyState(state);

            List<String> res = new List<String>(Nodes.Count + 1);
            foreach (var node in Nodes)
            {
                res.AddRange(node.Flatten(newstate));
            }

            return res;
        }

        public override bool IsEmpty
        {
            get
            {
                foreach (var node in Nodes)
                    if (!node.IsEmpty)
                        return false;
                return true;
            }
        }
    }
}
