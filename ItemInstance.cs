#nullable enable

namespace _4DND;

public class ItemInstance
{
    public string Name { get; set; } = "";
    public int RemainingMinutes { get; set; } = 0;
    public bool IsLit { get; set; } = false;

    public ItemInstance() { }

    public ItemInstance(string name)
    {
        Name = name;
        if (name == "Torch")
        {
            RemainingMinutes = 60;
        }
    }

    public override string ToString() => Name;
}
