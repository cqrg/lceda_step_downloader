# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

立创EDA 3D模型下载器 — 一个 Windows WPF 桌面应用，用于从 LCSC 商城搜索电子元器件，并下载其 STEP/OBJ 格式的 3D 模型文件。

## 构建与运行

```bash
# 还原依赖并构建
dotnet restore
dotnet build

# 构建 Release 版本
dotnet build -c Release

# 运行
dotnet run --project lceda_step_downloader/lceda_step_downloader.csproj
```

项目没有自动化测试。

## 技术栈

- .NET 6.0 WPF，目标平台 x86
- **Stylet** — MVVM 框架，通过 `{s:Action}` 绑定将 XAML 事件路由到 ViewModel 方法
- **HandyControls** — 中文化 WPF UI 控件库（SearchBar、ComboBox、Growl 通知、LoadingCircle 等）
- **HelixToolkit.Wpf** — WPF 3D 视口，用于 OBJ 模型预览

## 架构：Stylet MVVM

项目使用 Stylet 的"ViewModel-first"风格：

- `Bootstrapper.cs` — 组合根。将 `RootViewModel` 注册为启动时由 Stylet 解析的根 ViewModel。`App.xaml` 通过 `ApplicationLoader` XML 加载它。
- `ViewModels/RootViewModel.cs` — **唯一的 ViewModel**，承载全部业务逻辑（约 480 行）。这是一个 `PropertyChangedBase` 子类，包含搜索、API 调用、文件下载、OBJ/MTL 拆分以及 UI 状态属性。底部还定义了两个值转换器（`IndexConverter`、`BooleanOrConverter`）。
- `Views/RootView.xaml` — **唯一的 View**。通过 Stylet 的命名约定（RootViewModel → RootView）自动解析。使用 HandyControls 窗口chrome 和 HelixToolkit 的 `HelixViewport3D` 进行 3D 渲染。
- `Models/RootModel.cs` — 来自 LCSC 搜索 API 的 JSON 响应模型（`Root`、`ResultItem`、`Attributes` 等）
- `Models/ComponentModel.cs` — 来自 LCEDA 组件详情 API 的 JSON 响应模型（`Component`、`Result`，含 `3d_model_uuid` 字段）

## 关键 API 流程

1. **搜索**：`GET https://pro.lceda.cn/api/szlcsc/eda/product/list?wd=<keyword>` → 反序列化为 `Models.Root.Root`
2. **获取 3D 模型 UUID**：`GET https://pro.lceda.cn/api/components/<model_uuid>?uuid=<model_uuid>` → 反序列化为 `Models.Component.Component`
3. **下载 OBJ**（预览）：`GET https://modules.lceda.cn/3dmodel/<uuid>` → 原始 OBJ+MTL 合并流 → `ObjMtlSplit()` 进行分离
4. **下载 STEP**：`GET https://modules.lceda.cn/qAxj6KHrDKw4blvCG8QJPs7Y/<uuid>` → 直接写入 `step/` 目录

`DownloadStepAsync()` 中的注释代码块展示了一个替代方案：构建 PCB 数据并通过 `pcb2step` API 进行 POST，之前曾使用该方案，后来被弃用，改为直接下载 STEP。

## 项目结构说明

- 根目录下的 `RootView.xaml` 和 `RootViewModel - 副本.cs` 是旧备份/草稿 —— 它们**不属于**实际构建。实际文件位于 `lceda_step_downloader/Views/` 和 `lceda_step_downloader/ViewModels/` 目录中。
- `lceda_step_downloader/asserts/` 包含静态资源（图片）。
- `lceda_step_downloader/doc/` 包含文档截图。
- OBJ 缓存写入 `temp/`（相对于执行目录），STEP 文件写入 `step/`（相对于执行目录）。两个目录在应用启动时会自动创建。
- HttpClient 是静态的，启用了 GZip 自动解压。
