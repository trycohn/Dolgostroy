using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyDifficulty { Easy, Medium, Hard }
public enum BattleCategory { Control = 0, Instinct = 1, Weapons = 2 }

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("--- ССЫЛКИ НА 3D МИР (Заморозка на время боя) ---")]
    public IsoPlayerController worldPlayer;
    public GameObject worldMapObject;

    [Header("--- 2D ЭКРАН БОЯ (Canvas) ---")]
    public GameObject battleScreen2D;
    public Image backgroundImage2D;
    public Image playerImage2D;
    public Image playerWeaponImage2D;
    public Image enemyImage2D;

    [Header("--- ВЕРХНИЕ БАРЫ (Игрок слева, Враг справа) ---")]
    public Image playerHpFill;
    public TMP_Text playerHpText;
    public TMP_Text playerNameText;
    public Image playerFocusFill;
    public TMP_Text playerFocusText;

    public Image enemyHpFill;
    public TMP_Text enemyHpText;
    public TMP_Text enemyNameText;
    public TMP_Text roundBannerText;
    public TMP_Text floatingLogText;

    [Header("--- НИЖНЯЯ ПАНЕЛЬ (Категории, 4 Карточки, Очередь, GO/SKIP) ---")]
    public Button btnTabControl;
    public Button btnTabInstinct;
    public Button btnTabWeapons;
    public TMP_Text headerLeftLabel;
    public TMP_Text headerRightLabel;

    public Button[] cardButtons = new Button[4];
    public Image[] cardBgs = new Image[4];
    public TMP_Text[] cardTitles = new TMP_Text[4];
    public TMP_Text[] cardEffects = new TMP_Text[4];
    public TMP_Text[] cardCosts = new TMP_Text[4];

    public GameObject[] queueRowObjects = new GameObject[4];
    public Image[] queueRowBgs = new Image[4];
    public TMP_Text[] queueRowTexts = new TMP_Text[4];
    public Button[] queueCancelButtons = new Button[4];

    public Button btnMenuFlee;
    public Button btnGoOrSkip;
    public TMP_Text btnGoOrSkipLabel;

    [Header("--- ХАРАКТЕРИСТИКИ ИГРОКА ---")]
    public int playerMaxHP = 120;
    public int playerCurrentHP;
    public int playerMaxKP = 60;
    public int playerCurrentKP;
    public int medkitsCount = 3;
    public int constitutionCount = 2;

    [Header("--- ДАННЫЕ ТЕКУЩЕГО ВРАГА ---")]
    public string currentEnemyName = "Враг";
    public EnemyDifficulty enemyDifficulty = EnemyDifficulty.Medium;
    public int enemyMaxHP = 100;
    public int enemyCurrentHP;
    public int enemyCurrentKP = 20;
    public int enemyMaxKP = 60;

    [HideInInspector] public int currentRound = 1;
    [HideInInspector] public bool isPlayerTurn = false;
    [HideInInspector] public bool isComboRound = false;
    [HideInInspector] public bool battleActive = false;

    private BattleCategory activeCategory = BattleCategory.Weapons;
    private int enemyLastHealRound = -10;
    private float damageBuffMultiplier = 1f;
    private float incomingDamageMultiplier = 1f;
    private bool enemyStunned = false;
    private GameObject currentWorldEnemy;
    private bool uiEventsBound = false;

    public enum MoveEffectType
    {
        Damage,
        SharpenBuff,
        HealSelf,
        FortifyShield,
        FocusRestore,
        WeakenEnemy,
        StunStrike,
        LifeDrain
    }

    public struct QueuedAction
    {
        public string actionName;
        public BattleCategory category;
        public MoveEffectType effectType;
        public int minDmg;
        public int maxDmg;
        public int value;
        public int kpCost;
    }

    public List<QueuedAction> comboQueue = new List<QueuedAction>();

    // Цвета карточек по категориям как в Vampire's Fall
    private static readonly Color ColControlCard  = new Color(0.28f, 0.14f, 0.36f, 0.96f);
    private static readonly Color ColInstinctCard = new Color(0.10f, 0.25f, 0.30f, 0.96f);
    private static readonly Color ColWeaponCard   = new Color(0.36f, 0.10f, 0.10f, 0.96f);

    void Awake()
    {
        Instance = this;
        playerCurrentHP = playerMaxHP;
        BindUIEvents();
        if (battleScreen2D != null) battleScreen2D.SetActive(false);
    }

    public void BindUIEvents()
    {
        if (uiEventsBound) return;
        uiEventsBound = true;

        if (btnTabControl != null) btnTabControl.onClick.AddListener(() => SelectCategory(BattleCategory.Control));
        if (btnTabInstinct != null) btnTabInstinct.onClick.AddListener(() => SelectCategory(BattleCategory.Instinct));
        if (btnTabWeapons != null) btnTabWeapons.onClick.AddListener(() => SelectCategory(BattleCategory.Weapons));

        if (cardButtons != null)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int idx = i;
                if (cardButtons[i] != null)
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(idx));
            }
        }

        if (queueCancelButtons != null)
        {
            for (int i = 0; i < queueCancelButtons.Length; i++)
            {
                int idx = i;
                if (queueCancelButtons[i] != null)
                    queueCancelButtons[i].onClick.AddListener(() => RemoveFromQueue(idx));
            }
        }

        if (btnGoOrSkip != null) btnGoOrSkip.onClick.AddListener(Btn_SkipOrFinishTurn);
        if (btnMenuFlee != null) btnMenuFlee.onClick.AddListener(Btn_FleeBattle);
    }

    // =========================================================================
    // 1. СТАРТ 2D БОЯ
    // =========================================================================
    public void StartBattle(GameObject worldEnemy, string enemyName, int enemyHP, EnemyDifficulty difficulty, Sprite enemySprite = null, Sprite bgSprite = null)
    {
        if (battleActive) return;
        battleActive = true;
        currentWorldEnemy = worldEnemy;
        currentEnemyName = string.IsNullOrEmpty(enemyName) ? "ДОЛЖНИК" : enemyName;

        // Закрываем городские окна, если были открыты
        if (InventoryScreenView.Instance != null && InventoryScreenView.Instance.IsOpen)
            InventoryScreenView.Instance.Close();
        QuestUI.CloseJournal();

        if (worldPlayer != null) worldPlayer.enabled = false;
        if (worldMapObject != null) worldMapObject.SetActive(false);

        if (battleScreen2D != null) battleScreen2D.SetActive(true);

        if (enemyImage2D != null && enemySprite != null)
            enemyImage2D.sprite = enemySprite;

        if (backgroundImage2D != null && bgSprite != null)
            backgroundImage2D.sprite = bgSprite;

        if (RPGGameManager.Instance != null)
            playerMaxHP = RPGGameManager.Instance.GetTotalMaxResolve();

        enemyDifficulty = difficulty;
        enemyMaxHP = Mathf.Max(10, enemyHP);
        enemyCurrentHP = enemyMaxHP;
        enemyCurrentKP = 20;
        enemyLastHealRound = -10;

        playerCurrentHP = Mathf.Clamp(playerCurrentHP, 1, playerMaxHP);
        playerCurrentKP = 25;
        currentRound = 0;
        damageBuffMultiplier = 1f;
        incomingDamageMultiplier = 1f;
        enemyStunned = false;

        activeCategory = BattleCategory.Weapons;
        if (floatingLogText != null) floatingLogText.text = "";

        StartNewRound();
    }

    public void SetPlayerWeaponSprite(Sprite newWeaponSprite)
    {
        if (playerWeaponImage2D != null)
        {
            playerWeaponImage2D.sprite = newWeaponSprite;
            playerWeaponImage2D.enabled = newWeaponSprite != null;
        }
    }

    private void StartNewRound()
    {
        currentRound++;
        comboQueue.Clear();
        incomingDamageMultiplier = 1f;

        isComboRound = (currentRound % 3 == 0);

        playerCurrentKP = Mathf.Min(playerMaxKP, playerCurrentKP + 12);
        if (enemyDifficulty == EnemyDifficulty.Hard)
            enemyCurrentKP = Mathf.Min(enemyMaxKP, enemyCurrentKP + 10);

        isPlayerTurn = true;
        RefreshBattleUI();
    }

    // =========================================================================
    // 2. КАТЕГОРИИ И КАРТОЧКИ ДЕЙСТВИЙ (CONTROL / INSTINCT / WEAPONS)
    // =========================================================================
    public void SelectCategory(BattleCategory cat)
    {
        if (!isPlayerTurn) return;
        activeCategory = cat;
        RefreshBattleUI();
    }

    private int GetEquipBonusDamage()
    {
        return RPGGameManager.Instance != null ? RPGGameManager.Instance.GetTotalDamageBonus() : 10;
    }

    private QueuedAction GetCardDefinition(BattleCategory cat, int slot)
    {
        int b = GetEquipBonusDamage();

        switch (cat)
        {
            case BattleCategory.Control:
                switch (slot)
                {
                    case 0:
                        return new QueuedAction
                        {
                            actionName = "УДАР В СПИНУ",
                            category = BattleCategory.Control,
                            effectType = MoveEffectType.Damage,
                            minDmg = Mathf.RoundToInt((14 + b) * 1.25f),
                            maxDmg = Mathf.RoundToInt((20 + b) * 1.25f),
                            kpCost = 20
                        };
                    case 1:
                        return new QueuedAction
                        {
                            actionName = "ПОДАВЛЕНИЕ",
                            category = BattleCategory.Control,
                            effectType = MoveEffectType.WeakenEnemy,
                            minDmg = 6 + b / 2,
                            maxDmg = 10 + b / 2,
                            kpCost = 25
                        };
                    case 2:
                        return new QueuedAction
                        {
                            actionName = "ВЫКАЧКА СИЛ",
                            category = BattleCategory.Control,
                            effectType = MoveEffectType.LifeDrain,
                            minDmg = 12 + b,
                            maxDmg = 18 + b,
                            kpCost = 30
                        };
                    default:
                        return new QueuedAction
                        {
                            actionName = "ОГЛУШЕНИЕ",
                            category = BattleCategory.Control,
                            effectType = MoveEffectType.StunStrike,
                            minDmg = 10 + b,
                            maxDmg = 15 + b,
                            kpCost = 40
                        };
                }

            case BattleCategory.Instinct:
                switch (slot)
                {
                    case 0:
                        return new QueuedAction
                        {
                            actionName = "ЗАТОЧКА (+50%)",
                            category = BattleCategory.Instinct,
                            effectType = MoveEffectType.SharpenBuff,
                            value = 50,
                            kpCost = 15
                        };
                    case 1:
                        return new QueuedAction
                        {
                            actionName = "ПЕРЕВЯЗКА",
                            category = BattleCategory.Instinct,
                            effectType = MoveEffectType.HealSelf,
                            value = 45,
                            kpCost = 20
                        };
                    case 2:
                        return new QueuedAction
                        {
                            actionName = "СТОЙКОСТЬ",
                            category = BattleCategory.Instinct,
                            effectType = MoveEffectType.FortifyShield,
                            value = 50,
                            kpCost = 15
                        };
                    default:
                        return new QueuedAction
                        {
                            actionName = "КОНЦЕНТРАЦИЯ",
                            category = BattleCategory.Instinct,
                            effectType = MoveEffectType.FocusRestore,
                            value = 25,
                            kpCost = 0
                        };
                }

            default: // BattleCategory.Weapons
                switch (slot)
                {
                    case 0:
                        return new QueuedAction
                        {
                            actionName = "БЫСТРЫЙ УДАР",
                            category = BattleCategory.Weapons,
                            effectType = MoveEffectType.Damage,
                            minDmg = 6 + b,
                            maxDmg = 11 + b,
                            kpCost = 0
                        };
                    case 1:
                        return new QueuedAction
                        {
                            actionName = "ВЫПАД",
                            category = BattleCategory.Weapons,
                            effectType = MoveEffectType.Damage,
                            minDmg = 14 + b,
                            maxDmg = 21 + b,
                            kpCost = 14
                        };
                    case 2:
                        return new QueuedAction
                        {
                            actionName = "ТЯЖЁЛЫЙ УДАР",
                            category = BattleCategory.Weapons,
                            effectType = MoveEffectType.Damage,
                            minDmg = 24 + b,
                            maxDmg = 34 + b,
                            kpCost = 26
                        };
                    default:
                        return new QueuedAction
                        {
                            actionName = "ДВОЙНОЙ НАТИСК",
                            category = BattleCategory.Weapons,
                            effectType = MoveEffectType.Damage,
                            minDmg = 38 + Mathf.RoundToInt(b * 1.4f),
                            maxDmg = 52 + Mathf.RoundToInt(b * 1.4f),
                            kpCost = 42
                        };
                }
        }
    }

    private void OnCardClicked(int slotIndex)
    {
        if (!isPlayerTurn) return;

        QueuedAction act = GetCardDefinition(activeCategory, slotIndex);
        if (playerCurrentKP < act.kpCost)
        {
            ShowFloatingLog("Не хватает FOCUS!");
            return;
        }

        int maxQueueSize = isComboRound ? 4 : 1;
        if (comboQueue.Count >= maxQueueSize)
        {
            // В обычный раунд клик по карточке сразу выполняет её или заменяет выбранную
            if (!isComboRound)
            {
                playerCurrentKP -= act.kpCost;
                comboQueue.Add(act);
                RefreshBattleUI();
                StartCoroutine(ExecutePlayerQueueRoutine());
                return;
            }
            ShowFloatingLog("Очередь заполнена! Нажмите GO.");
            return;
        }

        playerCurrentKP -= act.kpCost;
        comboQueue.Add(act);
        RefreshBattleUI();

        // В обычный раунд баффы с ненулевой ценой можно сразу применить, а удар запускает ход
        if (!isComboRound)
        {
            StartCoroutine(ExecutePlayerQueueRoutine());
        }
    }

    public void RemoveFromQueue(int index)
    {
        if (!isPlayerTurn || index < 0 || index >= comboQueue.Count) return;
        playerCurrentKP = Mathf.Min(playerMaxKP, playerCurrentKP + comboQueue[index].kpCost);
        comboQueue.RemoveAt(index);
        RefreshBattleUI();
    }

    // Оставлены старые публичные методы на случай старых кнопок
    public void Btn_WeakAttack() { SelectCategory(BattleCategory.Weapons); OnCardClicked(0); }
    public void Btn_StrongAttack() { SelectCategory(BattleCategory.Weapons); OnCardClicked(1); }
    public void Btn_SpecialWeaponAttack() { SelectCategory(BattleCategory.Weapons); OnCardClicked(2); }
    public void Btn_Ability_DamageBuff() { SelectCategory(BattleCategory.Instinct); OnCardClicked(0); }
    public void Btn_Item_Medkit() { SelectCategory(BattleCategory.Instinct); OnCardClicked(1); }
    public void Btn_Item_Constitution() { SelectCategory(BattleCategory.Instinct); OnCardClicked(3); }

    public void Btn_SkipOrFinishTurn()
    {
        if (!isPlayerTurn) return;

        if (comboQueue.Count > 0)
        {
            StartCoroutine(ExecutePlayerQueueRoutine());
            return;
        }

        isPlayerTurn = false;
        playerCurrentKP = Mathf.Min(playerMaxKP, playerCurrentKP + 15);
        ShowFloatingLog("Пропуск хода: +15 FOCUS");
        RefreshBattleUI();
        StartCoroutine(EnemyTurnRoutine());
    }

    public void Btn_FleeBattle()
    {
        if (!battleActive || !isPlayerTurn) return;
        StopAllCoroutines();
        battleActive = false;
        if (battleScreen2D != null) battleScreen2D.SetActive(false);
        if (worldMapObject != null) worldMapObject.SetActive(true);
        if (worldPlayer != null) worldPlayer.enabled = true;
    }

    // =========================================================================
    // 3. ВЫПОЛНЕНИЕ ОЧЕРЕДИ ИГРОКА (QUEUE -> GO!)
    // =========================================================================
    private IEnumerator ExecutePlayerQueueRoutine()
    {
        isPlayerTurn = false;
        RefreshBattleUI();

        var actionsToRun = new List<QueuedAction>(comboQueue);
        comboQueue.Clear();
        RefreshBattleUI();

        foreach (var act in actionsToRun)
        {
            switch (act.effectType)
            {
                case MoveEffectType.SharpenBuff:
                    damageBuffMultiplier = 1.5f;
                    ShowFloatingLog("ЗАТОЧКА: Урон +50%!");
                    yield return StartCoroutine(FlashSpriteColor(playerImage2D, new Color(1f, 0.75f, 0.2f)));
                    break;

                case MoveEffectType.HealSelf:
                    playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + act.value);
                    ShowFloatingLog($"+{act.value} HP");
                    RefreshBattleUI();
                    yield return StartCoroutine(FlashSpriteColor(playerImage2D, Color.green));
                    break;

                case MoveEffectType.FortifyShield:
                    incomingDamageMultiplier = 0.5f;
                    ShowFloatingLog("СТОЙКОСТЬ: Защита +50%");
                    yield return StartCoroutine(FlashSpriteColor(playerImage2D, Color.cyan));
                    break;

                case MoveEffectType.FocusRestore:
                    playerCurrentKP = Mathf.Min(playerMaxKP, playerCurrentKP + act.value);
                    ShowFloatingLog($"+{act.value} FOCUS");
                    RefreshBattleUI();
                    yield return StartCoroutine(FlashSpriteColor(playerImage2D, new Color(0.3f, 0.7f, 1f)));
                    break;

                case MoveEffectType.WeakenEnemy:
                case MoveEffectType.LifeDrain:
                case MoveEffectType.StunStrike:
                case MoveEffectType.Damage:
                    int rawDmg = Random.Range(act.minDmg, act.maxDmg + 1);
                    int finalDmg = Mathf.RoundToInt(rawDmg * damageBuffMultiplier);

                    yield return StartCoroutine(Animate2DHit(playerImage2D, enemyImage2D, 75f));

                    enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - finalDmg);

                    if (act.effectType == MoveEffectType.LifeDrain)
                    {
                        int heal = Mathf.Max(5, finalDmg / 2);
                        playerCurrentHP = Mathf.Min(playerMaxHP, playerCurrentHP + heal);
                        ShowFloatingLog($"{act.actionName}: -{finalDmg} HP (Вампиризм +{heal})");
                    }
                    else if (act.effectType == MoveEffectType.StunStrike)
                    {
                        enemyStunned = true;
                        ShowFloatingLog($"{act.actionName}: -{finalDmg} HP (ВРАГ ОГЛУШЁН!)");
                    }
                    else if (act.effectType == MoveEffectType.WeakenEnemy)
                    {
                        incomingDamageMultiplier = 0.65f;
                        ShowFloatingLog($"{act.actionName}: -{finalDmg} HP (Враг ослаблен)");
                    }
                    else
                    {
                        ShowFloatingLog($"{act.actionName}: -{finalDmg} DMG!");
                    }

                    RefreshBattleUI();
                    yield return new WaitForSeconds(0.28f);

                    if (enemyCurrentHP <= 0)
                    {
                        yield return new WaitForSeconds(0.4f);
                        EndBattle(true);
                        yield break;
                    }
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        damageBuffMultiplier = 1f;
        yield return new WaitForSeconds(0.35f);
        StartCoroutine(EnemyTurnRoutine());
    }

    // =========================================================================
    // 4. ХОД ВРАГА
    // =========================================================================
    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        if (enemyStunned)
        {
            enemyStunned = false;
            ShowFloatingLog($"{currentEnemyName} пропускает ход (Оглушение)!");
            yield return new WaitForSeconds(0.7f);
            StartNewRound();
            yield break;
        }

        bool lowHP = (enemyCurrentHP <= enemyMaxHP * 0.4f);
        bool canHeal = (currentRound - enemyLastHealRound >= 2);

        switch (enemyDifficulty)
        {
            case EnemyDifficulty.Easy:
                yield return StartCoroutine(EnemyHitPlayer(7, 12));
                break;

            case EnemyDifficulty.Medium:
                if (lowHP && canHeal)
                {
                    enemyLastHealRound = currentRound;
                    int heal = Mathf.RoundToInt(enemyMaxHP * 0.25f);
                    enemyCurrentHP = Mathf.Min(enemyMaxHP, enemyCurrentHP + heal);
                    ShowFloatingLog($"{currentEnemyName} лечится: +{heal} HP");
                    RefreshBattleUI();
                    yield return StartCoroutine(FlashSpriteColor(enemyImage2D, Color.green));
                }
                else
                {
                    yield return StartCoroutine(EnemyHitPlayer(11, 18));
                }
                break;

            case EnemyDifficulty.Hard:
                if (lowHP && canHeal)
                {
                    enemyLastHealRound = currentRound;
                    int heal = Mathf.RoundToInt(enemyMaxHP * 0.3f);
                    enemyCurrentHP = Mathf.Min(enemyMaxHP, enemyCurrentHP + heal);
                    ShowFloatingLog($"{currentEnemyName} лечится: +{heal} HP");
                    RefreshBattleUI();
                    yield return StartCoroutine(FlashSpriteColor(enemyImage2D, Color.green));
                }
                else if (isComboRound)
                {
                    while (enemyCurrentKP >= 15 && playerCurrentHP > 0)
                    {
                        enemyCurrentKP -= 15;
                        yield return StartCoroutine(EnemyHitPlayer(14, 21));
                        yield return new WaitForSeconds(0.22f);
                    }
                    if (playerCurrentHP > 0)
                        yield return StartCoroutine(EnemyHitPlayer(9, 14));
                }
                else
                {
                    enemyCurrentKP = Mathf.Min(enemyMaxKP, enemyCurrentKP + 5);
                    yield return StartCoroutine(EnemyHitPlayer(12, 18));
                }
                break;
        }

        if (playerCurrentHP <= 0)
        {
            yield return new WaitForSeconds(0.6f);
            EndBattle(false);
            yield break;
        }

        yield return new WaitForSeconds(0.4f);
        StartNewRound();
    }

    private IEnumerator EnemyHitPlayer(int minD, int maxD)
    {
        yield return StartCoroutine(Animate2DHit(enemyImage2D, playerImage2D, -75f));
        int dmg = Mathf.Max(1, Mathf.RoundToInt(Random.Range(minD, maxD + 1) * incomingDamageMultiplier));
        playerCurrentHP = Mathf.Max(0, playerCurrentHP - dmg);
        ShowFloatingLog($"{currentEnemyName} наносит -{dmg} DMG!");
        RefreshBattleUI();
    }

    private IEnumerator Animate2DHit(Image attacker, Image defender, float moveOffsetX)
    {
        if (attacker != null)
        {
            RectTransform rt = attacker.rectTransform;
            Vector2 startPos = rt.anchoredPosition;
            rt.anchoredPosition = startPos + new Vector2(moveOffsetX, 0);
            yield return new WaitForSeconds(0.12f);
            if (defender != null) StartCoroutine(FlashSpriteColor(defender, Color.red));
            yield return new WaitForSeconds(0.12f);
            rt.anchoredPosition = startPos;
        }
        else
        {
            yield return new WaitForSeconds(0.24f);
        }
    }

    private IEnumerator FlashSpriteColor(Image img, Color flashCol)
    {
        if (img == null) yield break;
        img.color = flashCol;
        yield return new WaitForSeconds(0.16f);
        img.color = Color.white;
    }

    private void ShowFloatingLog(string msg)
    {
        if (floatingLogText != null) floatingLogText.text = msg;
    }

    // =========================================================================
    // 5. ОБНОВЛЕНИЕ ВСЕХ ЭЛЕМЕНТОВ ЭКРАНА БОЯ (VAMPIRE'S FALL STYLE)
    // =========================================================================
    public void RefreshBattleUI()
    {
        string pName = RPGGameManager.Instance != null ? RPGGameManager.Instance.characterName.ToUpper() : "LUCARD";
        if (playerNameText != null) playerNameText.text = pName;
        if (playerHpText != null) playerHpText.text = $"{playerCurrentHP} / {playerMaxHP}";
        if (playerHpFill != null) SetBarRatio(playerHpFill, playerCurrentHP / (float)Mathf.Max(1, playerMaxHP));

        if (playerFocusText != null) playerFocusText.text = $"{playerCurrentKP} / {playerMaxKP}";
        if (playerFocusFill != null) SetBarRatio(playerFocusFill, playerCurrentKP / (float)Mathf.Max(1, playerMaxKP));

        if (enemyNameText != null) enemyNameText.text = currentEnemyName.ToUpper();
        if (enemyHpText != null) enemyHpText.text = $"{enemyCurrentHP} / {enemyMaxHP}";
        if (enemyHpFill != null) SetBarRatio(enemyHpFill, enemyCurrentHP / (float)Mathf.Max(1, enemyMaxHP));

        if (roundBannerText != null)
        {
            roundBannerText.text = isComboRound
                ? $"<color=#F28C28>РАУНД {currentRound} — КОМБО-ХОД (НАБЕРИТЕ СЕРИЮ В QUEUE!)</color>"
                : $"РАУНД {currentRound}";
        }

        // Заголовки над карточками (LEFT / RIGHT или название ветки)
        if (headerLeftLabel != null)
            headerLeftLabel.text = activeCategory == BattleCategory.Weapons ? "LEFT / MAIN" : activeCategory.ToString().ToUpper();
        if (headerRightLabel != null)
            headerRightLabel.text = activeCategory == BattleCategory.Weapons ? "RIGHT / DUAL" : "SPECIAL";

        // 4 карточки способностей
        Color cardTheme = activeCategory == BattleCategory.Control ? ColControlCard
                        : activeCategory == BattleCategory.Instinct ? ColInstinctCard
                        : ColWeaponCard;

        for (int i = 0; i < 4; i++)
        {
            QueuedAction def = GetCardDefinition(activeCategory, i);
            bool canAfford = isPlayerTurn && playerCurrentKP >= def.kpCost;

            if (cardBgs != null && i < cardBgs.Length && cardBgs[i] != null)
                cardBgs[i].color = canAfford ? cardTheme : new Color(cardTheme.r * 0.45f, cardTheme.g * 0.45f, cardTheme.b * 0.45f, 0.9f);

            if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null)
                cardButtons[i].interactable = canAfford;

            if (cardTitles != null && i < cardTitles.Length && cardTitles[i] != null)
                cardTitles[i].text = def.actionName;

            if (cardEffects != null && i < cardEffects.Length && cardEffects[i] != null)
            {
                if (def.effectType == MoveEffectType.Damage || def.effectType == MoveEffectType.WeakenEnemy ||
                    def.effectType == MoveEffectType.LifeDrain || def.effectType == MoveEffectType.StunStrike)
                {
                    int minD = Mathf.RoundToInt(def.minDmg * damageBuffMultiplier);
                    int maxD = Mathf.RoundToInt(def.maxDmg * damageBuffMultiplier);
                    cardEffects[i].text = $"{minD}-{maxD} DMG";
                }
                else if (def.effectType == MoveEffectType.SharpenBuff)
                    cardEffects[i].text = $"+{def.value}% К УРОНУ";
                else if (def.effectType == MoveEffectType.HealSelf)
                    cardEffects[i].text = $"+{def.value} РЕШИМОСТЬ";
                else if (def.effectType == MoveEffectType.FortifyShield)
                    cardEffects[i].text = $"-{def.value}% УРОНА ВРАГА";
                else
                    cardEffects[i].text = $"+{def.value} FOCUS";
            }

            if (cardCosts != null && i < cardCosts.Length && cardCosts[i] != null)
                cardCosts[i].text = $"{def.kpCost} <color=#4DA6FF>●</color>";
        }

        // Очередь (QUEUE)
        if (queueRowObjects != null)
        {
            for (int i = 0; i < queueRowObjects.Length; i++)
            {
                if (queueRowObjects[i] == null) continue;
                bool active = i < comboQueue.Count;
                queueRowObjects[i].SetActive(active);
                if (active)
                {
                    var q = comboQueue[i];
                    if (queueRowTexts != null && i < queueRowTexts.Length && queueRowTexts[i] != null)
                        queueRowTexts[i].text = $"{q.actionName}  <color=#8AB4F8>- {q.kpCost} Focus</color>";

                    if (queueRowBgs != null && i < queueRowBgs.Length && queueRowBgs[i] != null)
                    {
                        queueRowBgs[i].color = q.category == BattleCategory.Control ? ColControlCard
                                             : q.category == BattleCategory.Instinct ? ColInstinctCard
                                             : ColWeaponCard;
                    }
                }
            }
        }

        // Кнопка GO / SKIP TURN
        if (btnGoOrSkip != null) btnGoOrSkip.interactable = isPlayerTurn;
        if (btnGoOrSkipLabel != null)
            btnGoOrSkipLabel.text = comboQueue.Count > 0 ? "GO !" : "SKIP TURN";
    }

    private static void SetBarRatio(Image bar, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        var rt = bar.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(ratio, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // 6. ЗАВЕРШЕНИЕ БОЯ И НАГРАДЫ
    // =========================================================================
    private void EndBattle(bool playerWon)
    {
        battleActive = false;
        if (battleScreen2D != null) battleScreen2D.SetActive(false);

        if (worldMapObject != null) worldMapObject.SetActive(true);
        if (worldPlayer != null) worldPlayer.enabled = true;

        if (playerWon)
        {
            if (currentWorldEnemy != null)
            {
                var inter = currentWorldEnemy.GetComponent<WorldInteractable>();
                if (inter != null)
                {
                    if (RPGGameManager.Instance != null)
                    {
                        RPGGameManager.Instance.gold += inter.goldReward;
                        RPGGameManager.Instance.AddXP(inter.expReward);
                    }

                    if (QuestManager.Instance != null)
                        QuestManager.Instance.ReportDefeat(inter.npcId);

                    if (inter.destroyOnDefeat)
                        Destroy(currentWorldEnemy);
                    else if (inter.afterWinDialogue != null && DialogueManager.Instance != null)
                        DialogueManager.Instance.OpenData(inter, inter.afterWinDialogue);
                }
                else
                {
                    Destroy(currentWorldEnemy);
                }
            }
        }
        else
        {
            playerCurrentHP = playerMaxHP;
        }
    }
}