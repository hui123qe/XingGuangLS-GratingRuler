namespace GratingRuler.Models;

public sealed record TestTask(string Id, string Name, string Category)
{
    public string DisplayName => $"{Id}  {Name}";
    public static IReadOnlyList<TestTask> All { get; } =
    [
        new("T01-S", "静态往返精度与重复性", "Static"),
        new("T01-D", "动态往返精度与重复性", "Dynamic"),
        new("T02", "Z 相零位重复性", "Edge"),
        new("T03", "单向静态测量", "Static"),
        new("T04", "静态噪声", "Noise"),
        new("T05", "速度与加速度", "Speed"),
        new("T06", "动态 Rz（待确认）", "Rz"),
        new("T07", "IO 边沿位置重复性", "Edge")
    ];
}
