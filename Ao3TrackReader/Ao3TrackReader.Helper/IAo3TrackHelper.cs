using System;
using System.Collections.Generic;


namespace Ao3TrackReader.Helper
{
    public interface IAo3TrackHelper
    {
        string MemberDef { get; }
        void Reset();
        void OnAlterFontSize(int fontSize);
        void OnJumpToLastLocation(bool pagejump);
    }
}
