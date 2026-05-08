using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Svg.Skia;

namespace lceda_step_downloader.Controls
{
    public class SvgViewer : Canvas
    {
        public static readonly DependencyProperty SvgPathProperty =
            DependencyProperty.Register(
                nameof(SvgPath),
                typeof(string),
                typeof(SvgViewer),
                new PropertyMetadata(null, OnSvgPathChanged));

        public static readonly DependencyProperty SvgContentProperty =
            DependencyProperty.Register(
                nameof(SvgContent),
                typeof(string),
                typeof(SvgViewer),
                new PropertyMetadata(null, OnSvgContentChanged));

        public string SvgPath
        {
            get => (string)GetValue(SvgPathProperty);
            set => SetValue(SvgPathProperty, value);
        }

        public string SvgContent
        {
            get => (string)GetValue(SvgContentProperty);
            set => SetValue(SvgContentProperty, value);
        }

        private SKSvg _svg;
        private SKPicture _picture;
        private double _zoom = 1.0;
        private Point _panOffset;
        private Point _lastMousePos;
        private bool _isPanning;

        public SvgViewer()
        {
            Background = Brushes.White;
            ClipToBounds = true;

            MouseWheel += OnMouseWheel;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            SizeChanged += OnSizeChanged;
        }

        private static void OnSvgPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgViewer viewer)
            {
                viewer.LoadSvgFromFile(e.NewValue as string);
            }
        }

        private static void OnSvgContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgViewer viewer)
            {
                viewer.LoadSvgFromContent(e.NewValue as string);
            }
        }

        private void LoadSvgFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _svg = null;
                _picture = null;
                InvalidateVisual();
                return;
            }

            try
            {
                _svg = new SKSvg();
                _picture = _svg.Load(path);
                ResetView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 SVG 文件失败: {ex.Message}");
                _svg = null;
                _picture = null;
                InvalidateVisual();
            }
        }

        private void LoadSvgFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                _svg = null;
                _picture = null;
                InvalidateVisual();
                return;
            }

            try
            {
                _svg = new SKSvg();
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                _picture = _svg.Load(stream);
                ResetView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 SVG 内容失败: {ex.Message}");
                _svg = null;
                _picture = null;
                InvalidateVisual();
            }
        }

        private void ResetView()
        {
            if (_picture == null) return;

            var bounds = _picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // 计算适合控件的缩放比例
            var scaleX = ActualWidth / bounds.Width;
            var scaleY = ActualHeight / bounds.Height;
            _zoom = Math.Min(scaleX, scaleY) * 0.9; // 留 10% 边距

            // 居中显示
            _panOffset = new Point(
                (ActualWidth - bounds.Width * _zoom) / 2 - bounds.Left * _zoom,
                (ActualHeight - bounds.Height * _zoom) / 2 - bounds.Top * _zoom);

            InvalidateVisual();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_picture == null) return;

            var mousePos = e.GetPosition(this);
            var oldZoom = _zoom;

            // 缩放
            var delta = e.Delta > 0 ? 1.1 : 0.9;
            _zoom *= delta;
            _zoom = Math.Max(0.1, Math.Min(100, _zoom));

            // 调整偏移以保持鼠标位置不变
            var zoomRatio = _zoom / oldZoom;
            _panOffset = new Point(
                mousePos.X - (mousePos.X - _panOffset.X) * zoomRatio,
                mousePos.Y - (mousePos.Y - _panOffset.Y) * zoomRatio);

            InvalidateVisual();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_picture == null) return;

            _isPanning = true;
            _lastMousePos = e.GetPosition(this);
            CaptureMouse();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ReleaseMouseCapture();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning || _picture == null) return;

            var currentPos = e.GetPosition(this);
            var delta = currentPos - _lastMousePos;

            _panOffset = new Point(
                _panOffset.X + delta.X,
                _panOffset.Y + delta.Y);

            _lastMousePos = currentPos;
            InvalidateVisual();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_picture != null)
            {
                ResetView();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_picture == null) return;

            // 绘制背景
            dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));

            // 应用变换并绘制 SVG
            dc.PushTransform(new TranslateTransform(_panOffset.X, _panOffset.Y));
            dc.PushTransform(new ScaleTransform(_zoom, _zoom));

            // 将 SKPicture 转换为 WPF 可绘制对象
            var imageSize = new SKSizeI(
                (int)(_picture.CullRect.Width * _zoom),
                (int)(_picture.CullRect.Height * _zoom));
            var image = SKImage.FromPicture(_picture, imageSize);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            dc.DrawImage(bitmap, new Rect(
                _picture.CullRect.Left,
                _picture.CullRect.Top,
                _picture.CullRect.Width,
                _picture.CullRect.Height));

            dc.Pop();
            dc.Pop();
        }
    }
}
