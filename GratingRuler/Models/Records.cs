namespace GratingRuler.Models;

public sealed record PointRecord(int Cycle, string Direction, int Point, string Target,
    string Dut, string Reference, string Error, string Quality);

public sealed record TriggerRecord(int Cycle, string Direction, string Edge, string Boundary,
    string TriggerId, string LatchedPosition, string Timestamp, string Quality);

public sealed record SampleRecord(string Sequence, string Channel, string Timestamp,
    string Dut, string Reference, string Unit, string Quality);

public sealed record SessionRecord(string Id, DateTime StartedAt, string Batch,
    string Dut, string Task, string State, string Quality, string Verdict);

public sealed record AlarmRecord(DateTime OccurredAt, string Level, string Source,
    string Code, string Message, string State, string AcknowledgedBy);
