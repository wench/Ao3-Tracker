using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Helper
{
    public interface ISettings
    {
        bool? tempToC { get; }
        bool? distToM { get; }
        bool? volumeToM { get; }
        bool? weightToM { get; }
        bool showCatTags { get; }
        bool showWIPTags { get; }
        bool showRatingTags { get; }
        bool hideFilteredWorks { get; }
    }

    public sealed class Settings : ISettings
    {
        public bool? tempToC { get; set; }
        public bool? distToM { get; set; }
        public bool? volumeToM { get; set; }
        public bool? weightToM { get; set; }
        public bool showCatTags { get; set; }
        public bool showWIPTags { get; set; }
        public bool showRatingTags { get; set; }
        public bool hideFilteredWorks { get; set; }
    }
}
