using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public partial class ReadingListView : ListView
	{
		public static Color GroupTitleColor { get {
				var c = App.Colors["SystemChromeAltLowColor"];
				return new Color(((int)(c.R * 255) ^ 0x90) / 255.0, ((int)(c.G * 255) ^ 0) / 510.0, ((int)(c.B * 255) ^ 0) / 255.0);
			} }
        public static Color GroupTypeColor { get { return App.Colors["SystemChromeHighColor"]; } }

        public event EventHandler<Object> ContextOpen;
        public void OnOpen(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);

            ContextOpen?.Invoke(this, mi.CommandParameter);
        }

        public event EventHandler<Object> ContextOpenLast;
        public void OnOpenLast(object sender, EventArgs e)
        {            
            var mi = ((MenuItem)sender);            

            ContextOpenLast?.Invoke(this, mi.CommandParameter);
        }

        public event EventHandler<Object> ContextDelete;
        public void OnDelete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            ContextDelete?.Invoke(this, mi.CommandParameter);
        }

        public event EventHandler<Object> AddPage;
        public void OnAddPage(object sender, EventArgs e)
        {
            AddPage?.Invoke(this, e);
        }

        public void OnClose(object sender, EventArgs e)
        {
            OnScreen = false;
        }

        public ReadingListView ()
		{
			InitializeComponent ();
		}

        public bool OnScreen
        {
            get
            {
                return TranslationX < Width / 2;
            }
            set
            {
                if (value == false)
                {
                    Unfocus();
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(Width, 0, 100, Easing.CubicIn);
                }
                else
                {
                    ViewExtensions.CancelAnimations(this);
                    this.TranslateTo(0, 0, 100, Easing.CubicIn);
                    Focus();
                }
            }
        }


    }
}
