using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RPGGameManager : MonoBehaviour
{
    public static RPGGameManager Instance;

    [Header("--- ПРОКАЧКА И РЕСУРСЫ ---")]
    public string characterName = "Сборщик";
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public int skillPoints = 0;
    public int gold = 2340;

    [Header("--- РЕШИМОСТЬ И СТАТЫ ---")]
    public int baseResolveMax = 100;
    public int baseDamage = 10;
    public float maxCarryWeight = 50f;

    [Header("--- ИНВЕНТАРЬ И ЭКИПИРОВКА ---")]
    public InventoryScreenView inventoryUI;
    public List<ItemData> inventory = new List<ItemData>();

    [System.Serializable]
    public class EquipEntry { public EquipSlot slot; public ItemData item; }
    [Tooltip("Что надето на старте игры. Slot = None → слот берётся из предмета")]
    public List<EquipEntry> startingEquipment = new List<EquipEntry>();

    private readonly ItemData[] equipped = new ItemData[EquipSlotInfo.Count];

    public static event System.Action OnEquipmentChanged;

    public ItemData equippedWeapon { get { return GetEquipped(EquipSlot.Weapon); } }
    public ItemData equippedArmor  { get { return GetEquipped(EquipSlot.Jacket); } }
    public ItemData equippedHelmet { get { return GetEquipped(EquipSlot.Head); } }
    public ItemData equippedRing   { get { return GetEquipped(EquipSlot.Ring); } }

    [Header("--- КАРТА МИРА ---")]
    public Texture2D worldMapTexture;
    public Transform playerTransform;
    public Vector2 mapWorldMin = new Vector2(-110f, -110f);
    public Vector2 mapWorldMax = new Vector2(110f, 110f);

    [HideInInspector] public WorldInteractable currentInteractable;

    private bool inDialogue
    {
        get { return DialogueManager.Instance != null && DialogueManager.Instance.IsOpen; }
        set { if (!value && DialogueManager.Instance != null) DialogueManager.Instance.Close(); }
    }

    private bool IsInventoryOpen => inventoryUI != null && inventoryUI.IsOpen;
    private bool showShopWindow = false;
    private bool showWorldMap = false;

    void Awake()
    {
        Instance = this;

        foreach (var e in startingEquipment)
        {
            if (e == null || e.item == null) continue;
            EquipSlot slot = e.slot != EquipSlot.None ? e.slot : e.item.ResolveSlot();
            if (slot != EquipSlot.None) equipped[(int)slot] = e.item;
        }
    }

    void Start()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryScreenView>();
    }

    void Update()
    {
        if (currentInteractable != null && !inDialogue && !showShopWindow && !IsInventoryOpen && !showWorldMap && !QuestUI.JournalOpen)
        {
            bool ePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                ePressed = true;
#else
            if (Input.GetKeyDown(KeyCode.E))
                ePressed = true;
#endif
            if (ePressed)
            {
                OpenDialogue(currentInteractable);
            }
        }
    }

    public void ToggleInventoryUI()
    {
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryScreenView>();
        if (inventoryUI == null) return;

        showWorldMap = false;
        showShopWindow = false;
        QuestUI.CloseJournal();
        inventoryUI.Toggle();
    }

    public void SetNearbyInteractable(WorldInteractable inter)
    {
        currentInteractable = inter;
        if (inter == null)
        {
            inDialogue = false;
            showShopWindow = false;
        }
    }

    public void OpenDialogue(WorldInteractable inter)
    {
        if (inventoryUI != null) inventoryUI.Close();
        showWorldMap = false;
        QuestUI.CloseJournal();
        if (DialogueManager.Instance != null) DialogueManager.Instance.Open(inter);
    }

    public void OpenShop(WorldInteractable npc)
    {
        if (inventoryUI != null) inventoryUI.Close();
        showShopWindow = true;
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            skillPoints += 2;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.4f);
            baseResolveMax += 15;
            RecalculateStats();
            if (BattleManager.Instance != null)
                BattleManager.Instance.playerCurrentHP = BattleManager.Instance.playerMaxHP;
        }
    }

    private IEnumerable<ItemData> EquippedItems()
    {
        for (int i = 1; i < equipped.Length; i++)
            if (equipped[i] != null) yield return equipped[i];
    }

    public int GetTotalMaxResolve()
    {
        int total = baseResolveMax;
        foreach (var it in EquippedItems()) total += it.bonusHP;
        return total;
    }

    public int GetTotalDamageBonus()
    {
        int dmg = baseDamage;
        foreach (var it in EquippedItems()) dmg += it.bonusDamage;
        return dmg;
    }

    public float GetCurrentWeight()
    {
        float w = 0f;
        foreach (var item in inventory) if (item != null) w += item.weight;
        foreach (var it in EquippedItems()) w += it.weight;
        return w;
    }

    public ItemData GetEquipped(EquipSlot slot)
    {
        return slot == EquipSlot.None ? null : equipped[(int)slot];
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;

        if (item.itemType == ItemType.Consumable)
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.playerCurrentHP = Mathf.Min(
                    GetTotalMaxResolve(),
                    BattleManager.Instance.playerCurrentHP + item.healAmount
                );
            }
            inventory.Remove(item);
            if (OnEquipmentChanged != null) OnEquipmentChanged();
            return;
        }

        Equip(item);
    }

    public bool Equip(ItemData item)
    {
        if (item == null) return false;
        EquipSlot slot = item.ResolveSlot();
        if (slot == EquipSlot.None) return false;

        inventory.Remove(item);
        var old = equipped[(int)slot];
        if (old != null) inventory.Add(old);

        equipped[(int)slot] = item;
        AfterSlotChanged(slot);
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        var old = GetEquipped(slot);
        if (old == null) return false;

        equipped[(int)slot] = null;
        inventory.Add(old);
        AfterSlotChanged(slot);
        return true;
    }

    private void AfterSlotChanged(EquipSlot slot)
    {
        if (slot == EquipSlot.Weapon && BattleManager.Instance != null)
        {
            var w = equipped[(int)slot];
            BattleManager.Instance.SetPlayerWeaponSprite(w != null ? w.characterPaperdollSprite : null);
        }
        RecalculateStats();
        if (OnEquipmentChanged != null) OnEquipmentChanged();
    }

    public void RecalculateStats()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.playerMaxHP = GetTotalMaxResolve();
            if (BattleManager.Instance.playerCurrentHP > BattleManager.Instance.playerMaxHP)
                BattleManager.Instance.playerCurrentHP = BattleManager.Instance.playerMaxHP;
        }
    }

    // =========================================================================
    // КОМПАКТНЫЙ НИЖНИЙ HUD В СТИЛЕ РЕФЕРЕНСА (Старый IMGUI-инвентарь полностью удалён)
    // =========================================================================
    void OnGUI()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.battleActive) return;

        UITheme.Begin();
        GUIStyle boxStyle = UITheme.Box;
        GUIStyle btnStyle = UITheme.Btn;

        int curHP = BattleManager.Instance != null ? BattleManager.Instance.playerCurrentHP : baseResolveMax;
        int maxHP = GetTotalMaxResolve();
        int curKP = BattleManager.Instance != null ? BattleManager.Instance.playerCurrentKP : 60;
        int maxKP = BattleManager.Instance != null ? BattleManager.Instance.playerMaxKP : 60;

        // Левый нижний блок как на референсе: Уровень + полоски Решимости, Опыта и Активности
        float hudY = UITheme.H - 96f;
        GUI.Box(new Rect(18f, hudY, 64f, 80f), $"<b><size=24>{level}</size></b>\nУр.", boxStyle);

        Rect statsPanel = new Rect(86f, hudY, 240f, 80f);
        GUI.Box(statsPanel, GUIContent.none, UITheme.Panel);
        UITheme.Bar(new Rect(statsPanel.x + 8f, statsPanel.y + 8f, statsPanel.width - 16f, 20f), curHP / (float)Mathf.Max(1, maxHP), UITheme.BloodHi);
        GUI.Label(new Rect(statsPanel.x + 12f, statsPanel.y + 7f, statsPanel.width - 24f, 22f), $"Решимость: {curHP} / {maxHP}", UITheme.Small);

        UITheme.Bar(new Rect(statsPanel.x + 8f, statsPanel.y + 30f, statsPanel.width - 16f, 20f), currentXP / (float)Mathf.Max(1, xpToNextLevel), UITheme.Good);
        GUI.Label(new Rect(statsPanel.x + 12f, statsPanel.y + 29f, statsPanel.width - 24f, 22f), $"Ур. {level} XP: {currentXP} / {xpToNextLevel}", UITheme.Small);

        UITheme.Bar(new Rect(statsPanel.x + 8f, statsPanel.y + 52f, statsPanel.width - 16f, 20f), curKP / (float)Mathf.Max(1, maxKP), UITheme.Gold);
        GUI.Label(new Rect(statsPanel.x + 12f, statsPanel.y + 51f, statsPanel.width - 24f, 22f), $"Активность: {curKP} / {maxKP}", UITheme.Small);

        // Правый нижний блок как на референсе: Деньги, Инвентарь [I], Карта [M], Долги [J]
        float rightX = UITheme.W - 560f;
        GUI.Box(new Rect(rightX, hudY + 12f, 130f, 56f), $"<b><color=#{UITheme.GoldHex}>{gold}</color></b>\nруб.", boxStyle);

        if (GUI.Button(new Rect(rightX + 138f, hudY + 12f, 135f, 56f), "ИНВЕНТАРЬ [I]", IsInventoryOpen ? UITheme.BtnAccent : btnStyle))
        {
            ToggleInventoryUI();
        }
        if (GUI.Button(new Rect(rightX + 281f, hudY + 12f, 125f, 56f), "КАРТА", showWorldMap ? UITheme.BtnAccent : btnStyle))
        {
            if (inventoryUI != null) inventoryUI.Close();
            QuestUI.CloseJournal();
            showShopWindow = false;
            showWorldMap = !showWorldMap;
        }
        if (GUI.Button(new Rect(rightX + 414f, hudY + 12f, 125f, 56f), "ДОЛГИ [J]", QuestUI.JournalOpen ? UITheme.BtnAccent : btnStyle))
        {
            if (inventoryUI != null) inventoryUI.Close();
            showWorldMap = false;
            showShopWindow = false;
            QuestUI.ToggleJournal();
        }

        // Кнопка взаимодействия при подходе к NPC
        if (currentInteractable != null && !inDialogue && !showShopWindow && !IsInventoryOpen && !showWorldMap && !QuestUI.JournalOpen)
        {
            string actionIcon = "Поговорить:";
            if (currentInteractable.interactType == InteractType.ShopNPC) actionIcon = "Магазин:";
            if (currentInteractable.interactType == InteractType.EnemyNPC) actionIcon = "Бой:";

            Rect interactBtnRect = new Rect(UITheme.W - 310f, hudY - 90f, 280f, 75f);
            if (GUI.Button(interactBtnRect, $"<b>{actionIcon}</b>\n{currentInteractable.titleName} [E]", UITheme.BtnAccent))
            {
                OpenDialogue(currentInteractable);
            }
        }

        // Магазин
        if (showShopWindow && currentInteractable != null)
        {
            Rect sr = new Rect(UITheme.W * 0.5f - 420f, UITheme.H * 0.5f - 250f, 840f, 500f);
            GUI.Box(sr, $"МАГАЗИН: {currentInteractable.titleName} (Баланс: {gold} руб.)", boxStyle);

            var shopItems = currentInteractable.shopItems;
            if (shopItems != null)
            {
                for (int i = 0; i < shopItems.Length; i++)
                {
                    var item = shopItems[i];
                    if (item == null) continue;
                    if (GUI.Button(new Rect(sr.x + 30f + (i % 3) * 265f, sr.y + 55f + (i / 3) * 95f, 250f, 80f),
                        $"Купить: {item.itemName}\nЦена: {item.price} руб.\n(Урон: +{item.bonusDamage}, HP: +{item.bonusHP})", btnStyle))
                    {
                        if (gold >= item.price)
                        {
                            gold -= item.price;
                            inventory.Add(item);
                            if (OnEquipmentChanged != null) OnEquipmentChanged();
                        }
                    }
                }
            }

            if (GUI.Button(new Rect(sr.center.x - 100f, sr.yMax - 55f, 200f, 42f), "Выйти из магазина", btnStyle))
                showShopWindow = false;
        }

        // Карта
        if (showWorldMap)
        {
            Rect mapRect = new Rect(UITheme.W * 0.5f - 350f, UITheme.H * 0.5f - 250f, 700f, 480f);
            GUI.Box(mapRect, "КАРТА ГОРОДА", boxStyle);

            if (worldMapTexture != null)
                GUI.DrawTexture(new Rect(mapRect.x + 10, mapRect.y + 30, mapRect.width - 20, mapRect.height - 80), worldMapTexture, ScaleMode.StretchToFill);

            if (playerTransform != null)
            {
                float normX = Mathf.InverseLerp(mapWorldMin.x, mapWorldMax.x, playerTransform.position.x);
                float normZ = Mathf.InverseLerp(mapWorldMin.y, mapWorldMax.y, playerTransform.position.z);

                float dotX = (mapRect.x + 20) + normX * (mapRect.width - 40);
                float dotY = (mapRect.y + mapRect.height - 60) - normZ * (mapRect.height - 100);

                GUI.Button(new Rect(dotX - 45, dotY - 15, 90, 30), "ВЫ ТУТ", UITheme.BtnAccent);
            }

            if (GUI.Button(new Rect(mapRect.center.x - 80, mapRect.yMax - 45, 160, 38), "Закрыть карту", btnStyle))
                showWorldMap = false;
        }

        UITheme.End();
    }
}