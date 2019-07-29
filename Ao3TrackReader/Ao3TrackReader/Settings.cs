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
        [DatabaseVar(var = "UnitConvOptions.temp")]
        public UnitConvSetting temp;
        [DatabaseVar(var = "UnitConvOptions.dist")]
        public UnitConvSetting dist;
        [DatabaseVar(var = "UnitConvOptions.volume")]
        public UnitConvSetting volume;
        [DatabaseVar(var = "TagOptions.showCatTags")]
        public UnitConvSetting weight;

        int IUnitConvSettings.temp => (int)temp;
        int IUnitConvSettings.dist => (int)dist;
        int IUnitConvSettings.volume => (int)volume;
        int IUnitConvSettings.weight => (int)weight;
    }

    public sealed class TagSettings : ITagSettings
    {
        [DatabaseVar(var = "TagOptions.showCatTags")]
        private bool _showCatTags;
        [DatabaseVar(var = "TagOptions.showWIPTags")]
        private bool _showWIPTags;
        [DatabaseVar(var = "TagOptions.showRatingTags")]
        private bool _showRatingTags;

        public bool showCatTags { get => _showCatTags; } 
        public bool showWIPTags { get => _showWIPTags;  }
        public bool showRatingTags { get => _showRatingTags;}
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class DatabaseVarAttribute: Attribute
    {
        public bool nested;
        public string var;
    }
    public sealed class ListFilteringSettings : IListFilteringSettings
    {
        public bool hideFilteredWorks { get => _hideFilteredWorks; set => _hideFilteredWorks = value; }
        public bool onlyGeneralTeen { get => _onlyGeneralTeen; set => _onlyGeneralTeen = value; }

#if __ANDROID__
        public const bool def_onlyGeneralTeen = false;
#else
        public const bool def_onlyGeneralTeen = true;
#endif
        [DatabaseVar(var = "ListFiltering.HideWorks")]
        private bool _hideFilteredWorks;
        [DatabaseVar(var = "ListFiltering.OnlyGeneralTeen")]
        private bool _onlyGeneralTeen;
    }

    public sealed class Settings : ISettings
    {
        [DatabaseVar(nested = true)]
        private readonly UnitConvSettings _unitConv = new UnitConvSettings();
        [DatabaseVar(var = "override_site_theme")]
        private bool _override_site_theme;
        [DatabaseVar(var = "Theme")]
        private string _theme;

        public UnitConvSettings unitConv => _unitConv; IUnitConvSettings ISettings.unitConv { get { return unitConv; } }

        [DatabaseVar(nested = true)]
        public TagSettings tags = new TagSettings();
        ITagSettings ISettings.tags { get { return tags; } }

        [DatabaseVar(nested = true)]
        public ListFilteringSettings listFiltering  = new ListFilteringSettings();
        IListFilteringSettings ISettings.listFiltering { get { return listFiltering; } }

        public bool override_site_theme { get => _override_site_theme; set => _override_site_theme = value; }

        public string theme { get => _theme; set => _theme = value; }
    }
}
