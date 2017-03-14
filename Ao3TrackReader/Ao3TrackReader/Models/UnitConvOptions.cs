using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{
    public sealed class UnitConvOptions : Helper.IUnitConvOptions
    {
        public bool? tempToC { get; set; }
        public bool? distToM { get; set; }
        public bool? volumeToM { get; set; }
        public bool? weightToM { get; set; }
    }
}
