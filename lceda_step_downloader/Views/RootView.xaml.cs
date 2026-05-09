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
#controls{position:fixed;bottom:6px;right:6px;z-index:999;display:flex;gap:3px;opacity:0.7;transition:opacity .2s}
#controls:hover{opacity:1}
#controls button{width:28px;height:26px;border:1px solid #999;border-radius:3px;background:" + bgColor + @";color:" + (bgColor == "#000000" ? "#ccc" : "#333") + @";font-size:13px;cursor:pointer;line-height:1;display:flex;align-items:center;justify-content:center}
#controls button:hover{background:" + (bgColor == "#000000" ? "#333" : "#e0e0e0") + @"}
</style>
</head>
<body>
<div id='wrap'>
" + svgContent + @"
</div>
<div id='controls'>
  <button onclick='__svgControl.recenter()' title='回中'>⊕</button>
  <button onclick='__svgControl.zoomOut()' title='缩小'>−</button>
  <button onclick='__svgControl.zoomIn()' title='放大'>+</button>
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

function applyZoom(newZoom, cx, cy){
  // cx/cy 是缩放原点在 wrap 容器中的坐标, 未传则居中
  if(cx==null){cx=wrap.clientWidth/2;cy=wrap.clientHeight/2;}
  var ratio=newZoom/zoom;
  panX=cx-offsetX-(cx-offsetX-panX)*ratio;
  panY=cy-offsetY-(cy-offsetY-panY)*ratio;
  zoom=newZoom;
  updateSVG();
}

wrap.addEventListener('wheel',function(e){
  e.preventDefault();
  var rect=wrap.getBoundingClientRect();
  var cx=e.clientX-rect.left, cy=e.clientY-rect.top;
  var z=e.deltaY>0?0.9:1.1;
  applyZoom(Math.max(0.1,Math.min(20,zoom*z)), cx, cy);
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
  zoomIn:function(){applyZoom(Math.min(20,zoom*1.3));},
  zoomOut:function(){applyZoom(Math.max(0.1,zoom/1.3));},
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

    }
}
