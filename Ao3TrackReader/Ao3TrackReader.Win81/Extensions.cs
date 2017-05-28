using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ao3TrackReader
{
    public static partial class Extensions
    {
        public static bool TrySetCanceled<T>(this TaskCompletionSource<T> source, CancellationToken token)
        {
            return source.TrySetCanceled();
        }
    }
}
