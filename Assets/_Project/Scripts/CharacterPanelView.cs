using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Левая часть окна инвентаря: имя, звание, кукла и 11 слотов экипировки.
/// Только отображает данные RPGGameManager и сообщает о кликах — логики боя/статов тут нет.
/// </summary>
public class CharacterPanelView : MonoBehaviour
{
    [Header("Тексты")]
    public TMP_Text nameText;
    public TMP_Text classText;
    public TMP_Text rankText;
    public string classTitle = "КОЛЛЕКТОР";
    public string rankTitle = "СИГМА НА СУЕТЕ";

    [Header("Кукла и слоты")]
    public Image doll;
    public EquipSlotView[] slots;

    /// <summary>Выбран слот (item == null не бывает: пустые слоты не выбираются). Для правой панели с описанием.</summary>
    public event Action<EquipSlot, ItemData> SlotSelected;
    /// <summary>Выбор снят.</summary>
    public event Action SelectionCleared;

    private EquipSlotView selected;

    void OnEnable()
    {
        RPGGameManager.OnEquipmentChanged += Refresh;
        if (slots != null)
            foreach (var s in slots) if (s != null) s.Clicked += OnSlotClicked;
        Refresh();
    }

    void OnDisable()
    {
        RPGGameManager.OnEquipmentChanged -= Refresh;
        if (slots != null)
            foreach (var s in slots) if (s != null) s.Clicked -= OnSlotClicked;
    }

    public void Refresh()
    {
        var rpg = RPGGameManager.Instance;

        if (nameText != null) nameText.text = rpg != null ? rpg.characterName.ToUpper() : "";
        if (classText != null) classText.text = classTitle;
        if (rankText != null) rankText.text = rankTitle;
        if (doll != null) doll.enabled = doll.sprite != null;

        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s == null) continue;
            s.RefreshLabel();
            s.SetItem(rpg != null ? rpg.GetEquipped(s.slot) : null);
        }

        // Если выбранный слот опустел (предмет сняли) — убираем выделение
        if (selected != null && selected.Item == null) ClearSelection();
    }

    public void ClearSelection()
    {
        if (selected != null) selected.SetSelected(false);
        selected = null;
        if (SelectionCleared != null) SelectionCleared();
    }

    private void OnSlotClicked(EquipSlotView view, int clicks)
    {
        if (view.Item == null) { ClearSelection(); return; }

        // Двойной клик — снять предмет обратно в инвентарь
        if (clicks >= 2)
        {
            var rpg = RPGGameManager.Instance;
            if (rpg != null) rpg.Unequip(view.slot);
            return; // Refresh вызовется через OnEquipmentChanged
        }

        if (selected != null) selected.SetSelected(false);
        selected = view;
        view.SetSelected(true);
        if (SlotSelected != null) SlotSelected(view.slot, view.Item);
    }
}
