using System;

namespace OutlineUi.Services;

public class ConflictResolver
{
    public enum ConflictResolution
    {
        OverwriteLocal,
        OverwriteServer,
        Skip,
        Cancel
    }

    public bool CheckConflict(DateTime? localTime, DateTime? serverTime)
    {
        if (!localTime.HasValue || !serverTime.HasValue)
            return false;
        return localTime.Value != serverTime.Value;
    }
}
