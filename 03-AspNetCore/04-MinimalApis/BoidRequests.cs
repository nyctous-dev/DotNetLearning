public sealed record CreateBoidRequest(
    double X,
    double Y,
    double Speed);

public sealed record ReplaceBoidRequest(
    double X,
    double Y,
    double Speed);

public sealed record PatchBoidRequest(
    double? X,
    double? Y,
    double? Speed);