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
using System.Text;
using HtmlAgilityPack;
using Ao3TrackReader.Models;

namespace Ao3TrackReader.Text
{
    public static class HtmlConverter
    {
        // Ao3 supports these tags in works:
        // a, abbr, acronym, address, b, big, blockquote, br, caption, center, cite, code, col, colgroup, dd, del, dfn, div, dl, dt, em, 
        // h1, h2, h3, h4, h5, h6, hr, i, img, ins, kbd, li, ol, p, pre, q, s, samp, small, span, strike, strong, sub, sup, table, tbody, 
        // td, tfoot, th, thead, tr, tt, u, ul, var.


        public static Text ConvertNode(HtmlNode html)
        {
            Span span = null;

            switch (html.Name)
            {
                case "a":
                    span = new Span();
                    break;

                case "abbr":
                    span = new Span();
                    break;

                case "acronym":
                    span = new Span();
                    break;

                case "address":
                    span = new Span();
                    break;

                case "b":
                    span = new Span();
                    span.Bold = true;
                    break;

                case "big":
                    // Deprecated
                    span = new Span();
                    break;

                case "blockquote":
                    span = new Block();
                    break;

                case "br":
                    return new String { Text = "\n" };


                case "center":
                    // Deprecated
                    span = new Block();
                    break;

                case "cite":
                    span = new Span();
                    span.Italic = true;
                    break;

                case "code":
                    span = new Span();
                    // span.FontFace = "fixed";
                    break;

                case "dd":
                    span = new Span();
                    break;

                case "del":
                    span = new Span();
                    span.Strike = true;
                    break;

                case "dfn":
                    span = new Span();
                    break;

                case "div":
                    span = new Block();
                    break;

                case "dl":
                    span = new Span();
                    break;

                case "dt":
                    span = new Span();
                    break;

                case "em":
                    span = new Span();
                    span.Italic = true;
                    break;

                case "h1":
                    span = new Block();
                    break;

                case "h2":
                    span = new Block();
                    break;

                case "h3":
                    span = new Block();
                    break;

                case "h4":
                    span = new Block();
                    break;

                case "h5":
                    span = new Block();
                    break;

                case "h6":
                    span = new Block();
                    break;

                case "hr":
                    return new String { Text = "\n----\n" };

                case "i":
                    span = new Span();
                    span.Italic = true;
                    break;

                case "img":
                    // Yeah... no
                    break;

                case "ins":
                    span = new Span();
                    break;

                case "kbd":
                    span = new Span();
                    // span.FontFace = "fixed";
                    break;

                case "li":
                    span = new Block();
                    break;

                case "ol":
                    {
                        span = new Block();
                        int i = 1;
                        foreach (var li in html.Elements("li"))
                        {
                            span.Nodes.Add(new String { Text = i.ToString() + ": " });    // Handle list type...
                            span.Nodes.Add(ConvertNode(li));
                            i++;
                        }
                    }
                    return span;

                case "p":
                    span = new Block();
                    break;

                case "pre":
                    span = new Block();
                    // span.FontFace = "fixed";
                    break;

                case "q":
                    span = new Span();
                    break;

                case "s":
                    span = new Span();
                    span.Strike = true;
                    break;

                case "samp":
                    span = new Span();
                    // span.FontFace = "fixed";
                    break;

                case "small":
                    span = new Span();
                    break;

                case "span":
                    span = new Span();
                    break;

                case "strike":
                    span = new Span();
                    span.Strike = true;
                    break;

                case "strong":
                    span = new Span();
                    span.Bold = true;
                    break;

                case "sub":
                    span = new Span();
                    span.Sub = true;
                    break;

                case "sup":
                    span = new Span();
                    span.Super = true;
                    break;

                case "tt":
                    span = new Span();
                    // span.FontFace = "fixed";
                    break;

                case "u":
                    span = new Span();
                    span.Underline = true;
                    break;

                case "ul":
                    {
                        span = new Block();
                        foreach (var li in html.Elements("li"))
                        {
                            span.Nodes.Add(new String { Text = "* " });
                            span.Nodes.Add(ConvertNode(li));
                        }
                    }
                    return span;

                case "var":
                    span = new Span();
                    span.Italic = true;
                    break;


                // Table elements

                case "caption":
                    span = new Block();
                    break;

                case "col":
                    break;

                case "colgroup":
                    break;

                case "table":
                    span = new Block();
                    break;

                case "tbody":
                    span = new Block();
                    break;

                case "td":
                    span = new Span();
                    break;

                case "tfoot":
                    break;

                case "th":
                    span = new Span();
                    break;

                case "thead":
                    span = new Block();
                    break;

                case "tr":
                    span = new Block();
                    break;


            }

            if (span == null)
                return null;

            // Convert children and text nodes
            foreach (var child in html.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case HtmlNodeType.Text:
                        var text = child.InnerHtml.HtmlDecode().Trim();
                        if (!string.IsNullOrWhiteSpace(text)) span.Nodes.Add(text);
                        break;

                    case HtmlNodeType.Element:
                        var cspan = ConvertNode(child);
                        if (cspan != null) span.Nodes.Add(cspan);
                        break;
                }

            }

            return span;
        }
    }
}
