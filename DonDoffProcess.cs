#nullable enable

namespace _4DND;

public class DonDoffProcess
{
    public ItemInstance? Item { get; set; }
    public bool IsDoffing { get; set; }
    public double TotalMinutes { get; set; }
    public double MinutesRemaining { get; set; }
    public int RoundsRemaining { get; set; }
    public bool IsActive { get; set; }
    public bool HasHelp { get; set; }
}
