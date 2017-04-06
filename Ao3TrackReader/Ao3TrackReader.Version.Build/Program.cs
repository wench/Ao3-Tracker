using System;
using System.Xml;

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
                    string newValue;
                    switch (args[i].Substring(o+1))
                    {
                        case "Integer":
                            newValue = Ao3TrackReader.Version.Version.Integer.ToString();
                            break;

                        case "String":
                            newValue = Ao3TrackReader.Version.Version.String;
                            break;

                        case "LongString":
                            newValue = Ao3TrackReader.Version.Version.LongString;
                            break;

                        case "AltString":
                            newValue = Ao3TrackReader.Version.Version.AltString;
                            break;

                        default:
                            throw new ArgumentException("Unknown type", "args[" + i + "]");
                    }                 

                    Console.WriteLine("{0}: {1} => {2}", args[i].Substring(0, o).Replace("_:",""), n.InnerText, newValue);
                    if (n.InnerText != newValue)
                    {
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
