using System;

namespace OutlineUi.ViewModels;

public class ConflictCheckEventArgs : EventArgs
{
    public string DocumentTitle { get; set; } = string.Empty;
    public DateTime LocalTime { get; set; }
    public DateTime ServerTime { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Action<bool> ResultHandler { get; set; } = _ => { };
}
