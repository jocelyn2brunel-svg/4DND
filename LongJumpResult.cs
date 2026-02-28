#nullable enable

namespace _4DND;

public class LongJumpResult
{
    public Creature Creature { get; set; } = null!;
    public int DistanceFt { get; set; }
    public int MovementSpentFt { get; set; }
    public bool ClearedObstacle { get; set; }
    public bool LandedOnFeet { get; set; }
    public bool HasRunningStart { get; set; }
    public int AthleticsRoll { get; set; }
    public int AcrobaticsRoll { get; set; }
}
