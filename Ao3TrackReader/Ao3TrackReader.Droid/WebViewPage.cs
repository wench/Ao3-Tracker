using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using Xamarin.Forms;
using Ao3TrackReader.Helper;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms.Platform.Android;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Ao3TrackReader
{
    public partial class WebViewPage : IEventHandler
    {
        public bool canGoBack
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool canGoForward
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string[] cssToInject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double leftOffset
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string NextPage
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public double opacity
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string PrevPage
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string[] scriptsToInject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void GoBack()
        {
            throw new NotImplementedException();
        }

        public void GoForward()
        {
            throw new NotImplementedException();
        }

        public Task<string> showContextMenu(double x, double y, [ReadOnlyArray] string[] menuItems)
        {
            throw new NotImplementedException();
        }
    }
}