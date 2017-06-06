/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Helper
{
    public interface IUnitConvSettings
    {
        int temp { get; }
        int dist { get; }
        int volume { get; }
        int weight { get; }
    }

    public interface ITagSettings
    {
        bool showCatTags { get; }
        bool showWIPTags { get; }
        bool showRatingTags { get; }
    }

    public interface IListFilteringSettings
    {
        bool hideFilteredWorks { get; }
        bool onlyGeneralTeen { get; }
    }

    public interface ISettings
    {
        IUnitConvSettings unitConv { get; }
        ITagSettings tags { get; }
        IListFilteringSettings listFiltering { get; }
    }


}
