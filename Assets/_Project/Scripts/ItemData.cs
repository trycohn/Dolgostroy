using UnityEngine;

public enum ItemType { Weapon, Armor, Helmet, Boots, Ring, Consumable }

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "Железный меч";
    [TextArea(2, 5)] public string description;
    public Sprite icon;                 // 2D-иконка для сетки инвентаря и магазина
    public Sprite characterPaperdollSprite; // 2D-картинка на кукле персонажа (и в 2D-бою!)
    public ItemType itemType = ItemType.Weapon;

    [Header("Экипировка")]
    [Tooltip("В какой слот одевается. None = определить по itemType (так работают старые предметы)")]
    public EquipSlot equipSlot = EquipSlot.None;

    [Header("Цена и Вес")]
    public int price = 100;
    public float weight = 2.5f;

    [Header("Бонусы к характеристикам")]
    public int bonusDamage = 5;
    public int bonusHP = 0;             // Бонус к Решимости/HP
    public int bonusMaxKP = 0;          // Бонус к Концентрации (Focus)
    public int healAmount = 0;          // Если это аптечка/расходник

    /// <summary>Слот, в который можно надеть предмет (None — предмет не экипируется).</summary>
    public EquipSlot ResolveSlot()
    {
        if (equipSlot != EquipSlot.None) return equipSlot;
        switch (itemType)
        {
            case ItemType.Weapon: return EquipSlot.Weapon;
            case ItemType.Armor: return EquipSlot.Jacket;
            case ItemType.Helmet: return EquipSlot.Head;
            case ItemType.Boots: return EquipSlot.Shoes;
            case ItemType.Ring: return EquipSlot.Ring;
            default: return EquipSlot.None;
        }
    }
}
