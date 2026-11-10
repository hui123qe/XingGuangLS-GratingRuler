using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using GratingRuler.Infrastructure;
using GratingRuler.Models;

namespace GratingRuler.ViewModels;

public sealed class ParametersViewModel : ObservableObject
{
    private static readonly string DraftPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GratingRuler", "recipe-draft.json");
    private string _dutType = "一维增量式光栅尺";
    private string _deviceId = "";
    private string _batch = "";
    private string _reference = "激光干涉仪";
    private readonly Dictionary<(string Dut, string Task), Dictionary<string, string>> _taskValues = [];
    private (string Dut, string Task)? _fieldsOwner;
    private string _feedback = "填写参数后保存或确认。";
    private TestTask _selectedTask = TestTask.All[0];
    private bool _isLocked;
    private bool _isBusy;

    public ParametersViewModel()
    {
        SaveCommand = new(async _ => await SaveAsync(), _ => CanEdit);
        RestoreCommand = new(async _ => await RestoreAsync(), _ => CanEdit);
        LockCommand = new(_ => Lock(), _ => CanEdit);
        UnlockCommand = new(_ => { IsLocked = false; Feedback = "参数已解锁。"; }, _ => IsLocked && !IsBusy);
        RefreshTasks();
    }

    public string[] DutTypes { get; } = ["一维增量式光栅尺", "二维增量式光栅尺", "绝对式光栅尺", "模拟量传感器", "IO 传感器", "激光工具胶（名称待确认）"];
    public string[] References { get; } = ["激光干涉仪", "轴反馈（需确认独立性）"];
    public ObservableCollection<TestTask> AvailableTasks { get; } = [];
    public ObservableCollection<ParameterField> MotionFields { get; } = [];
    public ObservableCollection<ParameterField> MeasurementFields { get; } = [];
    public ObservableCollection<ParameterField> PoseFields { get; } = [];
    public ObservableCollection<ParameterField> AnalysisFields { get; } = [];
    public ObservableCollection<ParameterField> CalculationFields { get; } = [];
    public bool IsReferenceCalculated => IsSpeed && MeasurementFields.Any(field => field.Key == "referenceMode" && field.Value == "基准位置计算");
    public bool HasPose => PoseFields.Count > 0;
    public bool IsSpeed => SelectedTask.Id == "T05";
    public string MotionTitle => SelectedTask.Id == "T06" ? "平移运动与动态 Rz 轨迹（待定）" : "运动参数";
    private IEnumerable<ParameterField> AllFields => MotionFields.Concat(MeasurementFields).Concat(PoseFields).Concat(AnalysisFields).Concat(CalculationFields);
    public RelayCommand SaveCommand { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand LockCommand { get; }
    public RelayCommand UnlockCommand { get; }

    public string DutType
    {
        get => _dutType;
        set { if (CanEdit && Set(ref _dutType, value)) { RefreshTasks(); NotifySummary(); } }
    }
    public string Bench => IsSpeed ? "高速测试台" : "常规测试台";
    public string DeviceId { get => _deviceId; set { if (CanEdit && Set(ref _deviceId, value)) NotifySummary(); } }
    public string Batch { get => _batch; set { if (CanEdit && Set(ref _batch, value)) NotifySummary(); } }
    public string Reference { get => _reference; set { if (CanEdit && Set(ref _reference, value)) NotifySummary(); } }
    public TestTask SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (value is null || !CanEdit || !Set(ref _selectedTask, value)) return;
            RebuildFields();
            OnPropertyChanged(nameof(TaskNote));
            OnPropertyChanged(nameof(Bench));
            OnPropertyChanged(nameof(IsSpeed));
            OnPropertyChanged(nameof(MotionTitle));
            NotifySummary();
        }
    }
    public string Feedback { get => _feedback; private set => Set(ref _feedback, value); }
    public bool IsLocked
    {
        get => _isLocked;
        private set { if (Set(ref _isLocked, value)) { UpdateCommands(); OnPropertyChanged(nameof(LockText)); } }
    }
    public bool IsBusy
    {
        get => _isBusy;
        private set { if (Set(ref _isBusy, value)) UpdateCommands(); }
    }
    public bool CanEdit => !IsLocked && !IsBusy;
    public string LockText => IsLocked ? "已锁定" : "未锁定";
    public string DeviceText => string.IsNullOrWhiteSpace(DeviceId) ? "未填写" : DeviceId;
    public string BatchText => string.IsNullOrWhiteSpace(Batch) ? "未填写" : Batch;
    public string TaskNote => SelectedTask.Id switch
    {
        "T01-D" => "动态采集须确认触发源、参考锁存和时间对齐方式。",
        "T02" => "仅适用于具有 Z 相输出的设备；需确认通道、有效边沿及位置窗口。",
        "T04" => "到位 → 设置测试状态 → 等待稳定 → 采集；断电对象待确认，状态配置不执行供断电。",
        "T05" => "被测速度、加速度由传感器自身计算输出；匀速段比较速度，加速与减速段分别比较加速度。",
        "T06" => "联动、旋转中心及评价公式待确认，此任务仅供参数设计参考。",
        "T07" => "须分别定义电气边沿和物理边界，并确认硬件锁存能力。",
        _ => "点位范围、基准独立性及评价规则须结合设备配置复核。"
    };

    private void RefreshTasks()
    {
        var previous = SelectedTask.Id;
        var allowed = TestTask.All.Where(task => IsTaskAllowed(DutType, task.Id)).ToArray();
        AvailableTasks.Clear();
        foreach (var task in allowed) AvailableTasks.Add(task);
        SelectedTask = allowed.FirstOrDefault(task => task.Id == previous) ?? allowed[0];
        if (_fieldsOwner != (DutType, SelectedTask.Id)) RebuildFields();
        // 清空选项会让 ComboBox 暂时丢失选中项，即便任务未变也要刷新绑定。
        OnPropertyChanged(nameof(SelectedTask));
    }

    private static bool IsTaskAllowed(string dutType, string taskId) => dutType switch
    {
        "IO 传感器" => taskId == "T07",
        "一维增量式光栅尺" => taskId is "T01-S" or "T01-D" or "T02" or "T03" or "T04" or "T05",
        "二维增量式光栅尺" => taskId is "T01-S" or "T01-D" or "T02" or "T03" or "T04" or "T05" or "T06",
        _ => taskId is "T01-S" or "T01-D" or "T03" or "T04" or "T05"
    };

    private void RebuildFields()
    {
        if (_fieldsOwner is { } owner)
            _taskValues[owner] = AllFields.ToDictionary(field => field.Key, field => field.Value);
        MotionFields.Clear();
        MeasurementFields.Clear();
        PoseFields.Clear();
        AnalysisFields.Clear();
        CalculationFields.Clear();
        MotionFields.Add(new("start", SelectedTask.Category == "Noise" ? "固定位置" : "起始位置", "mm", "0"));
        if (SelectedTask.Category != "Noise")
            MotionFields.Add(new("end", "终止位置", "mm", "100"));
        MotionFields.Add(new("velocity", "运动速度", "mm/s", "10", 0.001));
        MotionFields.Add(new("acceleration", "加速度", "mm/s²", "100", 0.001));
        MotionFields.Add(new("deceleration", "减速度", "mm/s²", "100", 0.001));
        if (SelectedTask.Id != "T03")
            MeasurementFields.Add(new("cycles", "循环次数", "次", "3", 1, true));
        switch (SelectedTask.Category)
        {
            case "Static":
                MeasurementFields.Add(new("spacing", "点位间隔", "mm", "10", 0.001));
                MeasurementFields.Add(new("settle", "稳定时间", "s", "1", 0));
                MeasurementFields.Add(new("samples", "每点样本数", "个", "10", 1, true));
                break;
            case "Edge":
                MeasurementFields.Add(new("sampleRate", SelectedTask.Id == "T07" ? "采集频率" : "采样频率", "Hz", "1000", 1));
                MeasurementFields.Add(new("windowStart", "窗口起点", "mm", "0"));
                MeasurementFields.Add(new("windowEnd", "窗口终点", "mm", "100"));
                break;
            case "Dynamic":
                MeasurementFields.Add(new("triggerSpacing", "触发间隔", "mm", "1", 0.001));
                break;
            default:
                MeasurementFields.Add(new("sampleRate", "采样频率", "Hz", "1000", 1));
                if (SelectedTask.Category == "Noise")
                {
                    MeasurementFields.Add(new("powerState", "测试状态", "", "上电", choices: ["上电", "断电"]));
                    MeasurementFields.Add(new("settle", "稳定时长", "s", "1", 0));
                    MeasurementFields.Add(new("duration", "采集时长", "s", "10", 0.001));
                }
                break;
        }
        if (SelectedTask.Id == "T06")
        {
            PoseFields.Add(new("poseZ", "准备 Z 位置", "mm", "0"));
            PoseFields.Add(new("poseRx", "准备 Rx 角度", "°", "0"));
            PoseFields.Add(new("poseRy", "准备 Ry 角度", "°", "0"));
            PoseFields.Add(new("poseSettle", "姿态稳定时长", "s", "1", 0));
            MotionFields.Add(new("rzStart", "Rz 起始角度", "°", "0"));
            MotionFields.Add(new("rzEnd", "Rz 终止角度", "°", "1"));
            MotionFields.Add(new("rzVelocity", "Rz 角速度", "°/s", "1", 0.001));
            MotionFields.Add(new("rzAcceleration", "Rz 角加速度", "°/s²", "1", 0.001));
            MotionFields.Add(new("rzDeceleration", "Rz 角减速度", "°/s²", "1", 0.001));
        }
        if (IsSpeed)
        {
            MeasurementFields.Add(new("measurementAxis", "测量轴", "", "X", choices: DutType == "二维增量式光栅尺" ? ["X", "Y"] : ["X"]));
            var referenceMode = new ParameterField("referenceMode", "基准数据来源", "", "基准直接输出", choices: ["基准直接输出", "基准位置计算"]);
            referenceMode.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ParameterField.Value)) OnPropertyChanged(nameof(IsReferenceCalculated));
            };
            MeasurementFields.Add(referenceMode);
            AnalysisFields.Add(new("timeOffset", "被测时间修正", "ms", "0"));
            AnalysisFields.Add(new("trimDuration", "过渡剔除时长", "s", "0", 0));
            AnalysisFields.Add(new("velocityLimit", "速度误差限值", "mm/s", "", 0));
            AnalysisFields.Add(new("accelerationLimit", "加速段误差限值", "mm/s²", "", 0));
            AnalysisFields.Add(new("decelerationLimit", "减速段误差限值", "mm/s²", "", 0));
            CalculationFields.Add(new("differenceWindow", "差分窗口点数", "点", "3", 3, true));
            CalculationFields.Add(new("filterWindow", "平滑窗口点数", "点", "1", 1, true));
        }
        _fieldsOwner = (DutType, SelectedTask.Id);
        if (_taskValues.TryGetValue(_fieldsOwner.Value, out var values))
            foreach (var field in AllFields)
                if (values.TryGetValue(field.Key, out var value)) field.Value = value;
        OnPropertyChanged(nameof(HasPose));
        OnPropertyChanged(nameof(IsReferenceCalculated));
    }

    private bool Validate()
    {
        var fields = AllFields.ToArray();
        var valid = true;
        foreach (var field in fields.Where(field => IsReferenceCalculated || !CalculationFields.Contains(field)))
            valid &= field.Validate();
        if (!valid) { Feedback = "参数格式有误，请检查标红项。"; return false; }
        if (string.IsNullOrWhiteSpace(DeviceId)) { Feedback = "请填写设备编号。"; return false; }
        var start = fields.First(field => field.Key == "start").Number;
        var end = fields.FirstOrDefault(field => field.Key == "end")?.Number;
        if (end == start) { Feedback = "起始位置和终止位置不能相同。"; return false; }
        var spacing = fields.FirstOrDefault(field => field.Key == "spacing")?.Number;
        if (end.HasValue && spacing.HasValue && spacing > Math.Abs(end.Value - start))
        { Feedback = "点位间隔不能大于测试行程。"; return false; }
        var windowStart = fields.FirstOrDefault(field => field.Key == "windowStart")?.Number;
        var windowEnd = fields.FirstOrDefault(field => field.Key == "windowEnd")?.Number;
        if (windowStart.HasValue && windowEnd <= windowStart)
        { Feedback = "窗口终点必须大于窗口起点。"; return false; }
        var triggerSpacing = fields.FirstOrDefault(field => field.Key == "triggerSpacing")?.Number;
        if (end.HasValue && triggerSpacing > Math.Abs(end.Value - start))
        { Feedback = "触发间隔不能大于测试行程。"; return false; }
        if (IsReferenceCalculated && CalculationFields.Any(field => field.Number % 2 == 0))
        { Feedback = "差分和平滑窗口点数必须为奇数；平滑窗口为 1 时不滤波。"; return false; }
        return true;
    }

    private void Lock()
    {
        if (!Validate()) return;
        if (SelectedTask.Id == "T06") { Feedback = "动态 Rz 方案待确认，不能锁定。"; return; }
        IsLocked = true;
        Feedback = "参数已锁定。设备接入后仍须校验行程、安全链和采集条件。";
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var draft = new RecipeDraft(2, DutType, DeviceId, Batch, Reference, Bench, SelectedTask.Id,
                AllFields.ToDictionary(field => field.Key, field => field.Value));
            Directory.CreateDirectory(Path.GetDirectoryName(DraftPath)!);
            var temporaryPath = DraftPath + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, DraftPath, true);
            Feedback = "草稿已保存。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Feedback = $"保存失败：{ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task RestoreAsync()
    {
        IsBusy = true;
        try
        {
            if (!File.Exists(DraftPath)) { Feedback = "没有已保存的草稿。"; return; }
            var draft = JsonSerializer.Deserialize<RecipeDraft>(await File.ReadAllTextAsync(DraftPath));
            if (draft is null || draft.Version is not (1 or 2) || !DutTypes.Contains(draft.DutType)
                || !References.Contains(draft.Reference)
                || !IsTaskAllowed(draft.DutType, draft.TaskId)
                || draft.Fields is null || draft.DeviceId is null || draft.Batch is null)
            { Feedback = "草稿格式不受支持。"; return; }
            // 通过属性更新恢复界面联动；恢复不自动锁定或触发设备操作。
            IsBusy = false;
            DutType = draft.DutType;
            DeviceId = draft.DeviceId;
            Batch = draft.Batch;
            Reference = draft.Reference;
            SelectedTask = AvailableTasks.First(task => task.Id == draft.TaskId);
            // 恢复前清除本项目缓存，旧版缺少的字段采用新默认值。
            _taskValues.Remove((DutType, SelectedTask.Id));
            _fieldsOwner = null;
            RebuildFields();
            foreach (var field in AllFields)
                if (draft.Fields.TryGetValue(field.Key, out var value)) field.Value = value ?? "";
            Feedback = "已恢复草稿，请核对后确认。";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { Feedback = $"读取失败：{ex.Message}"; }
        finally { IsBusy = false; }
    }

    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(CanEdit));
        SaveCommand.RaiseCanExecuteChanged();
        RestoreCommand.RaiseCanExecuteChanged();
        LockCommand.RaiseCanExecuteChanged();
        UnlockCommand.RaiseCanExecuteChanged();
    }
    private void NotifySummary()
    {
        OnPropertyChanged(nameof(DeviceText));
        OnPropertyChanged(nameof(BatchText));
    }

    private sealed record RecipeDraft(int Version, string DutType, string DeviceId, string Batch,
        string Reference, string Bench, string TaskId, Dictionary<string, string> Fields);
}
