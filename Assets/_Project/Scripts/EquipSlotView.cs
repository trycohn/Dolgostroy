using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Один слот экипировки: иконка предмета, подпись, рамка выделения.</summary>
public class EquipSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Какой слот экипировки показывает эта ячейка")]
    public EquipSlot slot;

    public Image icon;
    public Image selectedFrame;
    public TMP_Text label;

    public ItemData Item { get; private set; }

    /// <summary>(слот, число кликов подряд: 1 — выбрать, 2 — снять)</summary>
    public event Action<EquipSlotView, int> Clicked;
    /// <summary>(слот, курсор над слотом?) — пригодится для подсказки с описанием предмета</summary>
    public event Action<EquipSlotView, bool> Hovered;

    public void SetItem(ItemData item)
    {
        Item = item;
        if (icon == null) return;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = icon.sprite != null;
    }

    public void SetSelected(bool value)
    {
        if (selectedFrame != null) selectedFrame.enabled = value;
    }

    public void RefreshLabel()
    {
        if (label != null) label.text = EquipSlotInfo.Label(slot);
    }

    public void OnPointerClick(PointerEventData e) { if (Clicked != null) Clicked(this, e.clickCount); }
    public void OnPointerEnter(PointerEventData e) { if (Hovered != null) Hovered(this, true); }
    public void OnPointerExit(PointerEventData e) { if (Hovered != null) Hovered(this, false); }
}
