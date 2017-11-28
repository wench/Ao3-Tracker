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

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Renderscripts;
using Android.Graphics.Drawables;
using Xamarin.Forms.Platform.Android;
using System.ComponentModel;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Ao3TrackReader.Controls.PaneView), typeof(Ao3TrackReader.Droid.PaneViewRenderer))]
namespace Ao3TrackReader.Droid
{
    public class PaneViewRenderer : Xamarin.Forms.Platform.Android.ViewRenderer<Ao3TrackReader.Controls.PaneView, View>
    {
        Canvas sourceCanvas;
        Bitmap source;
        Bitmap blurred;
        RenderScript rs;
        ScriptIntrinsicBlur script;
        bool useBlur;
        Color tint;

        public PaneViewRenderer(Context context) : base(context)
		{
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
            {
                App.Database.TryGetVariable("PaneViewRenderer.useBlur", bool.TryParse, out useBlur);
                App.Database.GetVariableEvents("PaneViewRenderer.useBlur").Updated += DatabaseVariable_Updated;
            }
            else
            {
                useBlur = false;
            }

            tint = Ao3TrackReader.Resources.Colors.Alt.Trans.VeryHigh.ToAndroid();
            tint.A = 0xDD;
        }

        private void DatabaseVariable_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                bool.TryParse(e.NewValue, out useBlur);

                if (useBlur)
                {
                    if (IsAttachedToWindow)
                    {
                        if (rs == null)
                            rs = RenderScript.Create(Context);

                        if (rs != null)
                            script = ScriptIntrinsicBlur.Create(rs, Android.Renderscripts.Element.U8_4(rs));
                    }
                }
                else
                {
                    script?.Dispose();
                    script = null;
                    sourceCanvas?.Dispose();
                    sourceCanvas = null;
                    source?.Dispose();
                    source = null;
                    blurred?.Dispose();
                    blurred = null;
                    rs?.Dispose();
                    rs = null;
                }

                if (Element != null)
                    UpdateBackgroundColor();
            });
        }

        [Obsolete]
        public PaneViewRenderer() : base()
        {

        }

        protected override void OnDetachedFromWindow()
        {
            script?.Dispose();
            script = null;
            sourceCanvas?.Dispose();
            sourceCanvas = null;
            source?.Dispose();
            source = null;
            blurred?.Dispose();
            blurred = null;
            rs?.Dispose();
            rs = null;

            if (Element != null)
                UpdateBackgroundColor();

            base.OnDetachedFromWindow();
        }
        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            if (useBlur)
            {
                rs = RenderScript.Create(Context);

                if (rs != null)
                    script = ScriptIntrinsicBlur.Create(rs, Android.Renderscripts.Element.U8_4(rs));

                if (Element != null)
                    UpdateBackgroundColor();
            }
        }

        View lastSourceView = null;
        
        protected override void OnDraw(Canvas canvas)
        {
            canvas.Save();

            try
            {
                if (rs != null)
                {
                    var mainLayoutRenderer = WebViewPage.Current.MainLayoutRenderer;
                    var sourceView = mainLayoutRenderer.View;

                    if (lastSourceView != sourceView)
                    {
                        if (lastSourceView != null)
                            lastSourceView.ViewTreeObserver.PreDraw -= ViewTreeObserver_PreDraw;

                        sourceView.ViewTreeObserver.PreDraw += ViewTreeObserver_PreDraw;
                        lastSourceView = sourceView;
                    }

                    // Create bitmaps if needed
                    if ((sourceView.Width + 50) != source?.Width || (sourceView.Height + 50) != source?.Height)
                    {
                        sourceCanvas?.Dispose();
                        source?.Dispose();
                        blurred?.Dispose();

                        source = Bitmap.CreateBitmap(sourceView.Width + 50, sourceView.Height + 50, Bitmap.Config.Argb8888);
                        source.EraseColor(Ao3TrackReader.Resources.Colors.Alt.High.ToAndroid());


                        blurred = Bitmap.CreateBitmap(sourceView.Width + 50, sourceView.Height + 50, Bitmap.Config.Argb8888);
                        sourceCanvas = new Canvas(source);
                    }


                    int[] sourceLoc = new int[2];
                    sourceView.GetLocationInWindow(sourceLoc);

                    int[] destLoc = new int[2];
                    GetLocationInWindow(destLoc);

                    var xOffset = sourceLoc[0] - sourceView.TranslationX - 25 - destLoc[0] + TranslationX;
                    var yOffset = sourceLoc[1] - sourceView.TranslationY - 25 - destLoc[1] + TranslationY;

                    sourceCanvas.Save();
                    sourceCanvas.ClipRect((int)-xOffset, (int)-yOffset, (int)-xOffset + Width + 50, (int)-yOffset + Height + 50);
                    sourceCanvas.Translate(25, 25);
                    sourceView.Draw(sourceCanvas);
                    sourceCanvas.Restore();

                    // Allocate memory for Renderscript to work with
                    Allocation input = Allocation.CreateFromBitmap(rs, source, Allocation.MipmapControl.MipmapFull, AllocationUsage.Script);
                    Allocation output = Allocation.CreateTyped(rs, input.Type);

                    // Load up an instance of the specific script that we want to use.
                    script.SetInput(input);

                    // Set the blur radius
                    script.SetRadius(25);


                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                    {
                        var lo = new Script.LaunchOptions();
                        lo.SetX((int)-xOffset, (int)-xOffset + Width);
                        lo.SetY((int)-yOffset, (int)-yOffset + Height);
                        script.ForEach(output, lo);
                    }
                    else
                    {
                        script.ForEach(output);
                    }

                    // Copy the output to the blurred bitmap
                    output.CopyTo(blurred);

                    input.Dispose();
                    output.Dispose();

                    canvas.ClipRect(0, 0, Width, Height);
                    canvas.Translate(xOffset, yOffset);
                    canvas.DrawBitmap(blurred, 0, 0, null);
                    canvas.DrawColor(tint);
                }

            }
            catch
            {
                script?.Dispose();
                script = null;
                sourceCanvas?.Dispose();
                sourceCanvas = null;
                source?.Dispose();
                source = null;
                blurred?.Dispose();
                blurred = null;
                rs?.Dispose();
                rs = null;

                useBlur = false;
                App.Database.GetVariableEvents("PaneViewRenderer.useBlur").Updated -= DatabaseVariable_Updated;

                UpdateBackgroundColor();
            }
            canvas.Restore();

            base.OnDraw(canvas);
        }

        private void ViewTreeObserver_PreDraw(object sender, ViewTreeObserver.PreDrawEventArgs e)
        {
            if (!IsDirty && lastSourceView.IsDirty)
                Invalidate();
        }

        protected override void UpdateBackgroundColor()
        {
            if (rs != null) Background = new ColorDrawable(Color.Transparent);
            else base.UpdateBackgroundColor();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == Xamarin.Forms.VisualElement.TranslationXProperty.PropertyName ||
                e.PropertyName == Xamarin.Forms.VisualElement.IsEnabledProperty.PropertyName || e.PropertyName == Xamarin.Forms.VisualElement.IsVisibleProperty.PropertyName)
            {
                Invalidate();
            }
        }
    }
}