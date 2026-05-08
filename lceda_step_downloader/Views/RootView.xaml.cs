using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using lceda_step_downloader.Models.Root;
using lceda_step_downloader.ViewModels;
using Microsoft.Web.WebView2.Wpf;

namespace lceda_step_downloader.Views
{
    public partial class RootView : HandyControl.Controls.Window
    {
        public RootView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is RootViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (e.NewValue is RootViewModel newVm)
                newVm.PropertyChanged += OnViewModelPropertyChanged;
        }

        private async void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RootViewModel.SchematicSvg):
                    await NavigateToSvgAsync(SchematicViewer, ((RootViewModel)sender).SchematicSvg);
                    break;
                case nameof(RootViewModel.FootprintSvg):
                    await NavigateToSvgAsync(FootprintViewer, ((RootViewModel)sender).FootprintSvg);
                    break;
            }
        }

        private async Task NavigateToSvgAsync(WebView2 webView, string svgContent)
        {
            if (webView.CoreWebView2 == null)
                await webView.EnsureCoreWebView2Async(null);

            if (string.IsNullOrEmpty(svgContent))
            {
                webView.CoreWebView2.NavigateToString("<html><body style='background:#fff'></body></html>");
                return;
            }

            var html = BuildSvgViewerHtml(svgContent);
            webView.CoreWebView2.NavigateToString(html);
        }

        private static string BuildSvgViewerHtml(string svgContent)
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{width:100%;height:100%;overflow:hidden;background:#fff}
#viewer{width:100%;height:100%;display:flex;align-items:center;justify-content:center;cursor:grab}
#viewer:active{cursor:grabbing}
#viewer svg{will-change:transform;max-width:100%;max-height:100%}
</style>
</head>
<body>
<div id='viewer'>
" + svgContent + @"
</div>
<script>
(function(){
var viewer=document.getElementById('viewer');
var svg=viewer.querySelector('svg');
var zoom=1,px=0,py=0,panning=!1,lx,ly;

function update(){
  if(svg)svg.style.transform='translate('+px+'px,'+py+'px) scale('+zoom+')';
}

viewer.addEventListener('wheel',function(e){
  e.preventDefault();
  var z=e.deltaY>0?0.9:1.1;
  zoom=Math.max(0.1,Math.min(10,zoom*z));
  update();
},{passive:!1});

viewer.addEventListener('mousedown',function(e){
  panning=!0;lx=e.clientX;ly=e.clientY;
});

window.addEventListener('mouseup',function(){panning=!1});

window.addEventListener('mousemove',function(e){
  if(!panning)return;
  px+=e.clientX-lx;py+=e.clientY-ly;
  lx=e.clientX;ly=e.clientY;
  update();
});

window.__svgControl={
  zoomIn:function(){zoom=Math.min(10,zoom*1.3);update();},
  zoomOut:function(){zoom=Math.max(0.1,zoom/1.3);update();},
  recenter:function(){zoom=1;px=0;py=0;update();}
};
})();
</script>
</body>
</html>";
        }

        private async void RootView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await SchematicViewer.EnsureCoreWebView2Async(null);
                await FootprintViewer.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.Error($"WebView2 初始化失败: {ex.Message}");
            }
        }

        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is ResultItem resultItem)
            {
                if (!string.IsNullOrEmpty(resultItem.PriceInfo))
                {
                    item.ToolTip = resultItem.PriceInfo;
                }
                else
                {
                    item.ToolTip = "价格加载中...";
                }
            }
        }

        // Schematic overlay buttons
        private async void SchematicRecenter_Click(object sender, RoutedEventArgs e)
        {
            if (SchematicViewer.CoreWebView2 != null)
                await SchematicViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.recenter()");
        }

        private async void SchematicZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (SchematicViewer.CoreWebView2 != null)
                await SchematicViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.zoomOut()");
        }

        private async void SchematicZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (SchematicViewer.CoreWebView2 != null)
                await SchematicViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.zoomIn()");
        }

        // Footprint overlay buttons
        private async void FootprintRecenter_Click(object sender, RoutedEventArgs e)
        {
            if (FootprintViewer.CoreWebView2 != null)
                await FootprintViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.recenter()");
        }

        private async void FootprintZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (FootprintViewer.CoreWebView2 != null)
                await FootprintViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.zoomOut()");
        }

        private async void FootprintZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (FootprintViewer.CoreWebView2 != null)
                await FootprintViewer.CoreWebView2.ExecuteScriptAsync("window.__svgControl.zoomIn()");
        }
    }
}
