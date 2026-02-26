# QuadToSpine2D

[![许可证](https://img.shields.io/badge/许可证-GPL3.0-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![平台](https://img.shields.io/badge/平台-Windows-lightgrey.svg)]()

QuadToSpine2D 是一款强大的工具，可将 QUAD 格式的 2D 动画数据转换为 Spine 2D 格式，具有实时动画预览和无缝集成能力。

## 🎯 支持版本

+ Spine 2D 3.8

## 🚀 开始使用

### 环境要求

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* Windows 10 或更高版本
* Spine 2D 3.8（用于导入结果）

### 使用流程

#### 步骤1：转换 QUAD 到 Spine 格式
1. **选择 QUAD 文件**：点击 "Open Quad File" 按钮选择你的 .quad 文件
2. **添加图像资源**：
   - 点击 "+" 按钮添加新的图像组
   - 为每个组添加对应的图像文件路径
   - 确保图像顺序正确
3. **配置缩放因子**：如果图像比原始图像大，缩放因子 = 当前图像大小 / 原始图像大小
4. **开始转换**：点击 "Process Data" 按钮开始转换过程
5. **获取结果**：转换完成后，你将获得 **Result.json** 文件和 **images** 文件夹
<div align="center"><img src="README/5.png" alt="转换结果" width="600"/></div>

#### 步骤2：导入到 Spine
1. **打开 Spine**：启动 Spine 2D 软件
2. **导入 JSON**：导入 "Result.json" 文件（忽略警告）
   <div align="center"><img src="README/2.png" alt="导入JSON" width="250"/></div>
3. **导入图像**：导入生成的 images 文件夹
   <div align="center"><img src="README/3.png" alt="导入图像" width="200"/></div>
4. **检查动画**：验证动画是否正确显示（确保已选择皮肤）
   <div align="center"><img src="README/4.png" alt="检查动画" width="350"/></div>

#### 替代方案：预览器页面
1. **加载资源**：
   - 选择 QUAD 文件
   - 添加图像文件路径
   - 点击 "Load" 加载数据
2. **动画控制**：
   - 使用播放、暂停、帧控制等功能
<div align="center"><img src="README/1.png" alt="预览器" width="350"/></div>

## ⚙️ 配置选项

### 转换器设置
- **缩放因子**：调整输出图像的缩放比例
- **循环动画**：设置是否启用动画循环
- **JSON 格式化**：选择输出 JSON 的格式化方式
- **保存路径**：自定义结果文件和图像的保存位置

### 预览器设置
- **画布大小**：调整预览画布的尺寸
- **帧率控制**：设置动画播放的 FPS
- **图像缩放**：控制预览图像的显示比例

<div align="center"><img src="README/6.png" alt="配置选项" width="500"/></div>

## 🛠 设置指南

* [如何获取 quad 文件](https://github.com/rufaswan/Web2D_Games/blob/master/docs/psxtools-steps.adoc)
* [详细信息](https://www.vg-resource.com/thread-38430.html)

## ⚠️ 已知问题

1. **动画顺序问题**：某些动画显示顺序错误
2. **图层显示缺失**：某些动画图层完全不显示
3. **文件兼容性**：无法转换某些 quad 文件

## 💬 支持

如需支持，请在 GitHub 上提交 issue 或联系维护者。

*如需获取 QUAD 文件，请参考 [psxtools 文档](https://github.com/rufaswan/Web2D_Games/blob/master/docs/psxtools-steps.adoc)*