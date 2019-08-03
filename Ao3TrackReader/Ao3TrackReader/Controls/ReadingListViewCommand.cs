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
using Ao3TrackReader.Models;
using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public abstract class ReadingListViewCommand : VirtualAutoUpdateCommand<Ao3PageViewModel>
    {
        public static readonly BindableProperty ReadingListProperty =
          BindableProperty.Create("ReadingList", typeof(ReadingListView), typeof(ReadingListViewCommand), defaultValue: null);

        public ReadingListView ReadingList
        {
            get { return (ReadingListView)GetValue(ReadingListProperty); }
            set { SetValue(ReadingListProperty, value); }
        }

        protected ReadingListViewCommand(params string[] triggerProperties) : base(triggerProperties)
        {
        }
    }

    public class MenuOpenLastCommand : ReadingListViewCommand
    {
        public MenuOpenLastCommand() : base("BaseData")
        {
        }

        protected override bool CanExecute()
        {
            return Target != null && (Target.BaseData?.Type == Models.Ao3PageType.Work || Target.BaseData?.Type == Models.Ao3PageType.Series);
        }

        protected override void Execute()
        {
            Ao3PageViewModel item = Target as Ao3PageViewModel;
            ReadingList?.Goto(item, true, false);
        }
    }

    public class MenuOpenFullWorkCommand : ReadingListViewCommand
    {
        public MenuOpenFullWorkCommand() : base("BaseData")
        {
        }

        protected override bool CanExecute()
        {
            return Target != null && (Target.BaseData?.Type == Models.Ao3PageType.Work) && Target.BaseData?.Details?.Chapters?.Available > 1;
        }

        protected override void Execute()
        {
            ReadingList?.Goto(Target, false, true);
        }
    }

    public class MenuOpenFullWorkLastCommand : ReadingListViewCommand
    {
        public MenuOpenFullWorkLastCommand() : base("BaseData")
        {
        }

        protected override bool CanExecute()
        {
            return Target != null && (Target.BaseData?.Type == Models.Ao3PageType.Work) && Target.BaseData.Details?.Chapters?.Available > 1;
        }

        protected override void Execute()
        {
            ReadingList?.Goto(Target, true, true);
        }
    }
}
