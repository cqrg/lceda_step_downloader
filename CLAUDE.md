# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

立创EDA 3D模型下载器 — 一个 Windows WPF 桌面应用，用于从 LCSC 商城搜索电子元器件，下载 STEP/OBJ 3D 模型，预览原理图和封装，生成引脚 CSV 列表。

## 构建与运行

```bash
dotnet restore
dotnet build
dotnet build -c Release
dotnet run --project lceda_step_downloader/lceda_step_downloader.csproj
```

项目没有自动化测试。编译后通知用户手动验证，不要自动执行 `dotnet build`。

## 技术栈

- .NET 6.0 WPF，目标平台 x86
- **Stylet** — MVVM 框架，通过 `{s:Action}` 绑定将 XAML 事件路由到 ViewModel 方法
- **HandyControls** — 中文化 WPF UI 控件库（SearchBar、Growl 通知、LoadingCircle 等）
- **HelixToolkit.Wpf** — WPF 3D 视口，用于 OBJ 模型预览
- **Microsoft.Web.WebView2** — 内嵌 Chromium 浏览器，用于 SVG 查看器
- 已移除 `SkiaSharp.Views.WPF` / `Svg.Skia`（原 SVG 渲染方案）

## 架构：Stylet MVVM（ViewModel-first）

- `Bootstrapper.cs` — 组合根，将 `RootViewModel` 注册为 Stylet 的根 ViewModel
- `ViewModels/RootViewModel.cs` — **唯一的 ViewModel**，承载全部业务逻辑。`PropertyChangedBase` 子类，包含搜索、API 调用、文件下载、OBJ/MTL 拆分、引脚列表生成及 UI 状态。底部定义三个值转换器（`IndexConverter`、`BooleanOrConverter`、`ViewerUrlConverter`）
- `Views/RootView.xaml` — **唯一的 View**。Stylet 通过命名约定（RootViewModel → RootView）自动解析
- `Views/RootView.xaml.cs` — 代码后台，处理 WebView2 初始化、属性变更监听、SVG→HTML 导航
- `Models/RootModel.cs` — LCSC 搜索 API 响应模型（`Root`、`ResultItem`、`Symbol`、`Footprint`、`Attributes` 等）
- `Models/ComponentModel.cs` — LCEDA 组件详情 API 响应模型（`Component`、`Result`）。Tags 和 version 字段使用 `JsonElement?` 以兼容不同文档类型的响应差异
- `WebBrowserHelper.cs` — WebBrowser 的 `BindableSource` 附加属性（遗留，WebView2 已替代）

## API 流程

| 功能 | API | 说明 |
|------|-----|------|
| 搜索 | `GET pro.lceda.cn/api/szlcsc/eda/product/list?wd=<keyword>` | 返回 `Models.Root.Root` |
| 价格查询 | `POST jlcpcb.com/api/overseas-pcb-order/v1/shoppingCart/smtGood/selectSmtComponentList` | 按 product_code 查询价格梯度 |
| 原理图/封装 SVG | `GET lceda.cn/api/products/<productCode>/svgs` | 返回按 docType 分组的 SVG |
| 符号引脚数据 | `GET pro.lceda.cn/api/components/<symbol_uuid>?uuid=<symbol_uuid>` | `dataStr` 字段为 EasyEDA 行式格式 |
| 3D 模型 UUID | `GET pro.lceda.cn/api/components/<uuid>?uuid=<uuid>` | `result.3d_model_uuid` |
| 下载 OBJ 模型 | `GET modules.lceda.cn/3dmodel/<uuid>` | OBJ+MTL 合并流，需 `ObjMtlSplit()` 分离 |
| 下载 STEP 模型 | `GET modules.lceda.cn/qAxj6KHrDKw4blvCG8QJPs7Y/<uuid>` | 直接写入 `step/` |

## EasyEDA 行式数据解析

符号 API 的 `dataStr` 不是标准 JSON，而是**每行一个 JSON 数组**的格式：

```
["DOCTYPE","SYMBOL","1.1"]
["PIN","e5",1,null,-85,115,10,0,null,0,0,1]
["ATTR","e6","e5","NAME","VBAT",false,true,-71.3,109.08502,0,"st3",0]
["ATTR","e7","e5","NUMBER","1",false,true,-75.5,114.08502,0,"st4",0]
```

`PIN` 行定义引脚（元素 ID 在索引 1），`ATTR` 行的 `"NAME"`/`"NUMBER"`（索引 3）用索引 2 的父元素 ID 关联到引脚。`ExtractPins()` 方法逐行解析此格式。

## 预览实现

- **原理图/封装**：WebView2 加载由 `BuildSvgViewerHtml()` 构建的 HTML，SVG 使用 `width`/`height` 属性控制缩放实现矢量渲染，鼠标滚轮/拖拽缩放平移
- **3D 模型**：HelixToolkit `HelixViewport3D` + `ObjReader`，OBJ 缓存到 `temp/`
- **器件图片**：直接绑定到 `Image` 控件，尝试将 `/middle/` 替换为 `/large/` 获取大图

## WebView2 注意事项

- WebView2 使用 HWND 渲染，**WPF 控件无法叠加在其上方**（空气域问题）。UI 按钮/提示必须放在 WebView2 HTML 内部或避开 WebView2 区域
- Growl 通知容器因空气域问题移至窗口左下角（`HorizontalAlignment="Left" VerticalAlignment="Bottom" Margin="10,0,0,60"`）
- 两个 WebView2 实例（原理图/封装）在 `RootView_Loaded` 中初始化，`OnViewModelPropertyChanged` 监听 `SchematicSvg`/`FootprintSvg` 属性变更并调用 `NavigateToSvgAsync`

## 输出目录

所有目录在构造函数中自动创建（`Directory.CreateDirectory` 自身幂等，无需 `Exists` 检查）：
- `temp/` — OBJ 模型缓存
- `step/` — STEP 文件
- `symbols/` — 原理图 SVG 下载
- `footprints/` — 封装 SVG 下载
- `datasheets/` — 规格书 PDF
- `pinlists/` — 引脚 CSV 列表

## HttpClient 配置

`HttpClient` 是静态的，启用 `AutomaticDecompression = DecompressionMethods.GZip`。使用 `GetStreamAsync` 时注意用 `using var stream` 释放网络流避免连接泄漏。后台线程通过 `Task.Run` 访问 `Selecteditem` 时必须捕获局部引用防止竞态。
