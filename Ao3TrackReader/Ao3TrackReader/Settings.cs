using System;
using System.Collections.Generic;
using System.Text;
using Ao3TrackReader.Helper;

namespace Ao3TrackReader
{
    public enum UnitConvSetting
    {
        None = 0,
        MetricToUS = 1,
        USToMetric = 2
    }

    public sealed class UnitConvSettings : IUnitConvSettings
    {
        public UnitConvSetting temp { get; set; }
        public UnitConvSetting dist { get; set; }
        public UnitConvSetting volume { get; set; }
        public UnitConvSetting weight { get; set; }

        int IUnitConvSettings.temp => (int)temp;
        int IUnitConvSettings.dist => (int)dist;
        int IUnitConvSettings.volume => (int)volume;
        int IUnitConvSettings.weight => (int)weight;
    }

    public sealed class TagSettings : ITagSettings
    {
        public bool showCatTags { get; set; }
        public bool showWIPTags { get; set; }
        public bool showRatingTags { get; set; }
    }

    public sealed class ListFilteringSettings : IListFilteringSettings
    {
        public bool hideFilteredWorks { get; set; }
        public bool onlyGeneralTeen { get; set; }

#if __ANDROID__
        public const bool def_onlyGeneralTeen = false;
#else
        public const bool def_onlyGeneralTeen = true;
#endif
    }

    public sealed class Settings : ISettings
    {
        public UnitConvSettings unitConv { get; } = new UnitConvSettings();
        IUnitConvSettings ISettings.unitConv { get { return unitConv; } }

        public TagSettings tags { get; } = new TagSettings();
        ITagSettings ISettings.tags { get { return tags; } }

        public ListFilteringSettings listFiltering { get; } = new ListFilteringSettings();
        IListFilteringSettings ISettings.listFiltering { get { return listFiltering; } }
    }
}
