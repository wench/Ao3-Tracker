using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Helper
{
    public interface IUnitConvOptions
    {
        bool? tempToC { get; }
        bool? distToM { get; }
        bool? volumeToM { get; }
        bool? weightToM { get; }
    }

}
