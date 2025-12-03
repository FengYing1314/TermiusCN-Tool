# Termius 汉化工具

![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=flat&logo=windows11)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?style=flat&logo=dotnet)
![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D4?style=flat&logo=microsoft)
![License](https://img.shields.io/badge/license-MIT-green)

基于 .NET 8 + WinUI 3 构建的现代化 Termius 汉化工具，专为 Windows设计。  
此项目诞生是因**termius**一直更新都需要手动替换app.asar。  
作者一直都懒得换，所以有了这个项目。

## ✨ 功能特性

- **🎨 原生体验** - 采用 WinUI 3 构建，完美契合 Windows 11 设计。
- **⚡ 一键汉化** - 自动检测版本，支持标准版/试用版/离线版一键替换。
- **🛡️ 安全可靠** - 自动备份原始文件，支持一键还原，操作无忧。
- **🔧 实用功能** - 支持 HTTP 代理、拖放文件、进程管理等。

## 📥 下载与安装

请前往 [Releases](https://github.com/FengYing1314/TermiusCN-Tool/releases) 页面下载最新版本。

### 📦 MSIX 安装包 (推荐)
下载 `TermiusCN-Tool-x64.msix` 或 `arm64.msix`。
> **注意**：首次安装可能需要开启 Windows 的「开发者模式」或安装证书。

### 📂 便携版 (Portable)
下载 `TermiusCN-Tool-Portable-x64.zip`。
解压后直接运行 `TermiusCN-Tool.exe` 即可，无需安装。

## 🚀 使用指南

1. **启动工具**：打开应用，工具会自动检测 Termius 安装路径。
2. **选择类型**：根据需求选择汉化类型（推荐标准版）。
3. **执行汉化**：点击“一键汉化”，等待完成。
4. **重启 Termius**：享受中文界面！

*如需还原，进入“备份管理”点击还原即可。*

## 🗑️ 卸载与清理

### 卸载工具
- **MSIX 版**：在 Windows 设置中直接卸载。
- **便携版**：直接删除程序文件夹。

### 清除数据
本工具会在 `%AppData%\TermiusCN-Tool` 目录下存储配置文件和备份。
如需彻底清除，请在工具的 **设置** 页面点击 **「清除所有数据并退出」**，或手动删除上述文件夹。

## 🛠️ 本地构建

```bash
# 克隆项目
git clone https://github.com/FengYing1314/TermiusCN-Tool.git

# 还原依赖
dotnet restore

# 发布 x64 版本
dotnet publish -c Release -r win-x64 -p:Platform=x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true --self-contained true
```

## 🙏 特别感谢

- [ArcSurge/Termius-Pro-zh_CN](https://github.com/ArcSurge/Termius-Pro-zh_CN) - 本项目使用的汉化文件来源

## ⚠️ 免责声明

本工具仅供学习交流，汉化资源来自社区。请支持正版软件。
作者不对使用本工具造成的任何数据丢失负责。


[def]: 