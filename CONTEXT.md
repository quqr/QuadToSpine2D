# QuadToSpine2D 领域词汇表

本文档定义项目中使用的核心领域术语。维护者应使用这些术语命名模块、类和方法，避免使用同义词。

## 核心概念

### Quad
Vanillaware 游戏引擎使用的 2D 动画格式。包含骨骼（Skeleton）、动画（Animation）、关键帧（Keyframe）、槽位（Slot）、命中盒（Hitbox）等数据结构。

### Spine
Spine 2D 动画编辑器使用的 JSON 格式。本项目的目标输出格式。

### 转换（Conversion）
将 Quad 格式数据转换为 Spine 格式的完整流程。

## 数据结构

### Skeleton（骨骼）
一个完整的动画角色定义，包含多个 Bone 引用。

### Animation（动画）
时间线（Timeline）的有序集合，定义了动画的帧序列。

### Keyframe（关键帧）
某一帧的图层快照，包含图层的位置、纹理、UV 等信息。

### KeyframeLayer（图层）
关键帧内的单个图层，包含源四边形（Srcquad）、目标四边形（Dstquad）、纹理ID（TexId）等属性。

### Timeline（时间线）
动画帧的容器，记录帧数、附件引用、变换矩阵等信息。

### Slot（槽位）
图层的容器，可以包含多个附件（Attach）。

### Attachment（附件）
可附加到槽位的对象，类型包括 Keyframe、Slot、Hitbox 等。

### Hitbox（命中盒）
碰撞检测区域，包含多层（HitboxLayer）。

### Pool（对象池）
复用 PoolData 对象的机制，优化内存分配。

## 处理流程

### ProcessQuadJsonFile
加载 Quad JSON 文件，执行反序列化和动画合并。

### ProcessSpine2DJson
将 Quad 数据转换为 Spine 格式的核心处理器。

### ProcessImages
处理纹理图像，执行裁剪和保存。

## 配置

### IConverterSettings（提议）
转换器配置接口，用于解耦核心层与 UI 层。

## 架构术语

参见 [LANGUAGE.md](docs/agents/LANGUAGE.md) 中的标准定义：
- Module（模块）
- Interface（接口）
- Implementation（实现）
- Depth（深度）
- Seam（接缝）
- Adapter（适配器）