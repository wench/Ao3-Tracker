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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ao3TrackReader.Text
{
    [Xamarin.Forms.ContentProperty("Nodes")]
    public partial class Span : TextEx, ICollection<TextEx>
    {
        public Span()
        {
            Nodes = new List<TextEx>();
        }
        public Span(IEnumerable<TextEx> from)
        {
            Nodes = new List<TextEx>(from);
        }

        public IList<TextEx> Nodes { get; private set; }

        public bool Pad { get; set; } = false;

        public override string ToString()
        {
            return string.Join(Pad?" ":"", Nodes);
        }

        public override ICollection<String> Flatten(StateNode state)
        {
            var newstate = new StateNode();
            newstate.ApplyState(this);
            newstate.ApplyState(state);

            List<String> res = new List<String>(Nodes.Count + 1);
            bool donefirst = false;
            foreach (var node in Nodes)
            {
                if (Pad && donefirst) res.Add(new String(" "));
                res.AddRange(node.Flatten(newstate));
                donefirst = true;
            }

            return res;
        }

        public void Add(TextEx item)
        {
            Nodes.Add(item);
        }

        public void Clear()
        {
            Nodes.Clear();
        }

        public bool Contains(TextEx item)
        {
            return Nodes.Contains(item);
        }

        public void CopyTo(TextEx[] array, int arrayIndex)
        {
            Nodes.CopyTo(array, arrayIndex);
        }

        public bool Remove(TextEx item)
        {
            return Nodes.Remove(item);
        }

        public IEnumerator<TextEx> GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        [Newtonsoft.Json.JsonIgnore]
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

        [Newtonsoft.Json.JsonIgnore]
        public int Count => Nodes.Count;

        [Newtonsoft.Json.JsonIgnore]
        public bool IsReadOnly => Nodes.IsReadOnly;
    }
}
