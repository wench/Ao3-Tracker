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

namespace Ao3TrackReader
{
    public class Injection
    {
        public Injection()
        {

        }
        public Injection(string filename)
        {
            Filename = filename;
            Type = System.IO.Path.GetExtension(filename);
        }
        public Injection(object function, string filename)
        {
            Filename = filename;
            Type = System.IO.Path.GetExtension(filename);
            this.function = function;
        }
        public Injection(FormattableString function, string filename)
        {
            Filename = filename;
            Type = System.IO.Path.GetExtension(filename);
            this.function = function;
        }
        public string Filename { get; set; }
        public string Type { get; set; }
        public object function;
        public string Function => function?.ToString(); 
        public string Content { get; set; } = null;

        public static implicit operator Injection(string filename)
        {
            return new Injection(filename);
        }
    }
}
