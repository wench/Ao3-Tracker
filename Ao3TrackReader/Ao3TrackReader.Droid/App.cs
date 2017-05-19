using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms;

using Environment = System.Environment;

namespace Ao3TrackReader
{
    public partial class App
    {
        public static InteractionMode InteractionMode
        {
            get
            {
                // Good enough for android for now
                switch (Device.Idiom)
                {
                    case TargetIdiom.Phone:
                        return InteractionMode.Phone;

                    case TargetIdiom.Tablet:
                        return InteractionMode.Tablet;

                    case TargetIdiom.Desktop:
                        return InteractionMode.PC;
                }
                return InteractionMode.Unknown;
            }
        }

        public static string OSArchitechture
        {
            get
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    return Build.SupportedAbis[0];
                else
                    return Build.CpuAbi;
            }
        }

        public static string OSVersion => Build.VERSION.Release;

        public static string OSName => Build.VERSION.BaseOs;

        public static string HardwareName => Build.Brand + " " + Build.Device + " " + Build.Model;

        public static string HardwareType => null;

        public static void TextFileSave(string filename, string text)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);
            File.WriteAllText(filePath, text);
        }
        public static Task<string> TextFileLoadAsync(string filename)
        {
            return Task.Run(() => {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);
                return File.ReadAllText(filePath);
            });
        }
        public static void TextFileDelete(string filename)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);
            File.Delete(filePath);
        }
    }
}