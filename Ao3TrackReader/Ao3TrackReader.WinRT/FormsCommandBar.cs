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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

#if WINDOWS_UWP
using BaseCommandBar = Xamarin.Forms.Platform.UWP.FormsCommandBar;
#else
using BaseCommandBar = Windows.UI.Xaml.Controls.CommandBar;
#endif


namespace Ao3TrackReader.WinRT
{
    public class FormsCommandBar : BaseCommandBar
    {
        bool haveDynamicOverflow = false;
        bool needOverflowmenu = false;
        public FormsCommandBar() : base()
        {
#if WINDOWS_UWP
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.CommandBar", "IsDynamicOverflowEnabled"))
                IsDynamicOverflowEnabled = haveDynamicOverflow = true;
#else
            needOverflowmenu = true;
            IsOpen = true;
            IsSticky = true;
#endif
            PrimaryCommands.VectorChanged += VectorChanged;
            SecondaryCommands.VectorChanged += VectorChanged;
        }

        bool reflowing = false;
        bool needseparator = false;
        MenuFlyout secondaryFlyout = null;
        List<AppBarButton> primary = new List<AppBarButton>();
        List<AppBarButton> secondary = new List<AppBarButton>();

        private void VectorChanged(Windows.Foundation.Collections.IObservableVector<ICommandBarElement> sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.Reset)
            {
                secondaryFlyout = null;
                needseparator = false;
                if (!reflowing)
                {
                    primary.Clear();
                    secondary.Clear();
                }
            }
            else if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
            {
                var itembase = sender[(int)args.Index];
                itembase.IsCompact = true;

                if (sender == SecondaryCommands && needOverflowmenu)
                {
                    if (secondaryFlyout == null)
                    {
                        PrimaryCommands.Add(new AppBarButton
                        {
                            Flyout = secondaryFlyout = new MenuFlyout(),
                            Icon = new BitmapIcon
                            {
                                UriSource = new Uri("ms-appx:///" + Ao3TrackReader.Resources.Icons.More)
                            }
                        });
                        secondaryFlyout.Opening += SecondaryFlyout_Opening;
                        secondaryFlyout.Closed += SecondaryFlyout_Closed;
                    }

                    if (needseparator)
                    {
                        needseparator = false;
                        secondaryFlyout.Items.Add(new MenuFlyoutSeparator());
                    }

                    if (itembase is AppBarSeparator sep)
                    {
                        secondaryFlyout.Items.Add(new MenuFlyoutSeparator());
                    }
                    else if (itembase is AppBarButton button)
                    {
                        if (!reflowing && !primary.Contains(button))
                            secondary.Add(button);

                        var menuitem = new MenuFlyoutItem
                        {
                            Text = button.Label,
                            Command = button.Command,
                            DataContext = button.DataContext
                        };

                        if (menuitem.DataContext is Ao3TrackReader.Controls.ToolbarItem)
                        {
                            // Create the binding description.
                            Binding b = new Binding()
                            {
                                Mode = BindingMode.OneWay,
                                Path = new PropertyPath("Foreground"),
                                Converter = new ColorConverter()
                            };
                            menuitem.SetBinding(MenuFlyoutItem.ForegroundProperty, b);
                        }

                        secondaryFlyout.Items.Add(menuitem);
                    }

                    if (itembase is FrameworkElement elem) elem.Visibility = Visibility.Collapsed;

                    return;
                }

                if (itembase is FrameworkElement e) e.Visibility = Visibility.Visible;

                var item = itembase as AppBarButton;
                if (item == null) return;

                var xitem = item.DataContext as Xamarin.Forms.ToolbarItem;
                if (xitem == null) return;

                if (sender == PrimaryCommands)
                {
                    if (!reflowing)
                    {
                        primary.Add(item);

                        if (!haveDynamicOverflow && ActualWidth > 0)
                        {
                            uint limit = (uint)Math.Floor(ActualWidth / 68) - 1;
                            if (args.Index >= limit)
                            {
                                item.Visibility = Visibility.Collapsed;
                                needseparator = false;
                                SecondaryCommands.Add(new AppBarButton
                                {
                                    Label = item.Label,
                                    Icon = item.Icon,
                                    Command = item.Command,
                                    DataContext = item.DataContext
                                });
                                needseparator = true;

                                return;
                            }
                        }
                    }
                }
                else if (sender == SecondaryCommands)
                {
                    if (needseparator)
                    {
                        needseparator = false;
                        SecondaryCommands.Insert((int)args.Index,new AppBarSeparator());
                    }
                    if (!reflowing) secondary.Add(item);
                }

                item.ClearValue(AppBarButton.IconProperty);
                if (!string.IsNullOrWhiteSpace(xitem.Icon?.File))
                {
                    var uri = new Uri("ms-appx:///" + xitem.Icon.File);
                    item.Icon = new BitmapIcon() { UriSource = uri };
                }

                if (item.DataContext is Ao3TrackReader.Controls.ToolbarItem)
                {
                    // Create the binding description.
                    Binding b = new Binding()
                    {
                        Mode = BindingMode.OneWay,
                        Path = new PropertyPath("Foreground"),
                        Converter = new ColorConverter()
                    };
                    item.SetBinding(AppBarButton.ForegroundProperty, b);
                }
            }
        }

        private void SecondaryFlyout_Closed(object sender, object e)
        {
            foreach (var item in PrimaryCommands)
            {
                if (item is AppBarButton abb)
                {
                    VisualStateManager.GoToState(abb, "HideLabel", true);
                }
            }
        }

        private void SecondaryFlyout_Opening(object sender, object e)
        {
            foreach (var item in PrimaryCommands)
            {
                if (item is AppBarButton abb)
                {
                    VisualStateManager.GoToState(abb, "ShowLabel", true);
                }
            }
        }

#if WINDOWS_UWP
        protected override Size MeasureOverride(Size availableSize)
        {
            Size res = availableSize;
            try
            {
                res = base.MeasureOverride(availableSize);
            }
            catch(System.Runtime.InteropServices.COMException)
            {
                res.Height = 48;
            }
            return res;
        }
#else
        protected override void OnClosed(object e)
        {
            IsOpen = true;
        }
#endif      

        protected override Size ArrangeOverride(Size finalSize)
        {
            var ret = base.ArrangeOverride(finalSize);

#if !WINDOWS_UWP
            if (Window.Current.Content is Frame f)
            {
                f.Margin = new Thickness(0, 0, 0, ret.Height);
            }
#endif

            if (!haveDynamicOverflow && primary.Count != 0)
            {
                uint limit = (uint)Math.Floor(ret.Width / 68) - 1;
                int currentCount = PrimaryCommands.CountVisible() - (needOverflowmenu ? 1 : 0);

                if (currentCount != limit && !(currentCount == primary.Count && limit > currentCount))
                {
                    reflowing = true;

                    secondaryFlyout = null;
                    needseparator = false;
                    PrimaryCommands.Clear();
                    SecondaryCommands.Clear();

                    int i = 0;
                    foreach (var item in primary)
                    {
                        if (i < limit)
                            PrimaryCommands.Add(item);
                        else
                            SecondaryCommands.Add(item);

                        i++;
                    }

                    needseparator = i > limit;

                    foreach (var item in secondary)
                    {
                        SecondaryCommands.Add(item);
                    }

                    reflowing = false;
                }
            }

            return ret;
        }

    }

    static class CommandBarExtensions
    {
        public static int CountVisible(this Windows.Foundation.Collections.IObservableVector<ICommandBarElement> vector)
        {
            int count = 0;
            foreach (var e in vector)
            {
                if (e is Control ctrl && ctrl.Visibility == Visibility.Visible)
                    count++;
            }
            return count;
        }
    }

}
