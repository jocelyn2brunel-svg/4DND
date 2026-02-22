using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace _4DND;

public class Inventory
{
    private readonly List<string> _items = new();
    public int Capacity { get; set; } = 50;
    
    public string? EquippedWeapon { get; set; }
    public string? EquippedArmor { get; set; }
    public string? EquippedShield { get; set; }
    
    public List<string> Items => _items;
    
    public bool AddItem(string itemName)
    {
        if (_items.Count >= Capacity) return false;
        _items.Add(itemName);
        return true;
    }
    
    public bool RemoveItem(string itemName)
    {
        return _items.Remove(itemName);
    }
    
    public bool HasItem(string itemName)
    {
        return _items.Contains(itemName);
    }
    
    public int GetItemCount(string itemName)
    {
        return _items.Count(i => i == itemName);
    }
    
    public bool EquipItem(string itemName)
    {
        if (!_items.Contains(itemName)) return false;
        
        var item = ItemDatabase.GetItem(itemName);
        if (!item.IsEquippable) return false;
        
        switch (item.Type)
        {
            case ItemType.Weapon:
                EquippedWeapon = itemName;
                return true;
            case ItemType.Armor:
                EquippedArmor = itemName;
                return true;
            case ItemType.Shield:
                EquippedShield = itemName;
                return true;
            default:
                return false;
        }
    }
    
    public bool UnequipItem(string itemName)
    {
        if (EquippedWeapon == itemName)
        {
            EquippedWeapon = null;
            return true;
        }
        if (EquippedArmor == itemName)
        {
            EquippedArmor = null;
            return true;
        }
        if (EquippedShield == itemName)
        {
            EquippedShield = null;
            return true;
        }
        return false;
    }
    
    public int GetTotalWeight()
    {
        int total = 0;
        foreach (var itemName in _items)
        {
            var item = ItemDatabase.GetItem(itemName);
            total += item.Weight;
        }
        return total;
    }
    
    public int CalculateArmorClass(int baseDexModifier)
    {
        int ac = 10;
        int dexBonus = baseDexModifier;
        
        // Equipped armor
        if (EquippedArmor != null)
        {
            var armor = ItemDatabase.GetItem(EquippedArmor);
            ac = armor.ArmorClass;
            dexBonus = System.Math.Min(dexBonus, armor.MaxDexBonus);
        }
        
        ac += dexBonus;
        
        // Equipped shield
        if (EquippedShield != null)
        {
            var shield = ItemDatabase.GetItem(EquippedShield);
            ac += shield.ArmorClass;
        }
        
        return ac;
    }
}
