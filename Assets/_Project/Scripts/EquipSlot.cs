/// <summary>Слоты экипировки (11 штук, как в левом окне инвентаря).</summary>
public enum EquipSlot
{
    None = 0,
    Weapon,           // основное оружие
    SecondaryWeapon,  // вспомогательное оружие
    Head,             // головной убор
    Jacket,           // куртка
    Pants,            // штаны
    Shoes,            // обувь
    Chain,            // цепь
    Backpack,         // рюкзак
    Watch,            // часы
    Phone,            // телефон
    Ring              // кольцо
}

public static class EquipSlotInfo
{
    /// <summary>Размер массива для индексации по (int)EquipSlot (включая None).</summary>
    public const int Count = 12;

    public static string Label(EquipSlot s)
    {
        switch (s)
        {
            case EquipSlot.Weapon: return "Оружие";
            case EquipSlot.SecondaryWeapon: return "Всп. оружие";
            case EquipSlot.Head: return "Головной убор";
            case EquipSlot.Jacket: return "Куртка";
            case EquipSlot.Pants: return "Штаны";
            case EquipSlot.Shoes: return "Обувь";
            case EquipSlot.Chain: return "Цепь";
            case EquipSlot.Backpack: return "Рюкзак";
            case EquipSlot.Watch: return "Часы";
            case EquipSlot.Phone: return "Телефон";
            case EquipSlot.Ring: return "Кольцо";
            default: return "";
        }
    }
}
