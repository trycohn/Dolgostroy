using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryScreenView : MonoBehaviour
{
    public static InventoryScreenView Instance;

    [Header("Главный контейнер окна")]
    public GameObject windowRoot;
    public Button closeButton;

    [Header("Левое окно (Кукла и 11 слотов)")]
    public CharacterPanelView characterPanel;

    [Header("Среднее окно (Фильтры, Поиск, Сетка 5x5, Вес)")]
    public Button[] filterButtons;
    public TMP_InputField searchInput;
    public EquipSlotView[] bagSlots;
    public TMP_Text[] bagCountLabels;
    public Image weightFillBar;
    public TMP_Text weightValueText;

    [Header("Правое окно (Детали предмета и Кнопки)")]
    public GameObject emptyStateObject;
    public GameObject detailsContent;
    public Image detailIcon;
    public TMP_Text detailTitle;
    public TMP_Text detailSubtitle;
    public TMP_Text detailDescription;
    public TMP_Text detailStats;
    public Button btnEquip;
    public TMP_Text btnEquipLabel;
    public Button btnUse;
    public Button btnDrop;
    public Button btnCompare;

    private int activeFilter = 0;
    private string searchQuery = "";
    private float weightBarMaxWidth = -1f;

    private struct StackedItem
    {
        public ItemData item;
        public int count;
    }
    private readonly List<StackedItem> displayedStacks = new List<StackedItem>();

    private ItemData selectedItem;
    private bool selectedFromEquipped;
    private EquipSlot selectedEquippedSlot;
    private int selectedBagIndex = -1;
    private bool compareMode = false;

    private static readonly Color TabActiveColor = new Color(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color TabNormalColor = new Color(0.20f, 0.22f, 0.27f, 1f);

    public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

    void Awake()
    {
        Instance = this;
        if (weightFillBar != null)
        {
            var rt = weightFillBar.rectTransform;
            weightBarMaxWidth = rt.sizeDelta.x;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(-weightBarMaxWidth * 0.5f, 0f);
        }
        BindEvents();
        if (windowRoot != null) windowRoot.SetActive(false);
    }

    private void BindEvents()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (filterButtons != null)
        {
            for (int i = 0; i < filterButtons.Length; i++)
            {
                int idx = i;
                if (filterButtons[i] != null)
                    filterButtons[i].onClick.AddListener(() => SetFilter(idx));
            }
        }

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(q => { searchQuery = q; RefreshBag(); RefreshDetails(); });

        if (bagSlots != null)
        {
            for (int i = 0; i < bagSlots.Length; i++)
            {
                int idx = i;
                if (bagSlots[i] != null)
                    bagSlots[i].Clicked += (view, clicks) => OnBagSlotClicked(idx, clicks);
            }
        }

        if (characterPanel != null)
        {
            characterPanel.SlotSelected += OnEquippedSlotSelected;
            characterPanel.SelectionCleared += OnEquippedSelectionCleared;
        }

        if (btnEquip != null) btnEquip.onClick.AddListener(ActionEquipOrUnequip);
        if (btnUse != null) btnUse.onClick.AddListener(ActionUse);
        if (btnDrop != null) btnDrop.onClick.AddListener(ActionDrop);
        if (btnCompare != null) btnCompare.onClick.AddListener(ActionToggleCompare);
    }

    void OnEnable() { RPGGameManager.OnEquipmentChanged += RefreshAll; }
    void OnDisable() { RPGGameManager.OnEquipmentChanged -= RefreshAll; }

    void Update()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.battleActive)
        {
            if (IsOpen) Close();
            return;
        }

        bool toggleKey = false, escKey = false, eKey = false, rKey = false, xKey = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            toggleKey = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame;
            escKey = Keyboard.current.escapeKey.wasPressedThisFrame;
            eKey = Keyboard.current.eKey.wasPressedThisFrame;
            rKey = Keyboard.current.rKey.wasPressedThisFrame;
            xKey = Keyboard.current.xKey.wasPressedThisFrame;
        }
#else
        toggleKey = Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
        escKey = Input.GetKeyDown(KeyCode.Escape);
        eKey = Input.GetKeyDown(KeyCode.E);
        rKey = Input.GetKeyDown(KeyCode.R);
        xKey = Input.GetKeyDown(KeyCode.X);
#endif

        bool typing = searchInput != null && searchInput.isFocused;

        if (!typing && toggleKey && (DialogueManager.Instance == null || !DialogueManager.Instance.IsOpen))
        {
            Toggle();
            return;
        }

        if (!IsOpen || typing) return;

        if (escKey) Close();
        else if (eKey && selectedItem != null) ActionEquipOrUnequip();
        else if (rKey && selectedItem != null) ActionToggleCompare();
        else if (xKey && selectedItem != null && !selectedFromEquipped) ActionDrop();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (windowRoot == null) return;
        QuestUI.CloseJournal();
        windowRoot.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        if (windowRoot == null) return;
        windowRoot.SetActive(false);
    }

    public void SetFilter(int index)
    {
        activeFilter = index;
        if (filterButtons != null)
        {
            for (int i = 0; i < filterButtons.Length; i++)
            {
                if (filterButtons[i] == null) continue;
                var img = filterButtons[i].GetComponent<Image>();
                if (img != null) img.color = (i == activeFilter) ? TabActiveColor : TabNormalColor;
            }
        }
        RefreshBag();
        RefreshDetails();
    }

    public void RefreshAll()
    {
        if (!IsOpen) return;
        if (characterPanel != null) characterPanel.Refresh();
        RefreshBag();
        RefreshWeight();
        RefreshDetails();
    }

    private bool PassesFilter(ItemData item)
    {
        if (item == null) return false;

        if (!string.IsNullOrEmpty(searchQuery))
        {
            if (string.IsNullOrEmpty(item.itemName) ||
                item.itemName.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        switch (activeFilter)
        {
            case 1: return item.ResolveSlot() != EquipSlot.None;
            case 2: return item.itemType == ItemType.Consumable;
            case 3: return item.ResolveSlot() == EquipSlot.None && item.itemType != ItemType.Consumable;
            case 4: return false;
            default: return true;
        }
    }

    public void RefreshBag()
    {
        displayedStacks.Clear();
        var rpg = RPGGameManager.Instance;

        if (rpg != null && rpg.inventory != null)
        {
            foreach (var it in rpg.inventory)
            {
                if (!PassesFilter(it)) continue;
                int found = -1;
                for (int i = 0; i < displayedStacks.Count; i++)
                {
                    if (displayedStacks[i].item == it) { found = i; break; }
                }
                if (found >= 0)
                {
                    var st = displayedStacks[found];
                    st.count++;
                    displayedStacks[found] = st;
                }
                else
                {
                    displayedStacks.Add(new StackedItem { item = it, count = 1 });
                }
            }
        }

        if (bagSlots != null)
        {
            for (int i = 0; i < bagSlots.Length; i++)
            {
                if (bagSlots[i] == null) continue;
                if (i < displayedStacks.Count)
                {
                    var st = displayedStacks[i];
                    bagSlots[i].SetItem(st.item);
                    bagSlots[i].SetSelected(!selectedFromEquipped && selectedBagIndex == i);
                    if (bagCountLabels != null && i < bagCountLabels.Length && bagCountLabels[i] != null)
                        bagCountLabels[i].text = st.count > 1 ? "x" + st.count : "x1";
                }
                else
                {
                    bagSlots[i].SetItem(null);
                    bagSlots[i].SetSelected(false);
                    if (bagCountLabels != null && i < bagCountLabels.Length && bagCountLabels[i] != null)
                        bagCountLabels[i].text = "";
                }
            }
        }

        if (selectedItem == null && displayedStacks.Count > 0)
        {
            SelectBagSlot(0);
        }
        else if (!selectedFromEquipped)
        {
            if (selectedBagIndex < 0 || selectedBagIndex >= displayedStacks.Count)
            {
                selectedItem = null;
                selectedBagIndex = -1;
            }
            else
            {
                selectedItem = displayedStacks[selectedBagIndex].item;
            }
        }
    }

    private void RefreshWeight()
    {
        var rpg = RPGGameManager.Instance;
        float cur = rpg != null ? rpg.GetCurrentWeight() : 0f;
        float max = rpg != null ? Mathf.Max(1f, rpg.maxCarryWeight) : 50f;
        float ratio = Mathf.Clamp01(cur / max);

        if (weightFillBar != null)
        {
            var rt = weightFillBar.rectTransform;
            if (weightBarMaxWidth < 0f) weightBarMaxWidth = 354f;
            rt.sizeDelta = new Vector2(weightBarMaxWidth * ratio, rt.sizeDelta.y);
        }
        if (weightValueText != null) weightValueText.text = $"{cur:F1} / {max:F0} кг";
    }

    private void OnBagSlotClicked(int index, int clicks)
    {
        if (index < 0 || index >= displayedStacks.Count) return;

        SelectBagSlot(index);

        if (clicks >= 2 && selectedItem != null)
        {
            if (selectedItem.itemType == ItemType.Consumable) ActionUse();
            else ActionEquipOrUnequip();
        }
    }

    private void SelectBagSlot(int index)
    {
        if (characterPanel != null) characterPanel.ClearSelection();

        selectedFromEquipped = false;
        selectedBagIndex = index;
        selectedItem = displayedStacks[index].item;

        if (bagSlots != null)
        {
            for (int i = 0; i < bagSlots.Length; i++)
                if (bagSlots[i] != null) bagSlots[i].SetSelected(i == index);
        }

        RefreshDetails();
    }

    private void OnEquippedSlotSelected(EquipSlot slot, ItemData item)
    {
        selectedFromEquipped = true;
        selectedEquippedSlot = slot;
        selectedItem = item;
        selectedBagIndex = -1;

        if (bagSlots != null)
            foreach (var b in bagSlots) if (b != null) b.SetSelected(false);

        RefreshDetails();
    }

    private void OnEquippedSelectionCleared()
    {
        if (selectedFromEquipped)
        {
            selectedFromEquipped = false;
            selectedItem = null;
            RefreshDetails();
        }
    }

    private void RefreshDetails()
    {
        bool hasItem = selectedItem != null;
        if (detailsContent != null) detailsContent.SetActive(hasItem);
        if (emptyStateObject != null) emptyStateObject.SetActive(!hasItem);
        if (!hasItem) return;

        if (detailIcon != null)
        {
            detailIcon.sprite = selectedItem.icon;
            detailIcon.enabled = selectedItem.icon != null;
        }

        if (detailTitle != null)
            detailTitle.text = selectedItem.itemName.ToUpper();

        EquipSlot targetSlot = selectedItem.ResolveSlot();
        if (detailSubtitle != null)
        {
            string slotName = targetSlot != EquipSlot.None ? EquipSlotInfo.Label(targetSlot) : "Расходник / Предмет";
            detailSubtitle.text = $"<color=#E69138>(Обычное)</color>\n{slotName}";
        }

        if (detailDescription != null)
        {
            detailDescription.text = string.IsNullOrEmpty(selectedItem.description)
                ? "Проверенный инструмент. Незаменимая вещь в работе сборщика."
                : selectedItem.description;
        }

        if (detailStats != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=#C9A44D><b>ХАРАКТЕРИСТИКИ</b></color>");
            if (selectedItem.bonusDamage != 0) sb.AppendLine($"Урон: +{selectedItem.bonusDamage}");
            if (selectedItem.bonusHP != 0) sb.AppendLine($"Решимость: +{selectedItem.bonusHP}");
            if (selectedItem.bonusMaxKP != 0) sb.AppendLine($"Активность: +{selectedItem.bonusMaxKP}");
            if (selectedItem.healAmount != 0) sb.AppendLine($"Лечение: +{selectedItem.healAmount} HP");
            sb.AppendLine($"Вес: {selectedItem.weight:F1} кг");
            sb.AppendLine($"Цена: {selectedItem.price} руб.");

            if (compareMode && !selectedFromEquipped && targetSlot != EquipSlot.None && RPGGameManager.Instance != null)
            {
                var eq = RPGGameManager.Instance.GetEquipped(targetSlot);
                int diffDmg = selectedItem.bonusDamage - (eq != null ? eq.bonusDamage : 0);
                int diffHp = selectedItem.bonusHP - (eq != null ? eq.bonusHP : 0);
                sb.AppendLine($"<color=#9E8F8F>Сравнение ({(eq != null ? eq.itemName : "Пусто")}): Урон {FormatDiff(diffDmg)}, HP {FormatDiff(diffHp)}</color>");
            }

            detailStats.text = sb.ToString();
        }

        bool canEquip = targetSlot != EquipSlot.None;
        bool canUse = selectedItem.itemType == ItemType.Consumable;

        if (btnEquip != null) btnEquip.interactable = canEquip || selectedFromEquipped;
        if (btnEquipLabel != null) btnEquipLabel.text = selectedFromEquipped ? "СНЯТЬ" : "ЭКИПИРОВАТЬ";
        if (btnUse != null) btnUse.interactable = canUse && !selectedFromEquipped;
        if (btnDrop != null) btnDrop.interactable = !selectedFromEquipped;
        if (btnCompare != null) btnCompare.interactable = canEquip && !selectedFromEquipped;
    }

    private static string FormatDiff(int d)
    {
        if (d > 0) return $"<color=#8CCB6E>+{d}</color>";
        if (d < 0) return $"<color=#E05A5A>{d}</color>";
        return "0";
    }

    private void ActionEquipOrUnequip()
    {
        var rpg = RPGGameManager.Instance;
        if (rpg == null || selectedItem == null) return;

        if (selectedFromEquipped)
        {
            rpg.Unequip(selectedEquippedSlot);
            selectedFromEquipped = false;
            selectedItem = null;
        }
        else if (selectedItem.ResolveSlot() != EquipSlot.None)
        {
            rpg.Equip(selectedItem);
        }
        RefreshAll();
    }

    private void ActionUse()
    {
        var rpg = RPGGameManager.Instance;
        if (rpg == null || selectedItem == null || selectedFromEquipped) return;
        if (selectedItem.itemType == ItemType.Consumable)
        {
            rpg.EquipItem(selectedItem);
            RefreshAll();
        }
    }

    private void ActionDrop()
    {
        var rpg = RPGGameManager.Instance;
        if (rpg == null || selectedItem == null || selectedFromEquipped) return;
        rpg.inventory.Remove(selectedItem);
        RefreshAll();
    }

    private void ActionToggleCompare()
    {
        compareMode = !compareMode;
        RefreshDetails();
    }
}