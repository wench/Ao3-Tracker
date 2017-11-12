using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ao3TrackReader.Controls
{
    [Xamarin.Forms.ContentProperty("Children")]
    public class TabbedLayout : ContentView, IViewContainer<TabView>
    {
        private StackLayout Tabs;
        private StackLayout Buttons;
        private ScrollView Scroll;
        private ScrollView ButtonsScroll;
        private Button ButtonsLeft;
        private Button ButtonsRight;

        public new IList<TabView> Children => new ListConverter<TabView, View>(Tabs.Children);

        int currentTabIndex = 0;
        TabView currentTab = null;
        double tabWidth = 400;

        Style buttonStyle;
        public TabbedLayout()
        {
            buttonStyle = (Style)App.Current.Resources["TabbedLayoutButtonStyle"];

            var template = (DataTemplate) App.Current.Resources["TabbedLayoutTemplate"];
            var templateContent = (View) template.CreateContent();

            ButtonsScroll = templateContent.FindByName<ScrollView>("ButtonsScroll");
            Buttons = templateContent.FindByName<StackLayout>("Buttons");
            Scroll = templateContent.FindByName<ScrollView>("Scroll");
            Tabs = templateContent.FindByName<StackLayout>("Tabs");
            ButtonsLeft = templateContent.FindByName<Button>("ButtonsLeft");
            ButtonsRight = templateContent.FindByName<Button>("ButtonsRight");

            Tabs.ChildAdded += Tabs_ChildAdded;
            Tabs.ChildRemoved += Tabs_ChildRemoved;
            Tabs.ChildrenReordered += Tabs_ChildrenReordered;
            Scroll.ScrollEnd += Scroll_ScrollEnd;
            Scroll.Scrolled += Scroll_Scrolled;
            Scroll.SizeChanged += Scroll_SizeChanged;

            ButtonsScroll.Scrolled += ButtonsScroll_Scrolled;
            ButtonsScroll.SizeChanged += ButtonsScroll_SizeChanged;
            ButtonsLeft.Pressed += ButtonsLeft_Pressed;
            ButtonsRight.Pressed += ButtonsRight_Pressed;

            Content = templateContent;
        }

        private void ButtonsScroll_Scrolled(object sender, ScrolledEventArgs e)
        {
            ButtonsLeft.IsEnabled = e.ScrollX > 0;
            ButtonsRight.IsEnabled = e.ScrollX < (ButtonsScroll.ContentSize.Width - ButtonsScroll.Width);
        }

        private void ButtonsScroll_SizeChanged(object sender, EventArgs e)
        {
            bool show = ButtonsScroll.Width < ButtonsScroll.ContentSize.Width;
            ButtonsRight.IsVisible = show;
            ButtonsLeft.IsVisible = show;

            ButtonsLeft.IsEnabled = ButtonsScroll.ScrollX > 0;
            ButtonsRight.IsEnabled = ButtonsScroll.ScrollX < (ButtonsScroll.ContentSize.Width - ButtonsScroll.Width);
        }

        private void ButtonsRight_Pressed(object sender, EventArgs e)
        {
            ButtonsScroll.ScrollToAsync(ButtonsScroll.ScrollX + 120, 0, true).ConfigureAwait(false);
        }

        private void ButtonsLeft_Pressed(object sender, EventArgs e)
        {
            ButtonsScroll.ScrollToAsync(ButtonsScroll.ScrollX - 120, 0, true).ConfigureAwait(false);
        }

        private void Scroll_SizeChanged(object sender, EventArgs e)
        {
            tabWidth = Scroll.Width;
            foreach (TabView tab in Children)
            {
                tab.WidthRequest = tabWidth;
            }
            double desiredScroll = currentTab != null ? currentTab.X : 0;
            Scroll.ScrollToAsync(desiredScroll, 0, false);
        }

        private int TabFromX(double x)
        {
            x += tabWidth / 2;
            for (int i = 0; i < Tabs.Children.Count; i++)
            {
                var tab = Tabs.Children[i];
                if ((tab.X + tab.Width) >= x)
                    return i;
            }
            return 0;
        }

        public int CurrentTab
        {
            get => currentTabIndex;

            set
            {
                if (value >= Tabs.Children.Count) value = Tabs.Children.Count - 1;
                if (value < 0) value = 0;

                for (int i = 0; i < Buttons.Children.Count; i++)
                {
                    if (i == value)
                    {
                        (Buttons.Children[i] as Button).IsActive = true;
                        ButtonsScroll.ScrollToAsync(Buttons.Children[i], ScrollToPosition.MakeVisible, true).ConfigureAwait(false);
                    }
                    else
                    {
                        (Buttons.Children[i] as Button).IsActive = false;

                    }
                }

                currentTabIndex = value;
                currentTab = value < Tabs.Children.Count ? Tabs.Children[value] as TabView : null;

                double desiredScroll = currentTab != null? currentTab.X:0;
                if (desiredScroll != Scroll.ScrollX) Scroll.ScrollToAsync(desiredScroll, 0, true).ConfigureAwait(false);

            }
        }

        private void Scroll_ScrollEnd(object sender, EventArgs e)
        {
            // Snap scrolling to a tab point
            CurrentTab = TabFromX(Scroll.ScrollX);
        }

        private void Scroll_Scrolled(object sender, ScrolledEventArgs e)
        {
            // Change active tab as we scroll over them
            currentTabIndex = TabFromX(Scroll.ScrollX); 
            currentTab = currentTabIndex < Tabs.Children.Count ? Tabs.Children[currentTabIndex] as TabView : null;

            for (int i = 0; i < Buttons.Children.Count; i++)
            {
                (Buttons.Children[i] as Button).IsActive = (i == currentTabIndex);
            }
        }

        private void Tabs_ChildAdded(object sender, ElementEventArgs e)
        {
            TabView tab = e.Element as TabView;
            tab.WidthRequest = tabWidth;

            var button = CreateButton();
            button.BindingContext = tab;
            Buttons.Children.Add(button);

            if (Tabs.Children.Count == 1)
            {
                currentTab = tab;
                button.IsActive = true;
            }
        }

        private void Tabs_ChildRemoved(object sender, ElementEventArgs e)
        {
            // Remove the button
            for (int i = 0; i < Buttons.Children.Count; i++)
            {
                if (Buttons.Children[i] == e.Element)
                {
                    Buttons.Children.RemoveAt(i);
                    break;
                }
            }

            int newCurrent = currentTabIndex;
            if (currentTab == e.Element)
            {
                if (newCurrent >= Tabs.Children.Count) newCurrent--;
            }
            else
            {
                for (int i = 0; i < Tabs.Children.Count; i++)
                {
                    if (Tabs.Children[i] == currentTab)
                    {
                        newCurrent = i;
                        break;
                    }
                }
            }
            CurrentTab = newCurrent;
        }

        private void Tabs_ChildrenReordered(object sender, EventArgs e)
        {
            Buttons.Children.Clear();

            int newCurrent = 0;
            for (int i = 0; i < Tabs.Children.Count; i++)
            {
                var tab = Tabs.Children[i];

                var button = CreateButton();
                button.BindingContext = tab;
                Buttons.Children.Add(button);
                if (tab == currentTab)
                {
                    button.IsActive = true;
                    newCurrent = i;
                }
            }

            currentTabIndex = newCurrent;
            double desiredScroll = currentTab != null ? currentTab.X : 0;
            Scroll.ScrollToAsync(desiredScroll, 0, false);
        }

        Button CreateButton()
        {
            Button button = new Button { Style = buttonStyle };
            button.Clicked += Button_Clicked;
            return button;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            for (int i = 0; i < Tabs.Children.Count; i++)
            {
                if (Tabs.Children[i] == button.BindingContext)
                {
                    CurrentTab = i;
                    break;
                }
            }
        }
    }
}