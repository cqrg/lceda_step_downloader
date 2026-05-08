# 立创EDA 3D模型下载器

Fork from [seishinkouki/lceda_step_downloader](https://github.com/seishinkouki/lceda_step_downloader)

从 [LCSC 商城](https://item.szlcsc.com/) 搜索电子元器件，下载 STEP/OBJ 3D 模型，预览原理图、封装和实物图片，生成引脚 CSV 列表。

## 新增功能（v1.1.0）

相比原始版本，新增以下功能：

### 交互式原理图/封装预览
- WebView2 内嵌浏览器原生 SVG 渲染，支持鼠标滚轮缩放（光标位置为原点）和拖拽平移
- 封装预览黑色背景，原理图白色背景
- 右下角悬浮按钮：回中 / 缩小 / 放大
- 下载原理图 SVG、封装 SVG 至本地目录

### 引脚列表生成
- 解析 EasyEDA 符号行式数据格式，提取引脚编号和名称
- 导出 CSV 文件（PinNumber, PinName）

### 搜索与信息展示
- 列表显示列头，支持拖拽调整列宽和排序列顺序
- 价格梯度显示（从 JLCPCB 实时查询，鼠标悬停查看）
- "详情"列直达 LCSC 器件详情页
- "实物图片"自动获取高清大图

### 3D 模型
- 选中器件自动加载 3D 模型预览（HelixToolkit 渲染）
- 下载 STEP 格式 3D 模型用于专业 EDA 工具
- OBJ 缓存加速二次预览

### 规格书
- 在线查看 / 下载 PDF 规格书

## 下载

从 [Releases](https://github.com/cqrg/lceda_step_downloader/releases) 下载最新版本。

## 构建

```bash
dotnet restore
dotnet build -c Release
```

要求 .NET 6.0 SDK，Windows 10/11（需 [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)，Win11 已内置）。

## 技术栈

- .NET 6.0 WPF (x86)
- Stylet MVVM 框架
- HandyControls UI 控件库
- HelixToolkit.Wpf 3D 渲染
- Microsoft.Web.WebView2 内嵌浏览器

## License

与原项目保持一致。
