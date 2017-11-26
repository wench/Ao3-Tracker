using System.Collections.Generic;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Ao3TrackReader.Droid
{
	internal static class ViewGroupExtensions
	{
		internal static IEnumerable<T> GetChildrenOfType<T>(this AViewGroup self) where T : AView
		{
			for (var i = 0; i < self.ChildCount; i++)
			{
				AView child = self.GetChildAt(i);
				var typedChild = child as T;
				if (typedChild != null)
					yield return typedChild;

				if (child is AViewGroup)
				{
					IEnumerable<T> myChildren = (child as AViewGroup).GetChildrenOfType<T>();
					foreach (T nextChild in myChildren)
						yield return nextChild;
				}
			}
		}

        internal static IEnumerable<T> GetParentOfType<T>(this AView self) where T : AView
        {
            var parent = self.Parent;
            if (parent is T typedChild)
                yield return typedChild;

            if (parent is AView av)
            {
                IEnumerable<T> myParents = av.GetParentOfType<T>();
                foreach (T nextParent in myParents)
                    yield return nextParent;
            }
        }
    }
}