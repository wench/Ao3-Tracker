using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Ao3TrackReader
{
    public static class Win81PhoneExtensions
    {
        public async static Task<IStorageItem> TryGetItemAsync(this StorageFolder storage, string name)
        {
            try
            {
                return await storage.GetItemAsync(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
