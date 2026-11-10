using System.Collections.ObjectModel;
using System.Windows.Threading;
using GratingRuler.Infrastructure;
using GratingRuler.Models;

namespace GratingRuler.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _clock;
    private string _selectedPage = "测试工作台";
    private string _operationFeedback = "设备未连接";
    private string _clockText = "";
    private string _queryText = "";
    private string _recordFeedback = "共 0 条";
    private DateTime? _fromDate = DateTime.Today.AddDays(-7);
    private DateTime? _toDate = DateTime.Today;

    public MainViewModel()
    {
        Parameters.PropertyChanged += ParametersChanged;
        OpenParametersCommand = new(_ => SelectedPage = "参数设置");
        // 未接入设备服务时，运动命令不可用；不能用界面状态模拟硬件反馈。
        UnavailableDeviceCommand = new(_ => OperationFeedback = "设备服务未接入，操作未执行。", _ => false);
        EmergencyStopCommand = new(_ => OperationFeedback = "软件急停未发送：设备服务未接入。请使用设备硬件急停。" );
        QueryRecordsCommand = new(_ =>
        {
            RecordFeedback = FromDate.HasValue && ToDate.HasValue && FromDate > ToDate
                ? "开始日期不能晚于结束日期。"
                : "共 0 条；尚无测试记录。";
        });
        ClearRecordFilterCommand = new(_ =>
        {
            QueryText = "";
            FromDate = DateTime.Today.AddDays(-7);
            ToDate = DateTime.Today;
            RecordFeedback = "共 0 条";
        });
        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += UpdateClock;
        UpdateClock(null, EventArgs.Empty);
        _clock.Start();
    }

    public string[] Pages { get; } = ["测试工作台", "参数设置", "维护界面", "运行记录", "报警记录"];
    public ParametersViewModel Parameters { get; } = new();
    public ObservableCollection<PointRecord> Points { get; } = [];
    public ObservableCollection<TriggerRecord> TriggerEvents { get; } = [];
    public ObservableCollection<SampleRecord> Samples { get; } = [];
    public ObservableCollection<SessionRecord> Sessions { get; } = [];
    public ObservableCollection<AlarmRecord> CurrentAlarms { get; } = [];
    public ObservableCollection<AlarmRecord> HistoryAlarms { get; } = [];
    public string SelectedPage { get => _selectedPage; set => Set(ref _selectedPage, value); }
    public string OperationFeedback { get => _operationFeedback; private set => Set(ref _operationFeedback, value); }
    public string ClockText { get => _clockText; private set => Set(ref _clockText, value); }
    public string QueryText { get => _queryText; set => Set(ref _queryText, value); }
    public DateTime? FromDate { get => _fromDate; set => Set(ref _fromDate, value); }
    public DateTime? ToDate { get => _toDate; set => Set(ref _toDate, value); }
    public string RecordFeedback { get => _recordFeedback; private set => Set(ref _recordFeedback, value); }
    public string RecordTitle => Parameters.SelectedTask.Category switch
    {
        "Static" => "点位记录",
        "Edge" => "触发事件",
        _ => "采样记录"
    };
    public string PrimaryReadingLabel => Parameters.DutType switch
    {
        "IO 传感器" => "输入电平",
        "模拟量传感器" => "DUT 工程值 / 单位待配置",
        "激光工具胶（名称待确认）" => "DUT 测量值 / 单位待配置",
        "二维增量式光栅尺" => "DUT X / Y 位置 / mm",
        _ => "DUT 位置 / mm"
    };
    public string SecondaryReadingLabel => Parameters.SelectedTask.Category == "Edge"
        ? "锁存基准位置 / mm" : "基准位置 / mm";
    public string ResultReadingLabel => Parameters.SelectedTask.Category switch
    {
        "Edge" => "有效事件 / 个",
        "Noise" => "峰峰值 / 通道单位",
        "Speed" => "速度误差 / mm/s",
        _ => "误差 / μm"
    };
    public string CurveCaption => Parameters.SelectedTask.Category switch
    {
        "Edge" => "循环 — 锁存基准位置 / mm",
        "Noise" => "时间 / s — 测量值 / 通道单位",
        "Speed" => "时间 / s — 速度 / mm/s；加速度 / mm/s²",
        "Rz" => "动态 Rz 方案待确认",
        _ => "基准位置 / mm — 误差 / μm"
    };
    public string ResultSummary => Parameters.SelectedTask.Category switch
    {
        "Edge" => "位置极差：-- mm　　缺失事件：--　　判定：未判定",
        "Noise" => "峰峰值：--　　标准差：--　　RMS：--　　判定：未判定",
        "Speed" => "匀速段速度误差：--　　加速段误差：--　　减速段误差：--　　判定：未判定",
        _ => "最大绝对误差：-- μm　　误差极差：-- μm　　判定：未判定"
    };
    public RelayCommand OpenParametersCommand { get; }
    public RelayCommand UnavailableDeviceCommand { get; }
    public RelayCommand EmergencyStopCommand { get; }
    public RelayCommand QueryRecordsCommand { get; }
    public RelayCommand ClearRecordFilterCommand { get; }

    private void UpdateClock(object? sender, EventArgs e) => ClockText = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
    private void ParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ParametersViewModel.SelectedTask) or nameof(ParametersViewModel.DutType))) return;
        OnPropertyChanged(nameof(RecordTitle));
        OnPropertyChanged(nameof(PrimaryReadingLabel));
        OnPropertyChanged(nameof(SecondaryReadingLabel));
        OnPropertyChanged(nameof(ResultReadingLabel));
        OnPropertyChanged(nameof(CurveCaption));
        OnPropertyChanged(nameof(ResultSummary));
    }
    public void Dispose()
    {
        Parameters.PropertyChanged -= ParametersChanged;
        _clock.Stop();
        _clock.Tick -= UpdateClock;
    }
}
