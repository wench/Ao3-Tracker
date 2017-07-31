using System;
using System.Xml;
using System.Reflection;

namespace Ao3TrackReader.Version.Build
{
    // Build tool to automatically update package build numbers to match code
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Ao3TrackReader.Version.Build (xmlfile) (xpath=type) [(xpath=type)...] ");
                return;
            }
            try
            {
                TypeInfo ti_version = typeof(Ao3TrackReader.Version.Version).GetTypeInfo();
                Console.WriteLine(args[0]);
                var doc = new XmlDocument();
                doc.Load(args[0]);
                XmlNamespaceManager nsmanager = new XmlNamespaceManager(doc.NameTable);
                foreach (var o in doc.DocumentElement.Attributes)
                {
                    var a = (XmlAttribute)o;
                    if (a.Name.StartsWith("xmlns:"))
                    {
                        nsmanager.AddNamespace(a.Name.Substring(6),a.Value);
                    }
                    if (a.Name == "xmlns")
                    {
                        nsmanager.AddNamespace("_", a.Value);
                    }
                }

                bool changed = false;
                for (int i = 1; i < args.Length; i++)
                {
                    int o = args[i].LastIndexOf('=');
                    if (o == -1) throw new ArgumentException("Incorrect syntax", "args[" + i + "]");

                    var n = doc.SelectSingleNode(args[i].Substring(0,o), nsmanager);
                    if (n == null) throw new ArgumentException("Can't find element", "args["+i+"]");

                    var sourcefield = ti_version.GetField(args[i].Substring(o + 1));
                    string newValue = sourcefield?.GetValue(null)?.ToString();
                    if (newValue == null) throw new ArgumentException("Unknown field", "args[" + i + "]");

                    if (n.InnerText != newValue)
                    {
                        Console.WriteLine("{0}: {1} => {2}", args[i].Substring(0, o).Replace("_:", ""), n.InnerText, newValue);
                        n.InnerText = newValue;
                        changed = true;
                    }
                }

                if (changed) doc.Save(args[0]);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
