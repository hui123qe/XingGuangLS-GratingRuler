# WPF XAML 人类可读代码生成规范（AI 编程约束）

> 版本：1.0  |  适用：C# / WPF 桌面应用（包括工业设备上位机）  |  优先级：**可读性 > 局部简短 > 炫技式抽象**
>
> 使用方式：将本文件放在项目 `docs/` 或仓库根目录，并在给 AI 的任务中声明：**“生成或修改 WPF 界面前，必须遵循本文件。”**

## 0. 目标与适用边界

目标是让开发者打开任意 `.xaml` 文件后，能快速回答：**界面分成哪些区域、每个区域使用什么控件、布局关系是什么、数据显示从哪里来、操作触发哪个命令、视觉样式定义在哪里。**

- 本文件约束 **AI 生成与修改 WPF UI 的写法**，不规定某种视觉风格。
- 规范分为 **MUST（必须）**、**SHOULD（推荐）**、**AVOID（原则上避免）**。
- 带有数值的“行数、层数”是本项目的 **代码评审启发式阈值，不是微软官方限制**；不能为满足阈值而把简单代码过度拆分。
- 优先遵循已有项目的命名、架构、主题系统和目标 .NET 版本；若与本文冲突，先说明冲突，不得擅自大规模重构。
- **禁止假造 WPF 原生属性或控件**：例如原生 `Grid` 没有 `RowSpacing/ColumnSpacing/Padding`，原生 `StackPanel` 没有 `Spacing`，原生 `Button` 没有 `CornerRadius`，原生 `TextBox` 没有 `PlaceholderText`。如使用第三方控件，必须确认已安装对应库。

## 1. 总体生成原则

1. **MUST：先布局，后样式，最后绑定和交互。** 不要一开始输出巨大的 `ControlTemplate`。
2. **MUST：XAML 表达“显示什么、放在哪里、如何绑定”，ViewModel 表达“显示状态、可执行命令”。** 设备通讯、线程、算法和业务流程不得写入 XAML 或 View 的 code-behind。
3. **MUST：在文件中按视觉顺序组织代码**：标题区 → 状态区 → 主内容区 → 操作区；与运行时界面从上到下、从左到右的阅读顺序保持一致。
4. **MUST：减少无意义的视觉树层级。** 不为一个 `TextBlock` 额外包装三层 `Grid/Border/StackPanel`。
5. **SHOULD：保留“局部直观性”。** 简单界面不要强制提取成大量 UserControl、Style 或 Converter，以免查一个按钮需跳转多个文件。
6. **MUST：修改已有界面时采用最小改动**，保留现有绑定、事件、资源键及设备控制语义，除非任务明确要求改动。

## 2. XAML 书写格式（统一遵守）

### 2.1 缩进、换行和空行

- 使用 **4 个空格**缩进，不使用 Tab；编码 UTF-8；统一代码格式化。
- **属性数 ≤ 2 且内容很短**：允许一行。例如 `<RowDefinition Height="Auto" />`。
- **属性数 ≥ 3 或整行过长**：标签独占一行，每个属性独占一行；`>` 和 `/>` 放在最后一个属性末尾。
- 单行建议不超过 **120 字符**（项目启发式阈值），但不要破坏绑定表达式的语义。
- 根节点的 `x:Class`、`xmlns`、`mc:Ignorable` 等分别换行。
- 不同语义区域之间保留一个空行；同一重复控件列表内部无需每一行都插空行。
- 简短子节点可单行；复杂节点必须多行展开，**禁止将复杂的 `Grid` 或 `DataTemplate` 压成单行**。

```xml
<!-- 推荐：结构和属性一眼可读 -->
<Button
    Grid.Row="2"
    MinWidth="100"
    Margin="8,0,0,0"
    Content="开始测量"
    Command="{Binding StartMeasureCommand}"
    IsEnabled="{Binding CanStartMeasure}" />

<!-- 推荐：只有一个属性的简单节点 -->
<RowDefinition Height="Auto" />
```

### 2.2 属性排序（相同类型的控件保持一致）

按以下组排序，不要求毫无意义的机械字母序：

1. 身份：`x:Name`、`x:Key`（仅在需要时使用）。
2. 布局位置：`Grid.Row`、`Grid.Column`、`Grid.RowSpan` 等。
3. 尺寸和间距：`MinWidth`、`Width`、`Height`、`Margin`、`Padding`、`HorizontalAlignment`、`VerticalAlignment`。
4. 外观：`Style`、`Background`、`Foreground`、`FontSize` 等。
5. 内容：`Text`、`Content`、`ItemsSource` 等。
6. 行为与绑定：`Command`、`CommandParameter`、`IsEnabled`、`Visibility` 等。

> 同一属性不应在 Style、局部属性和触发器里无缘由地多次覆盖；否则人工阅读难以判断最终效果。

### 2.3 注释

只给 **具有业务语义的界面分区**写注释，说明“区域是什么”，而不是复述“这里有一个 Grid”。

```xml
<!-- 01 页面标题与设备总状态 -->
...

<!-- 02 测量实时值 -->
...

<!-- 03 操作按钮 -->
...
```

- **AVOID**：对每一个 `TextBlock`、`Button` 写“文本控件”“按钮控件”。
- **MUST**：对于不直观的布局约束、设计取舍、特殊工况说明原因。
- **MUST**：不得留下 AI 的思考过程、无意义 TODO、被注释掉的大段过时代码。

## 3. 布局规范

| 需求 | 优先选择 | 理由 |
|---|---|---|
| 行列对齐、参数表单、主区域分栏 | `Grid` | 布局关系直接体现为行列 |
| 单方向少量元素排列 | `StackPanel` | 简单明确 |
| 顶部/底部/侧栏区域 | `DockPanel` 或 `Grid` | 清楚表达边缘布局 |
| 重复均分的少量内容 | `UniformGrid` | 不必写重复的行列定义 |
| 坐标图、设备示意图、轨迹叠加 | `Canvas` | 需要真实二维定位时适用 |

- **MUST**：普通窗口或表单采用相对布局；不要使用 `Canvas.Left/Top` 堆出整个页面。
- **MUST**：`Grid` 的 `RowDefinitions` 和 `ColumnDefinitions` 放在子控件之前；优先 `Auto`、`*`，仅在有明确尺寸需求时使用固定大小。
- **MUST**：每个子控件的 `Grid.Row/Column` 能对应已有的定义；禁止无效、超出范围或冲突的布局索引。
- **SHOULD**：用 `Margin` 表达控件之间的距离，用 `Padding` 表达容器内部留白；不要用一串空 `TextBlock` 或空 `Grid` 占位。
- **SHOULD**：使用 `MinWidth/MinHeight` 配合伸缩布局；避免把主窗口所有尺寸锁死。
- **SHOULD**：同一区域尽量遵循一致的间距系统，例如 4/8/12/16/24（设计约定，非 WPF 要求）。
- **AVOID**：在一个简单表单中反复套用 `Grid → Border → Grid → StackPanel → Grid`。
- **MUST**：考虑窗口缩放、中文长标签、DPI 和文本截断；数字字段应预留单位与异常占位符空间。

## 4. 文件拆分与模块组织

推荐目录（按项目规模增减，不要求每项都存在）：

```text
UI/
├── Views/
│   ├── MainWindow.xaml
│   ├── Pages/
│   │   └── MeasurementView.xaml
│   └── Controls/
│       ├── DeviceStatusCard.xaml
│       └── MeasurementValueCard.xaml
├── ViewModels/
│   ├── MeasurementViewModel.cs
│   └── DeviceStatusViewModel.cs
└── Resources/
    ├── Colors.xaml
    ├── Typography.xaml
    └── ControlStyles.xaml
```

拆分原则：

1. **MUST**：`MainWindow.xaml` 以页面结构、导航和区域组合为主，不容纳所有子页面的完整内容。
2. **SHOULD**：只有具备独立语义、可复用或可独立维护的区块，才提取 `UserControl`；例如“设备状态卡”“报警列表”“运动参数面板”。
3. **SHOULD**：当单个 XAML 文件超过约 **300 行**时检查能否按职责拆分；超过 **450 行**时在 Review 中说明为何保留。行数并非硬性指标。
4. **SHOULD**：若一个视觉子树嵌套超过约 **5 层**，检查是否存在无意义容器；必要的 `ControlTemplate` 和复杂表格可以例外。
5. **MUST**：不为了少十几行重复代码，制造需要跨越 4～5 个文件才能理解的泛化组件。
6. **MUST**：`UserControl` 默认继承外部 `DataContext`；不得在控件内部随意 `new` ViewModel 覆盖父级绑定。设计时数据使用 `d:DataContext` 或设计时辅助方案；确需特例时明确标注。
7. **MUST**：自定义可复用控件对外属性优先使用恰当的 `DependencyProperty`；不能在控件内部偷用全局单例读取数据。

## 5. 样式、颜色、资源与模板

- **MUST**：跨页面反复使用的颜色、字体、按钮样式，统一放入命名明确的 `ResourceDictionary`；避免大量散落的十六进制色值。
- **SHOULD**：仅本页面使用的简单样式就近放在 `UserControl.Resources`，不要为了“统一”创建只使用一次的全局样式文件。
- **SHOULD**：命名遵循“语义 + 类型”，如 `PrimaryButtonStyle`、`SectionTitleTextStyle`、`FaultBrush`。禁止 `Style1`、`NewBrush2`。
- **MUST**：只有在真正改变控件视觉树时才使用 `ControlTemplate`；调整字体、边框、间距、背景，优先普通 `Style`。
- **SHOULD**：静态主题优先 `StaticResource`；只有确需运行时资源替换时使用 `DynamicResource`。
- **MUST**：不要在每个页面复制整套按钮模板；也不要引入未安装的 MaterialDesign、MahApps、HandyControl 等依赖。
- **MUST**：资源字典合并顺序与依赖关系清晰，避免同名 Key 的意外遮蔽。
- **SHOULD**：状态颜色必须附带明确文本或图标，不能单靠红绿区分“正常/故障”。

## 6. 命名与数据绑定

- 文件名：`MeasurementView.xaml`、`MeasurementViewModel.cs`；ViewModel 属性：`MeasurementValue`、`MachineStatusText`；命令：`StartCommand`、`ResetCommand`。
- **MUST**：能直接用绑定访问的数据，不必额外设置 `x:Name`。只有代码引用、元素间绑定或关联可访问性标签时才命名元素。
- **MUST**：可读数据采用 `OneWay`（或默认已满足）；可编辑设置按需求指定 `TwoWay` 及 `UpdateSourceTrigger`。
- **MUST**：操作型按钮优先绑定 `ICommand`；`CanExecute` 或对应可用性状态负责禁用条件，禁止直接在 `Click` 处理器中调用控制器 SDK。
- **SHOULD**：多步骤、复杂条件在 ViewModel 中形成语义明确的属性；不要写让人无法读懂的深层 `MultiBinding`/`Converter` 组合。
- **MUST**：为关键测量数据定义未连接、尚未采样、无效数值时的显示策略（如 `--`），不得把无数据误显示为 `0`。
- **SHOULD**：适度使用 `FallbackValue` 和 `TargetNullValue`；如果需要区分“断连/无效/过期/真实零值”，由 ViewModel 提供显式展示文本和状态，而非只靠格式化。
- **MUST**：数值单位明确（mm、μm、℃ 等）；展示格式与设备采样原始数据分离。
- **MUST**：`DataContext` 的来源要可追踪，不能隐式依赖层层查找或随意写在 code-behind 中。

## 7. 工控桌面 UI 特别约束

- **MUST**：上位机设备服务、状态机、报警和数据采集与 WPF View 解耦；界面展示服务提供的状态，不自行决定设备安全逻辑。
- **MUST**：状态文案区分“未准备好 / 空闲 / 运行中 / 故障”，并且保留更详细的诊断原因入口（根据项目状态模型调整）。
- **MUST**：复位、启动、停止等指令必须有明确命令入口和可用性状态；**禁用按钮不等于实际安全联锁**，业务层仍应校验权限和机器状态。
- **MUST**：高频采集数据不要逐样本更新所有控件；按 UI 可接受的节奏聚合、节流刷新。刷新频率来自性能需求，不应由 AI 擅自固定。
- **MUST**：不在 UI 线程执行阻塞式设备访问、长时间数据库写入、轨迹运算和 `Thread.Sleep`。
- **SHOULD**：大量报警行或历史数据使用支持虚拟化的列表与数据模板；不要为每一行手工复制 XAML；注意不要通过不恰当的 `ScrollViewer` 包裹破坏虚拟化。
- **MUST**：涉及后台线程的集合和 UI 通知时，遵守 WPF Dispatcher 线程访问规则。
- **SHOULD**：输入项提供单位、范围、错误提示；关键命令的确认/拒绝/失败反馈必须可见。

## 8. 反例与推荐写法

### 8.1 不可接受：界面压缩、硬编码、事件混杂

```xml
<!-- 反例：难以看出行列与绑定，还把行为藏进 code-behind -->
<Grid><StackPanel Margin="12"><TextBlock Text="测量值"/><TextBlock Text="0.000"/><Button Content="启动" Click="Start_Click" Background="#1677FF"/></StackPanel></Grid>
```

### 8.2 推荐：小型完整页面（结构清晰、行为显式）

下面仅演示 **XAML 可读性**。绑定的 `MachineStatusText`、`CurrentThicknessText`、`StartCommand`、`ResetCommand` 应由外部 `DataContext` 的 ViewModel 提供。

```xml
<UserControl
    x:Class="IndustrialApp.UI.Views.Pages.MeasurementView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    d:DesignHeight="720"
    d:DesignWidth="1280">

    <UserControl.Resources>
        <Style x:Key="SectionTitleTextStyle" TargetType="TextBlock">
            <Setter Property="FontSize" Value="16" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
    </UserControl.Resources>

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <!-- 01 页面标题与机器状态 -->
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock
                DockPanel.Dock="Left"
                Text="厚度测量"
                FontSize="22"
                FontWeight="SemiBold" />

            <TextBlock
                DockPanel.Dock="Right"
                Margin="24,0,0,0"
                Text="{Binding MachineStatusText}"
                VerticalAlignment="Center" />
        </DockPanel>

        <!-- 02 实时测量数据显示 -->
        <Border
            Grid.Row="1"
            Margin="0,16,0,16"
            Padding="20"
            BorderBrush="#D0D5DD"
            BorderThickness="1"
            CornerRadius="6">

            <StackPanel>
                <TextBlock
                    Text="当前厚度"
                    Style="{StaticResource SectionTitleTextStyle}" />

                <TextBlock
                    Margin="0,16,0,0"
                    Text="{Binding CurrentThicknessText}"
                    FontSize="36"
                    FontWeight="SemiBold" />
            </StackPanel>
        </Border>

        <!-- 03 测试操作入口 -->
        <StackPanel
            Grid.Row="2"
            Orientation="Horizontal"
            HorizontalAlignment="Right">

            <Button
                MinWidth="100"
                Padding="12,6"
                Content="复位"
                Command="{Binding ResetCommand}" />

            <Button
                MinWidth="100"
                Margin="12,0,0,0"
                Padding="12,6"
                Content="开始测量"
                Command="{Binding StartCommand}" />
        </StackPanel>
    </Grid>
</UserControl>
```

阅读这个文件，应当不借助设计器就能看到：**标题与状态在顶部，实时值在中间，操作按钮在底部**。示例没有在 View 内临时创建 ViewModel，也没有假定任何设备 API。

## 9. AI 输出契约（每次生成/修改都必须遵守）

AI 在实际代码任务中须依序完成：

1. **先识别项目环境**：目标 .NET/WPF 版本、现有控件库、MVVM 框架、已存在的 Style 和命名惯例；不擅自增加 NuGet 依赖。
2. **先给简短结构草图**：列出界面区域和使用的布局容器（不超过 8 行即可），然后再写代码。
3. **列出新增/修改的文件**：说明每个文件负责哪块 UI；不为了“分层”凭空新增一堆类。
4. **生成完整可编译 XAML**：使用实际存在的 WPF 属性、命名空间、资源与绑定；若 ViewModel 不存在，明确给出需要补齐的属性/命令清单，不能假称已经实现。
5. **不破坏已有功能**：尤其是命令、绑定、资源键、DataContext、工业设备的使能或停止语义。
6. **完成后自查**：布局、绑定、命名、格式、资源、可访问性、编译和交互状态；无法执行编译时必须如实说明未验证。
7. **只描述有价值的修改**：最终回答说明改动文件、主要结构、验证结果和剩余 TODO，不需要重复粘贴整个 XAML 供人看第二遍。

### 一次性复制给 AI 的指令

```text
请严格遵循仓库中的《WPF XAML 人类可读代码生成规范（AI 编程约束）》修改界面。
目标优先级：代码易读、布局清晰、局部可维护，其次才是减少重复代码。

要求：
1. 先简要列出页面区域结构与计划修改文件，再实施。
2. 使用 WPF 原生有效属性；按“位置、尺寸、外观、内容、绑定”排序属性。
3. 4 空格缩进；超过两个属性的复杂标签分行；用注释标注主要视觉区域。
4. 不要创建没有意义的多层容器；不要把整页放进单个巨型 XAML 文件。
5. 样式适度复用，避免为一次性简单控件抽象多个资源或类。
6. UI 逻辑使用 MVVM 绑定和命令；不要把业务、设备通讯写在 code-behind。
7. 优先修改现有文件，禁止无关重构与擅自增加第三方 UI 依赖。
8. 输出可编译代码，检查所有资源键、绑定、行列索引及控件属性。
9. 明确列出哪些内容已验证、哪些因环境限制无法验证。
```

## 10. 提交前检查清单

- [ ] 文件结构符合界面阅读顺序；每个主要区域能 10 秒内定位。
- [ ] 复杂标签逐属性换行；缩进、空行、属性顺序一致。
- [ ] `Grid` 行列定义、控件位置、间距、伸缩行为正确。
- [ ] 无无意义嵌套、全屏 Canvas 绝对定位或人为制造的空控件。
- [ ] XAML 文件没有过度膨胀；拆分边界有业务语义。
- [ ] 常用样式复用；局部样式就近；无匿名重复模板和神秘资源键。
- [ ] 数据绑定的方向、更新机制、空值/失败值处理明确。
- [ ] `DataContext` 可追踪；无在 UserControl 内覆盖父级上下文的意外行为。
- [ ] 没有把设备 SDK、同步阻塞操作或安全判断直接写进 View。
- [ ] 开始/停止/复位等操作的禁用、故障和反馈行为明确。
- [ ] 界面缩放、长文字、单位和键盘/读屏标签得到考虑。
- [ ] 高频数据不会导致逐样本重绘整个页面。
- [ ] 无伪造的 WPF 原生属性、未安装依赖、失效 Style Key。
- [ ] 如果环境允许，执行构建；没有执行就明确写明“未编译验证”。

## 11. 可选：使用 XAML Styler 统一自动格式

[XAML Styler](https://github.com/Xavalon/XamlStyler) 是专门用于 XAML 格式化的社区工具。可以在仓库中建立 `Settings.XamlStyler`（JSON），使团队与 AI 产物采用统一格式。以下只演示**相关的部分配置**，其他选项可使用插件默认值。

```json
{
  "AttributesTolerance": 2,
  "KeepFirstAttributeOnSameLine": false,
  "MaxAttributesPerLine": 1,
  "AttributeIndentationStyle": 1,
  "EnableAttributeReordering": true,
  "FormatMarkupExtension": true,
  "SpaceBeforeClosingSlash": true,
  "FormatOnSave": true,
  "IndentSize": 4,
  "IndentWithTabs": false
}
```

> 注：工具统一的是**排版**，不能替代“合理拆分、正确绑定、少嵌套和视图/逻辑分离”等人工设计约束。格式化前应先确认工具版本与仓库既有配置，避免一次性改动大量无关文件。

## 12. 参考依据

官方文档与社区工具主要支持以下方向（**本文的行数阈值、属性排序偏好与 AI 输出契约是项目建议，而非官方标准**）：

1. Microsoft — [Use Automatic Layout Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/use-automatic-layout-overview)：相对布局、避免普通界面的绝对定位、适应文本和窗口变化。
2. Microsoft — [Styles and templates](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview)：Style / Template / ResourceDictionary 的职责与复用。
3. Microsoft — [Data binding overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview)：数据绑定与界面逻辑分离。
4. Microsoft — [Optimizing Performance: Layout and Design](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-layout-and-design)：布局树与测量/排列开销。
5. Microsoft — [Optimizing Performance: Controls](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-controls)：大列表、UI 虚拟化和容器回收。
6. Xavalon — [XAML Styler External Configurations](https://github.com/Xavalon/XamlStyler/wiki/External-Configurations)：属性换行、属性顺序和仓库级格式规则。
