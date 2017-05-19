using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    public partial class App
    {
        public static string OSArchitechture => null;

        public static string HardwareName
        {
            get
            {
                var eas = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
                var sku = eas.SystemSku;
                if (!string.IsNullOrWhiteSpace(sku)) return sku;
                return eas.SystemManufacturer + " " + eas.SystemProductName;
            }
        }

        public static T RunSynchronously<T>(Windows.Foundation.IAsyncOperation<T> iasync)
        {
            var task = iasync.AsTask();
            task.Wait();
            return task.Result;
        }

        public static void RunSynchronously(Windows.Foundation.IAsyncAction iasync)
        {
            var task = iasync.AsTask();
            task.Wait();
        }

        public static void TextFileSave(string filename, string text)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = RunSynchronously(localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting));
            RunSynchronously(FileIO.WriteTextAsync(sampleFile, text));
        }
        public static async Task<string> TextFileLoadAsync(string filename)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = (await storageFolder.TryGetItemAsync(filename)) as StorageFile;
            if (sampleFile == null) return null;
            return await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
        }
        public static void TextFileDelete(string filename)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            var sampleFile = RunSynchronously(storageFolder.TryGetItemAsync(filename)) as StorageFile;
            if (sampleFile != null) RunSynchronously(sampleFile.DeleteAsync());
        }
    }
}
