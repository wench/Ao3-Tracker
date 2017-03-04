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
            return Target != null && (Target.BaseData.Type == Models.Ao3PageType.Work || Target.BaseData.Type == Models.Ao3PageType.Series);
        }

        protected override void Execute()
        {
            Ao3PageViewModel item = Target as Ao3PageViewModel;
            ReadingList?.Goto(item,true,false);
        }
    }

    public class MenuOpenFullWorkCommand : ReadingListViewCommand
    {
        public MenuOpenFullWorkCommand() : base("BaseData")
        {
        }

        protected override bool CanExecute()
        {
            return Target != null && (Target.BaseData.Type == Models.Ao3PageType.Work) && Target.BaseData.Details?.Chapters?.Available > 1;
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
            return Target != null && (Target.BaseData.Type == Models.Ao3PageType.Work) && Target.BaseData.Details?.Chapters?.Available > 1;
        }

        protected override void Execute()
        {
            ReadingList?.Goto(Target, true, true);
        }
    }


}
