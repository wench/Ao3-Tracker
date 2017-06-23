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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

#if WINDOWS_UWP
using BaseCommandBar = Xamarin.Forms.Platform.UWP.FormsCommandBar;
#else
using BaseCommandBar = Windows.UI.Xaml.Controls.CommandBar;
#endif


namespace Ao3TrackReader.WinRT
{

#if WINDOWS_APP
    internal class AppBarEllipsis : AppBarButton
    {
        public AppBarEllipsis() : base()
        {
            var i = new SymbolIcon(Symbol.More);
            Icon = new BitmapIcon
            {
                UriSource = new Uri("ms-appx:///" + Ao3TrackReader.Resources.Icons.More)
            };
        }

        protected override void OnApplyTemplate()
        {
            var RootGrid = GetTemplateChild("RootGrid") as Grid;
            RootGrid.Width = 48;
        }
    }
#endif

    public class FormsCommandBar : BaseCommandBar
    {
        bool haveDynamicOverflow = false;

        public FormsCommandBar() : base()
        {
            if (Application.Current.Resources.TryGetValue("ao3t:AppBarButtonTemplate", out var template))
                AppBarButtonTemplate = template as ControlTemplate;
#if WINDOWS_UWP && (WINDOWS_14393 || WINDOWS_15063)
            if (Ao3TrackReader.UWP.App.UniversalApi >= 3)
                IsDynamicOverflowEnabled = haveDynamicOverflow = true;
#elif WINDOWS_APP
            IsOpen = true;
            IsSticky = true;
            Background = new SolidColorBrush(Ao3TrackReader.Resources.Colors.Alt.Solid.MediumHigh.ToWindows());
#endif
            PrimaryCommands.VectorChanged += VectorChanged;
            SecondaryCommands.VectorChanged += VectorChanged;
        }

        bool reflowing = false;
        bool needseparator = false;
        List<AppBarButton> primary = new List<AppBarButton>();
        List<AppBarButton> secondary = new List<AppBarButton>();
        private ControlTemplate AppBarButtonTemplate;

#if WINDOWS_APP
        MenuFlyout secondaryFlyout = null;
#endif

        private void VectorChanged(Windows.Foundation.Collections.IObservableVector<ICommandBarElement> sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.Reset)
            {
#if WINDOWS_APP
                secondaryFlyout = null;
#endif
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

#if WINDOWS_APP
                if (sender == SecondaryCommands)
                {
                    if (secondaryFlyout == null)
                    {
                        var ellipsis = new AppBarEllipsis
                        {
                            Flyout = secondaryFlyout = new MenuFlyout()
                        };

                        PrimaryCommands.Add(ellipsis);
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

                        var menuitem = new MenuFlyoutItemEx
                        {
                            Text = button.Label,
                            Command = button.Command,
                            DataContext = button.DataContext,
                        };
                        if (button.Icon is BitmapIcon bmp) menuitem.Icon = new BitmapIcon { UriSource = bmp.UriSource };
                        else if (button.Icon is SymbolIcon sym) menuitem.Icon = new SymbolIcon(sym.Symbol);

                        if (menuitem.DataContext is Ao3TrackReader.Controls.ToolbarItem)
                        {
                            // Create the binding description.
                            Binding b = new Binding()
                            {
                                Mode = BindingMode.OneWay,
                                Path = new PropertyPath("Foreground"),
                                Converter = new ColorConverter()
                            };
                            menuitem.SetBinding(MenuFlyoutItemEx.ForegroundProperty, b);
                        }

                        secondaryFlyout.Items.Add(menuitem);
                    }

                    if (itembase is FrameworkElement elem) elem.Visibility = Visibility.Collapsed;

                    return;
                }
#endif

                if (itembase is FrameworkElement e) e.Visibility = Visibility.Visible;

                var item = itembase as AppBarButton;
                if (item == null) return;
                if (AppBarButtonTemplate != null)
                    item.Template = AppBarButtonTemplate;

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
                                var newitem = new AppBarButton
                                {
                                    Label = item.Label,
                                    Command = item.Command,
                                    DataContext = item.DataContext
                                };
                                if (item.Icon is BitmapIcon bmp) newitem.Icon = new BitmapIcon { UriSource = bmp.UriSource };
                                else if (item.Icon is SymbolIcon sym) newitem.Icon = new SymbolIcon(sym.Symbol);
                                SecondaryCommands.Add(newitem);

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
#if !WINDOWS_PHONE_APP
                        SecondaryCommands.Insert((int)args.Index, new AppBarSeparator());
#else
                        SecondaryCommands.Insert((int)args.Index, new AppBarButton {
                            Label = "\x23AF\x23AF\x23AF\x23AF",
                            IsEnabled = false,
                    });
#endif
                    }
                    if (!reflowing) secondary.Add(item);

                    var ti = GetTemplateChild("SecondaryItemsControl");
                }

                item.ClearValue(AppBarButton.IconProperty);
                if (!string.IsNullOrWhiteSpace(xitem.Icon?.File))
                {
#if !WINDOWS_PHONE_APP
                    var uri = new Uri("ms-appx:///" + xitem.Icon.File);
#else
                    var uri = new Uri("ms-appx:///Appbar/" + xitem.Icon.File);
#endif
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
#elif WINDOWS_APP
        VisualTransition LabelsHiddenToShown;
        VisualTransition LabelsShownToHidden;
        VisualState LabelsHidden;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LabelsHiddenToShown = GetTemplateChild("LabelsHiddenToShown") as VisualTransition;
            LabelsShownToHidden = GetTemplateChild("LabelsShownToHidden") as VisualTransition;

            LabelsHidden = GetTemplateChild("LabelsHidden") as VisualState;
            LabelsHidden.Storyboard.Begin();
        }

        private void SecondaryFlyout_Closed(object sender, object e)
        {
            LabelsShownToHidden.Storyboard.Begin();
        }

        private void SecondaryFlyout_Opening(object sender, object e)
        {
            LabelsHiddenToShown.Storyboard.Begin();
        }
        protected override void OnClosed(object e)
        {
            IsOpen = true;
        }
#endif

        protected override Size ArrangeOverride(Size finalSize)
        {
            var ret = base.ArrangeOverride(finalSize);

            if (!haveDynamicOverflow && primary.Count != 0)
            {
                uint limit = (uint)Math.Floor(ret.Width / 68) - 1;
                int currentCount = PrimaryCommands.CountVisible();

                if (currentCount != limit && !(currentCount == primary.Count && limit > currentCount))
                {
                    reflowing = true;

#if WINDOWS_APP
                    secondaryFlyout = null;
#endif
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
                if (e is AppBarButton ctrl && ctrl.Visibility == Visibility.Visible && ctrl.Flyout == null)
                    count++;
            }
            return count;
        }
    }

}
