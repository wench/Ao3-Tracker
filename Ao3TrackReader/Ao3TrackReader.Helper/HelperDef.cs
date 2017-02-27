using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ao3TrackReader.Helper
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Event | AttributeTargets.Method, AllowMultiple = false)]
    sealed class ConverterAttribute : Attribute
    {
        internal string converter;
        public ConverterAttribute(string converter)
        {
            this.converter = converter;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false)]
    sealed class IgnoreAttribute : Attribute
    {
    }

    sealed class MemberDef
    {
        public string @return { get; set; }
        public IDictionary<int, string> args { get; set; }
        public string getter { get; set; }
        public string setter { get; set; }
        public int? promise { get; set; }
        public string gettername { get; set; }
        public string settername { get; set; }
    }

    sealed class HelperDef : Dictionary<string, MemberDef>
    {
        public void FillFromType(Type type)
        {
            foreach (var mi in type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                if (mi.IsSpecialName) continue;
                // Android needs attributes checked
                //mi.GetCustomAttribute<>

                if (mi.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;

                string name = mi.Name;
                name = name.Substring(0, 1).ToLower() + name.Substring(1);

                var md = new MemberDef();

                md.args = new Dictionary<int, string>();

                var pa = mi.GetParameters();
                for (int i = 0; i < pa.Length; i++)
                {
                    var pi = pa[i];
                    var c = pi.GetCustomAttribute<ConverterAttribute>()?.converter;
                    if (!string.IsNullOrEmpty(c)) md.args[i] = c;
                }

                var conv = mi.GetCustomAttribute<ConverterAttribute>()?.converter;
                if (!string.IsNullOrEmpty(conv)) md.@return = conv;

#if WINDOWS_UWP
                if (mi.ReturnType.IsConstructedGenericType)
                {
                    var generic = mi.ReturnType.GetGenericTypeDefinition();
                    if (generic == typeof (Windows.Foundation.IAsyncOperation<>))
                    {
                        md.promise = pa.Length;
                    }
                }
#endif
                Add(name, md);
            }

            foreach (var ei in type.GetEvents(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                if (ei.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;

                string name = "on" + ei.Name.ToLower();

                var md = new MemberDef();

                var conv = ei.GetCustomAttribute<ConverterAttribute>()?.converter;
                if (!string.IsNullOrEmpty(conv)) md.setter = conv;
                else md.setter = "true";

                Add(name, md);

            }

            foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                if (pi.GetCustomAttribute<IgnoreAttribute>() != null)
                    continue;

                string name = pi.Name;
                name = name.Substring(0, 1).ToLower() + name.Substring(1);

                var md = new MemberDef();

                var setter = pi.GetSetMethod();
                if (setter != null)
                {
                    var conv = setter.GetCustomAttribute<ConverterAttribute>()?.converter;
                    if (!string.IsNullOrEmpty(conv)) md.setter = conv;
                    else md.setter = "true";
                }

                var getter = pi.GetGetMethod();
                if (getter != null)
                {
                    var conv = getter.GetCustomAttribute<ConverterAttribute>()?.converter;
                    if (!string.IsNullOrEmpty(conv)) md.getter = conv;
                    else md.getter = "true";

                }

                Add(name, md);
            }
        }
        public string Serialize()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, settings);
        }
    }
}
