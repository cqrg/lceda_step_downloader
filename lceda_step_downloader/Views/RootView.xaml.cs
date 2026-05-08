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
                    await NavigateToSvgAsync(SchematicViewer, ((RootViewModel)sender).SchematicSvg, "#ffffff");
                    break;
                case nameof(RootViewModel.FootprintSvg):
                    await NavigateToSvgAsync(FootprintViewer, ((RootViewModel)sender).FootprintSvg, "#000000");
                    break;
            }
        }

        private async Task NavigateToSvgAsync(WebView2 webView, string svgContent, string bgColor = "#ffffff")
        {
            if (webView.CoreWebView2 == null)
                await webView.EnsureCoreWebView2Async(null);

            if (string.IsNullOrEmpty(svgContent))
            {
                webView.CoreWebView2.NavigateToString($"<html><body style='background:{bgColor}'></body></html>");
                return;
            }

            var html = BuildSvgViewerHtml(svgContent, bgColor);
            webView.CoreWebView2.NavigateToString(html);
        }

        private static string BuildSvgViewerHtml(string svgContent, string bgColor)
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
html,body{width:100%;height:100%;overflow:hidden;background:" + bgColor + @"}
#wrap{width:100%;height:100%;overflow:hidden;position:relative;cursor:grab}
#wrap:active{cursor:grabbing}
#wrap svg{position:absolute;top:0;left:0;display:block}
</style>
</head>
<body>
<div id='wrap'>
" + svgContent + @"
</div>
<script>
(function(){
var wrap=document.getElementById('wrap');
var svg=wrap.querySelector('svg');
if(!svg)return;

// 解析 viewBox 获取原始宽高比
var vb=svg.getAttribute('viewBox');
var vbW=100,vbH=100;
if(vb){
  var parts=vb.trim().split(/\s+/);
  if(parts.length>=4){vbW=+parts[2];vbH=+parts[3];}
}

// 初始适配窗口，作为基准缩放
var baseW,baseH,offsetX,offsetY;
function fitToScreen(){
  var ww=wrap.clientWidth, wh=wrap.clientHeight;
  var scale=Math.min(ww/vbW, wh/vbH)*0.9;
  baseW=vbW*scale; baseH=vbH*scale;
  offsetX=(ww-baseW)/2; offsetY=(wh-baseH)/2;
}
fitToScreen();

var zoom=1, panX=0, panY=0, panning=!1, lx, ly;

function updateSVG(){
  var w=baseW*zoom, h=baseH*zoom;
  svg.setAttribute('width', w);
  svg.setAttribute('height', h);
  svg.style.left=offsetX+panX+'px';
  svg.style.top=offsetY+panY+'px';
}
updateSVG();

wrap.addEventListener('wheel',function(e){
  e.preventDefault();
  var z=e.deltaY>0?0.9:1.1;
  zoom=Math.max(0.1,Math.min(20,zoom*z));
  updateSVG();
},{passive:!1});

wrap.addEventListener('mousedown',function(e){
  panning=!0;lx=e.clientX;ly=e.clientY;
});

window.addEventListener('mouseup',function(){panning=!1});

window.addEventListener('mousemove',function(e){
  if(!panning)return;
  panX+=e.clientX-lx;panY+=e.clientY-ly;
  lx=e.clientX;ly=e.clientY;
  updateSVG();
});

window.addEventListener('resize',function(){
  fitToScreen();
  updateSVG();
});

window.__svgControl={
  zoomIn:function(){zoom=Math.min(20,zoom*1.3);updateSVG();},
  zoomOut:function(){zoom=Math.max(0.1,zoom/1.3);updateSVG();},
  recenter:function(){zoom=1;panX=0;panY=0;updateSVG();}
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
