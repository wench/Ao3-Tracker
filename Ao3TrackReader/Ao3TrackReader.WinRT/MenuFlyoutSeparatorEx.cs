using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace Ao3TrackReader.WinRT
{
    public class MenuFlyoutSeparatorEx : MenuFlyoutSeparator
    {
        public ICommand Command { get; set; }
    }
}
