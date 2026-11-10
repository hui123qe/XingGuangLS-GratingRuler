using System.Globalization;
using GratingRuler.Infrastructure;

namespace GratingRuler.Models;

public sealed class ParameterField(
    string key, string label, string unit, string value,
    double? minimum = null, bool integer = false, string[]? choices = null) : ObservableObject
{
    private string _value = value;
    private string _error = "";
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string Unit { get; } = unit;
    public double? Minimum { get; } = minimum;
    public bool Integer { get; } = integer;
    public string[] Choices { get; } = choices ?? [];
    public bool IsChoice => Choices.Length > 0;
    public string Value
    {
        get => _value;
        set { if (Set(ref _value, value)) Validate(); }
    }
    public string Error { get => _error; private set => Set(ref _error, value); }
    public double Number => double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.NaN;

    public bool Validate()
    {
        if (IsChoice)
        {
            Error = Choices.Contains(Value) ? "" : "请选择有效选项";
            return Error.Length == 0;
        }
        var number = Number;
        Error = !double.IsFinite(number) ? "请输入有效数值"
            : Integer && number != Math.Truncate(number) ? "请输入整数"
            : Minimum is { } min && number < min ? $"不得小于 {min}"
            : "";
        return Error.Length == 0;
    }
}
