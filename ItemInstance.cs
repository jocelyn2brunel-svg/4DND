#nullable enable

namespace _4DND;

public class ItemInstance
{
    public string Name { get; set; } = "";
    public int RemainingMinutes { get; set; } = 0;
    public bool IsLit { get; set; } = false;
    /// <summary>
    /// Remaining units for ammunition items (e.g. 20 for a full pack of arrows).
    /// 0 for non-ammunition items.
    /// </summary>
    public int Quantity { get; set; } = 0;

    public ItemInstance() { }

    public ItemInstance(string name)
    {
        Name = name;
        if (name == "Torch")
        {
            RemainingMinutes = 60;
        }
        var itemData = ItemDatabase.GetItem(name);
        if (itemData.DefaultQuantity > 0)
            Quantity = itemData.DefaultQuantity;
    }

    public override string ToString() => Name;
}
