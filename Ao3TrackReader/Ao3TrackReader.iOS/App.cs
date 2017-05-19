using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Foundation;
using UIKit;

using Xamarin.Forms;

namespace Ao3TrackReader
{
    public partial class App
    {
        public static InteractionMode InteractionMode
        {
            get
            {
                // Good enough for ios for now
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

        public static string OSArchitechture => null;

        public static string OSVersion => null;

        public static string OSName => null;

        public static string HardwareName => null;

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