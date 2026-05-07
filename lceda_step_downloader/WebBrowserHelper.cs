using System;
using System.Windows;
using System.Windows.Controls;

namespace lceda_step_downloader
{
    public static class WebBrowserHelper
    {
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached(
                "BindableSource",
                typeof(Uri),
                typeof(WebBrowserHelper),
                new PropertyMetadata(null, OnBindableSourceChanged));

        public static Uri GetBindableSource(DependencyObject obj) =>
            (Uri)obj.GetValue(BindableSourceProperty);

        public static void SetBindableSource(DependencyObject obj, Uri value) =>
            obj.SetValue(BindableSourceProperty, value);

        private static void OnBindableSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebBrowser browser)
            {
                browser.Navigate(e.NewValue as Uri ?? new Uri("about:blank"));
            }
        }
    }
}
