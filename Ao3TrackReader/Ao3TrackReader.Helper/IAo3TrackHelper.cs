using System;
using System.Collections.Generic;


namespace Ao3TrackReader.Helper
{
    public interface IAo3TrackHelper
    {
        void Reset();
        void OnAlterFontSize();
        void OnJumpToLastLocation(bool pagejump);
    }
}
