using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    static class Extensions
    {
        static readonly IDictionary<string, string> m_replaceDict = new Dictionary<string, string> {
            {"\a", @"\a"},
            {"\b", @"\b"},
            {"\f", @"\f"},
            {"\n", @"\n"},
            {"\r", @"\r"},
            {"\t", @"\t"},
            {"\v", @"\v"},
            {"\\", @"\\"},
            {"\0", @"\0"},
            {"\"", "\\\""}
        };


        public static string ToLiteral(this string i_string)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(i_string);
        }

        public static string ToLiteral(this char c)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(c.ToString());
        }

        public static string HtmlDecode(this string str)
        {
            return WebUtility.HtmlDecode(str);
        }


        public static string[] GetClasses(this HtmlNode node)
        {
            var attrClass = node.Attributes["class"];
            if (attrClass != null && !string.IsNullOrEmpty(attrClass.Value))
                return attrClass.Value.Split(' ');
            return new string[] { };
        }

        public static bool HasClass(this HtmlNode node, string classname)
        {
            return Array.IndexOf(node.GetClasses(), classname) != -1;
        }

        public static HtmlNode ElementByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;

                if (e.HasClass(classname)) return e;
            }
            return null;
        }

        public static HtmlNode ElementByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                if (e.HasClass(classname)) return e;
            }
            return null;
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string classname)
        {
            foreach (var e in node.ChildNodes)
            {
                if (e.NodeType != HtmlNodeType.Element) continue;
                if (e.HasClass(classname)) yield return e;
            }
        }

        public static IEnumerable<HtmlNode> ElementsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Elements(tagname))
            {
                if (e.HasClass(classname)) yield return e;
            }
        }

        public static IEnumerable<HtmlNode> DescendantsByClass(this HtmlNode node, string tagname, string classname)
        {
            foreach (var e in node.Descendants(tagname))
            {
                if (e.HasClass(classname)) yield return e;
            }
        }

        /// <summary>
        /// Blend a color with another color
        /// </summary>
        /// <param name="color">Foreground color</param>
        /// <param name="behind">Background color</param>
        /// <returns></returns>
        public static Xamarin.Forms.Color Blend(this Xamarin.Forms.Color color, Xamarin.Forms.Color behind)
        {
            if (behind.A != 1) behind = behind.Blend(Xamarin.Forms.Color.Black);

            //return new Xamarin.Forms.Color(
            //        Math.Pow(Math.Pow(color.R, 2.2) * color.A + Math.Pow(behind.R, 2.2) * (1 - color.A), 1 / 2.2),
            //        Math.Pow(Math.Pow(color.G, 2.2) * color.A + Math.Pow(behind.G, 2.2) * (1 - color.A), 1 / 2.2),
            //        Math.Pow(Math.Pow(color.B, 2.2) * color.A + Math.Pow(behind.B, 2.2) * (1 - color.A), 1 / 2.2)
            //    );
            return new Xamarin.Forms.Color(
                    color.R * color.A + behind.R * (1 - color.A),
                    color.G * color.A + behind.G * (1 - color.A),
                    color.B * color.A + behind.B * (1 - color.A)
                    );
        }


        public static async Task<HtmlDocument> ReadAsHtmlDocumentAsync(this HttpContent httpContent)
        {
            HtmlDocument doc = new HtmlDocument();
            var content = await httpContent.ReadAsStringAsync();
            if (!String.IsNullOrWhiteSpace(content)) doc.Load(new System.IO.StringReader(content));
            return doc;
        }

#if WINDOWS_UWP
        public static Windows.UI.Color ToWindows(this Xamarin.Forms.Color color)
        {
            return Windows.UI.Color.FromArgb((byte)(color.A * 255), (byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
        }

        public static Xamarin.Forms.Color ToXamarin(this Windows.UI.Color color)
        {
            return Xamarin.Forms.Color.FromRgba((int)color.R, (int)color.G, (int)color.B, (int)color.A);
        }
#endif

    }
}
