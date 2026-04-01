namespace upd8.Models.Updates;

public sealed record UpdateApplyResult(
    bool Enabled,
    bool IsInstalled,
    bool AppliedOrRestarting,
    string? Message);
