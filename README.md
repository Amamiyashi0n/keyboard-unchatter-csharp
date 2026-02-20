# 键盘去抖动 (Keyboard Unchatter)

一个 Windows 键盘去抖动工具，用于过滤机械键盘抖动，消除键盘连键现象。
<img width="1285" height="842" alt="screenshot-20260220-225628" src="https://github.com/user-attachments/assets/c70dd54d-dbef-4102-bd6d-e42f16086825" />

## 功能

- 实时按键过滤 - 后台自动过滤重复按键
- 自定义阈值 - 支持调整去抖动时间（1-200ms）
- 按键统计 - 显示被拦截和成功输入的次数
- 灵活配置 - 支持启动最小化、关闭后后台运行
- 现代化界面 - Material Design 设计风格

## 系统要求

- Windows 7 及以上
- .NET Framework 3.5 或更高版本

## 安装

从 [Release](https://github.com/Amamiyashi0n/keyboard-unchatter-csharp/releases) 页面下载 `.exe` 文件，双击即可运行。

## 编译

1. 克隆仓库：
```bash
git clone https://github.com/Amamiyashi0n/keyboard-unchatter-csharp.git
```

2. 使用 Visual Studio 打开 `keyboard-unchatter-csharp.sln`

3. 生成解决方案（Ctrl+Shift+B）

4. 输出文件位于 `bin/Release/` 目录

## 使用

1. 启动应用后，点击 "启用过滤" 按钮启动去抖动
2. 使用滑块调整阈值（推荐 20ms）
3. 在统计页面查看按键被拦截的次数

## 配置

应用配置自动保存到 EXE 文件中，包括：

- 启用状态
- 去抖动阈值
- 启动选项
- 运行设置

## 技术

- 语言：C# (.NET Framework 3.5)
- UI 框架：WPF
- 键盘钩子：Windows API
- 设计：Material Design

## 许可证

MIT License - 详见 [LICENSE](LICENSE)

## 作者

Amamiyashi0n - [GitHub](https://github.com/Amamiyashi0n)

## 常见问题

Q: 什么是键盘抖动？
A: 机械键盘按键在接触时会产生微小的接触弹跳，导致单次按键被识别为多次。

Q: 阈值应该设置多少？
A: 大多数机械键盘的抖动时间在 5-50ms 之间，推荐设置为 20ms。

Q: 应用会影响游戏反作弊系统吗？
A: 此应用在系统级别拦截按键，部分游戏的反作弊系统可能会检测到此行为。在竞技游戏中使用前请检查相关游戏的条款。

Q: 如何卸载？
A: 直接删除 EXE 文件即可。
