# 键盘去抖动 (Keyboard Unchatter)

[![GitHub License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-3.5-blue)

一个高效的 Windows 键盘去抖动实用工具，用于过滤机械键盘抖动，消除键盘连键现象。通过在后台监听键盘输入，自动过滤短时间内的重复按键事件。

## 特性

- ✨ **Material Design 设计风格** - 现代化的用户界面
- 🎯 **实时按键过滤** - 后台自动过滤重复按键
- ⚙️ **自定义阈值** - 支持调整去抖动时间（1-200ms）
- 📊 **按键统计** - 显示被拦截和成功输入的次数
- 🔧 **灵活配置** - 支持启动最小化、关闭后后台运行等选项
- 💾 **配置持久化** - 自动保存用户设置到 EXE 文件
- 🎨 **精美界面** - 微软雅黑字体、圆形阴影按钮、流畅动画

## 系统要求

- **操作系统**：Windows 7 及以上
- **.NET Framework**：3.5 或更高版本
- **内存**：最低 50MB

## 安装

### 直接使用

从 [Release](https://github.com/Amamiyashi0n/keyboard-unchatter-csharp/releases) 页面下载最新的 `.exe` 文件，双击即可运行。

### 编译源代码

#### 要求

- Visual Studio 2010 或更高版本
- .NET Framework 3.5 SDK

#### 步骤

1. 克隆仓库：
```bash
git clone https://github.com/Amamiyashi0n/keyboard-unchatter-csharp.git
cd keyboard-unchatter-csharp
```

2. 使用 Visual Studio 打开 `keyboard-unchatter-csharp.sln`

3. 构建项目：
   - 点击 "生成" → "生成解决方案"
   - 或按 Ctrl+Shift+B

4. 输出文件位于 `bin/Release/` 目录

## 使用方法

### 基本操作

1. **启动应用**
   - 双击 `keyboard-unchatter-csharp.exe` 启动程序

2. **启用过滤**
   - 点击主窗口底部的 "启用过滤" 按钮
   - 状态指示器会变为绿色并显示 "已启用"

3. **调整阈值**
   - 使用滑块或输入框调整 "抖动阈值"
   - 范围：1-200 毫秒（推荐 20ms）
   - 点击 "应用" 保存设置

4. **查看统计**
   - 点击导航栏的 "统计" 查看按键数据
   - 显示每个按键被拦截和成功输入的次数

### 主要功能

#### 🏠 主页

- **启用/停止过滤** - 控制去抖动功能的开启/关闭
- **阈值调整** - 自定义敏感度（毫秒）
- **启动选项** - 设置启动时最小化、关闭后后台运行

#### 📊 统计页面

- 显示所有被监听按键的统计信息
- "成功次数" - 通过过滤器的按键次数
- "拦截次数" - 被识别为抖动的按键次数

#### ℹ️ 关于页面

- 项目信息和链接
- GitHub 仓库地址
- 赞助信息

## 配置说明

### 设置项

应用会自动保存以下配置到 EXE 文件尾部：

```json
{
  "active": false,              // 启用状态
  "chatterThreshold": 20,       // 去抖动阈值（毫秒）
  "openMinimized": false,       // 启动时最小化
  "closeToTray": true           // 关闭后后台运行
}
```

### 配置存储

- **位置**：用户隔离存储 (`AppData\Local\IsolatedStorage`)
- **文件名**：`runtime.config.json`
- **特点**：配置内嵌到 EXE，无需外部文件

## UI 设计特点

- **Material Design 风格** - 符合现代设计规范
- **圆形导航按钮** - 带有阴影效果和悬停动画
- **增强的交互动画**：
  - 复选框：点击时有缩放和颜色过渡动画
  - 滑块：悬停时放大并显示阴影效果
  - 按钮：按下时有按压反馈
- **一致的字体** - 全局使用微软雅黑字体
- **内嵌资源** - Miku 头像图片内嵌到 EXE 中

## 常见问题

### Q：什么是键盘抖动？
A：机械键盘按键在接触时会产生微小的接触弹跳，导致单次按键被识别为多次，这就是"抖动"现象。

### Q：阈值应该设置多少？
A：大多数机械键盘的抖动时间在 5-50ms 之间，推荐设置为 20ms。如果误杀有效按键，可以降低阈值。

### Q：应用会影响游戏反作弊系统吗？
A：此应用在系统级别拦截按键，部分游戏的反作弊系统可能会检测到此行为。**在竞技游戏中使用前请检查相关游戏的条款**。

### Q：如何卸载应用？
A：直接删除 EXE 文件即可。配置存储在隔离存储中，若要完全清除可删除：
```
%LocalAppData%\IsolatedStorage\
```

### Q：应用会占用多少资源？
A：后台运行时内存占用约 30-50MB，CPU 占用接近 0%（仅在有按键时处理）。

## 技术栈

- **语言**：C# (.NET Framework 3.5)
- **UI 框架**：WPF
- **键盘钩子**：Windows API (SetWindowsHookEx)
- **配置管理**：自定义 JSON + IsolatedStorage
- **设计**：Material Design

## 项目结构

```
keyboard-unchatter-csharp/
├── App.xaml              # 应用全局样式和资源
├── App.xaml.cs           # 应用初始化逻辑
├── MainWindow.xaml       # 主窗口 UI
├── MainWindow.xaml.cs    # 主窗口业务逻辑
├── ToastWindow.xaml      # 托盘通知窗口
├── InputHook.cs          # 键盘钩子实现
├── KeyboardMonitor.cs    # 去抖动核心逻辑
├── KeyStatusList.cs      # 按键状态管理
├── Debug.cs              # 调试辅助
├── res/
│   └── miku.jpg          # 应用头像（内嵌）
└── Properties/
    ├── Settings.settings # 应用配置
    └── AssemblyInfo.cs   # 程序集信息
```

## 架构设计

### 关键组件

1. **InputHook** - Windows 低级键盘钩子，捕获系统级按键事件
2. **KeyboardMonitor** - 去抖动算法核心，维护按键状态机
3. **MainWindow** - UI 主窗口，展示配置和统计信息
4. **配置系统** - EXE 尾部嵌入式配置，支持持久化

## 贡献

欢迎提交 Issue 和 Pull Request！

### 贡献步骤

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 作者

- **Amamiyashi0n** - [GitHub Profile](https://github.com/Amamiyashi0n)

## 致谢

- Material Design 设计规范
- Windows API 文档
- .NET Framework 社区

## 相关链接

- 📖 [项目 Wiki](https://github.com/Amamiyashi0n/keyboard-unchatter-csharp/wiki)
- 🐛 [Issue 跟踪](https://github.com/Amamiyashi0n/keyboard-unchatter-csharp/issues)
- 💬 [讨论区](https://github.com/Amamiyashi0n/keyboard-unchatter-csharp/discussions)
- 💝 [赞助我](https://afdian.com/a/amamiyashion/plan)

## 注意事项

> ⚠️ 本应用在系统级别拦截键盘输入。在某些游戏或安全软件中可能被检测为异常行为。
>
> 使用本应用前，请确保：
> 1. 了解您的游戏/应用的反作弊政策
> 2. 在信任的环境中运行
> 3. 定期检查按键统计确保工作正常

---

**最后更新**：2026 年 2 月 20 日

**版本**：v0.1.0

希望这个工具能够改善您的键盘体验！ 🎉
