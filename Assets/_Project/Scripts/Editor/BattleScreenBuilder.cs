#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Верхнее меню Unity: RPG -> UI -> Построить 2D Экран Боя (Vampire's Fall Style).
/// Автоматически создаёт 2D арену, верхние полоски HP/Focus, нижнюю панель с CONTROL/INSTINCT/WEAPONS,
/// 4 карточками ударов, списком QUEUE и кнопками MENU / SKIP TURN (GO) и связывает всё с BattleManager.
/// </summary>
public class BattleScreenBuilder : EditorWindow
{
    private Sprite defaultBgSprite, playerSprite, weaponSprite, enemySprite, frameSprite;
    private TMP_FontAsset font;

    private static readonly Color DarkSteel     = new Color(0.09f, 0.10f, 0.12f, 0.98f);
    private static readonly Color PanelBorder   = new Color(0.38f, 0.40f, 0.45f, 0.95f);
    private static readonly Color BloodRedBar   = new Color(0.72f, 0.14f, 0.14f, 1f);
    private static readonly Color FocusBlueBar  = new Color(0.16f, 0.42f, 0.68f, 1f);
    private static readonly Color ControlTabCol = new Color(0.28f, 0.16f, 0.38f, 1f);
    private static readonly Color InstinctTabCol= new Color(0.10f, 0.24f, 0.30f, 1f);
    private static readonly Color WeaponTabCol  = new Color(0.38f, 0.12f, 0.10f, 1f);
    private static readonly Color GoBtnCol      = new Color(0.45f, 0.08f, 0.08f, 1f);

    [MenuItem("RPG/UI/Построить 2D Экран Боя (Vampire's Fall)")]
    [MenuItem("GameObject/RPG UI/Построить 2D Экран Боя", false, 11)]
    static void Open()
    {
        GetWindow<BattleScreenBuilder>("Сборщик 2D Боя");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Собирает 2D экран боя по референсу Vampire's Fall: Origins:\n" +
            "• Отдельный 2D фон + спрайты героя и врага\n" +
            "• Верхние бары HP и FOCUS\n" +
            "• Нижняя панель: CONTROL / INSTINCT / WEAPONS, 4 карточки, QUEUE и кнопка SKIP TURN / GO.\n" +
            "Все поля необязательны (можно назначить позже).", MessageType.Info);

        defaultBgSprite = (Sprite)EditorGUILayout.ObjectField("Фон арены 2D", defaultBgSprite, typeof(Sprite), false);
        playerSprite    = (Sprite)EditorGUILayout.ObjectField("Спрайт Игрока 2D", playerSprite, typeof(Sprite), false);
        weaponSprite    = (Sprite)EditorGUILayout.ObjectField("Спрайт Оружия (необяз.)", weaponSprite, typeof(Sprite), false);
        enemySprite     = (Sprite)EditorGUILayout.ObjectField("Спрайт Врага 2D", enemySprite, typeof(Sprite), false);
        frameSprite     = (Sprite)EditorGUILayout.ObjectField("Рамка панелей (необяз.)", frameSprite, typeof(Sprite), false);
        font            = (TMP_FontAsset)EditorGUILayout.ObjectField("Шрифт TMP (необяз.)", font, typeof(TMP_FontAsset), false);

        GUILayout.Space(12);
        if (GUILayout.Button("ПОСТРОИТЬ 2D ЭКРАН БОЯ И ПРИВЯЗАТЬ К BATTLE MANAGER", GUILayout.Height(40)))
        {
            BuildBattleUI();
        }

        GUILayout.Space(8);
        if (Application.isPlaying && BattleManager.Instance != null)
        {
            if (GUILayout.Button("ЗАПУСТИТЬ ТЕСТОВЫЙ БОЙ ПРЯМО СЕЙЧАС", GUILayout.Height(32)))
            {
                BattleManager.Instance.StartBattle(null, "HOODED FIGURE", 130, EnemyDifficulty.Hard, enemySprite, defaultBgSprite);
            }
        }
    }

    private void BuildBattleUI()
    {
        var canvas = GetOrCreateCanvas();

        var old = canvas.transform.Find("BattleScreen2D");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var root = NewRect("BattleScreen2D", canvas.transform, new Vector2(1920, 1080), Vector2.zero);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build BattleScreen2D");

        // ====================================================================
        // 1. 2D ФОН АРЕНЫ И БОЙЦЫ (Верхние 72% экрана)
        // ====================================================================
        var bgRt = NewRect("ArenaBackground2D", root, new Vector2(1920, 1080), Vector2.zero);
        var bgImg = bgRt.gameObject.AddComponent<Image>();
        bgImg.sprite = defaultBgSprite;
        bgImg.color = defaultBgSprite != null ? Color.white : new Color(0.16f, 0.20f, 0.22f, 1f);

        // Земля/туман внизу арены для контраста
        var groundShade = NewRect("GroundVignette", bgRt, new Vector2(1920, 320), new Vector2(0, -100));
        var groundImg = groundShade.gameObject.AddComponent<Image>();
        groundImg.color = new Color(0.05f, 0.07f, 0.08f, 0.55f);
        groundImg.raycastTarget = false;

        // Игрок слева
        var playerRt = NewRect("PlayerFighter2D", bgRt, new Vector2(340, 440), new Vector2(-470, 20));
        var pImg = playerRt.gameObject.AddComponent<Image>();
        pImg.sprite = playerSprite;
        pImg.preserveAspect = true;
        pImg.color = Color.white;
        pImg.raycastTarget = false;

        var weaponRt = NewRect("PlayerWeapon2D", playerRt, new Vector2(180, 180), new Vector2(75, 10));
        var wImg = weaponRt.gameObject.AddComponent<Image>();
        wImg.sprite = weaponSprite;
        wImg.preserveAspect = true;
        wImg.enabled = weaponSprite != null;
        wImg.raycastTarget = false;

        // Враг справа
        var enemyRt = NewRect("EnemyFighter2D", bgRt, new Vector2(360, 460), new Vector2(470, 30));
        var eImg = enemyRt.gameObject.AddComponent<Image>();
        eImg.sprite = enemySprite;
        eImg.preserveAspect = true;
        eImg.color = Color.white;
        eImg.raycastTarget = false;

        // ====================================================================
        // 2. ВЕРХНИЕ БАРЫ: ИГРОК СЛЕВА (HP + FOCUS), ВРАГ СПРАВА (HP)
        // ====================================================================
        // --- Левый верхний блок игрока ---
        var pHpFrame = MakeBox(root, "PlayerHPBox", new Vector2(610, 52), new Vector2(-645, 504), DarkSteel);
        var pHpFill = MakeDynamicFillBar(pHpFrame, "PlayerHPFill", BloodRedBar);
        var pHpTxt = MakeText(pHpFrame, "PlayerHPValue", "128 / 128", 22, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(260, 44), new Vector2(-155, 0));
        var pNameTxt = MakeText(pHpFrame, "PlayerName", "LUCARD", 22, UITheme.Parchment, TextAlignmentOptions.MidlineRight, FontStyles.Bold, new Vector2(280, 44), new Vector2(145, 0));

        var pFocusFrame = MakeBox(root, "PlayerFocusBox", new Vector2(540, 42), new Vector2(-680, 452), DarkSteel);
        var pFocusFill = MakeDynamicFillBar(pFocusFrame, "PlayerFocusFill", FocusBlueBar);
        var pFocusTxt = MakeText(pFocusFrame, "PlayerFocusValue", "20 / 60", 19, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(220, 36), new Vector2(-140, 0));
        MakeText(pFocusFrame, "FocusLabel", "FOCUS", 19, UITheme.Parchment, TextAlignmentOptions.MidlineRight, FontStyles.Bold, new Vector2(200, 36), new Vector2(130, 0));

        // --- Правый верхний блок врага ---
        var eHpFrame = MakeBox(root, "EnemyHPBox", new Vector2(610, 52), new Vector2(645, 504), DarkSteel);
        var eHpFill = MakeDynamicFillBar(eHpFrame, "EnemyHPFill", BloodRedBar);
        var eNameTxt = MakeText(eHpFrame, "EnemyName", "HOODED FIGURE", 22, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(320, 44), new Vector2(-125, 0));
        var eHpTxt = MakeText(eHpFrame, "EnemyHPValue", "130 / 130", 22, UITheme.Parchment, TextAlignmentOptions.MidlineRight, FontStyles.Bold, new Vector2(240, 44), new Vector2(155, 0));

        // --- Центральный индикатор раунда и лог ударов ---
        var roundTxt = MakeText(root, "RoundBanner", "РАУНД 1", 20, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(620, 36), new Vector2(0, 504));
        var floatLogTxt = MakeText(root, "FloatingCombatLog", "", 26, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(800, 48), new Vector2(0, 290));

        // ====================================================================
        // 3. НИЖНЯЯ БОЕВАЯ ПАНЕЛЬ (Высота 290px, ровно как в Vampire's Fall)
        // ====================================================================
        var bottomHud = MakeBox(root, "BottomCombatBar", new Vector2(1920, 290), new Vector2(0, -395), DarkSteel);

        // Плашки-заголовки над секциями (LEFT, RIGHT, QUEUE)
        var leftHeaderBox = MakeBox(bottomHud, "HeaderLeftTab", new Vector2(180, 28), new Vector2(-425, 152), DarkSteel);
        var headerLeftTxt = MakeText(leftHeaderBox, "Label", "LEFT / MAIN", 14, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(170, 24), Vector2.zero);

        var rightHeaderBox = MakeBox(bottomHud, "HeaderRightTab", new Vector2(180, 28), new Vector2(35, 152), DarkSteel);
        var headerRightTxt = MakeText(rightHeaderBox, "Label", "RIGHT / DUAL", 14, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(170, 24), Vector2.zero);

        var queueHeaderBox = MakeBox(bottomHud, "HeaderQueueTab", new Vector2(160, 28), new Vector2(455, 152), DarkSteel);
        MakeText(queueHeaderBox, "Label", "QUEUE", 14, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(150, 24), Vector2.zero);

        // --- А. ЛЕВАЯ КОЛОНКА: 3 ВКЛАДКИ (CONTROL / INSTINCT / WEAPONS) ---
        var btnControl  = MakeButton(bottomHud, "Tab_Control",  "CONTROL",  22, new Vector2(270, 84), new Vector2(-815,  90), ControlTabCol, out _);
        var btnInstinct = MakeButton(bottomHud, "Tab_Instinct", "INSTINCT", 22, new Vector2(270, 84), new Vector2(-815,   0), InstinctTabCol, out _);
        var btnWeapons  = MakeButton(bottomHud, "Tab_Weapons",  "WEAPONS",  22, new Vector2(270, 84), new Vector2(-815, -90), WeaponTabCol, out _);

        // --- Б. ЦЕНТР: 4 БОЛЬШИЕ КАРТОЧКИ СПОСОБНОСТЕЙ ---
        var cardBtns   = new Button[4];
        var cardBgs    = new Image[4];
        var cardTitles = new TMP_Text[4];
        var cardDmgs   = new TMP_Text[4];
        var cardCosts  = new TMP_Text[4];

        for (int i = 0; i < 4; i++)
        {
            float cx = -540f + i * 230f;
            var cardRt = MakeBox(bottomHud, "ActionCard_" + i, new Vector2(222, 266), new Vector2(cx, 0), WeaponTabCol);
            var btn = cardRt.gameObject.AddComponent<Button>();
            var img = cardRt.GetComponent<Image>();
            btn.targetGraphic = img;

            cardBtns[i] = btn;
            cardBgs[i] = img;
            cardTitles[i] = MakeText(cardRt, "CardTitle", "STRIKE", 21, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(204, 54), new Vector2(0, 82));
            cardDmgs[i]   = MakeText(cardRt, "CardEffect", "15-22 DMG", 19, new Color(0.95f, 0.80f, 0.65f), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(204, 40), new Vector2(0, -10));
            cardCosts[i]  = MakeText(cardRt, "CardCost", "14 <color=#4DA6FF>●</color>", 26, new Color(0.55f, 0.82f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(204, 42), new Vector2(0, -92));
        }

        // --- В. ПАНЕЛЬ ОЧЕРЕДИ (QUEUE) ---
        var queuePanel = MakeBox(bottomHud, "QueueContainer", new Vector2(360, 266), new Vector2(455, 0), new Color(0.05f, 0.06f, 0.07f, 0.98f));
        var qRowObjs = new GameObject[4];
        var qRowBgs  = new Image[4];
        var qRowTxts = new TMP_Text[4];
        var qCancelBtns = new Button[4];

        for (int i = 0; i < 4; i++)
        {
            float ry = 92f - i * 62f;
            var rowRt = MakeBox(queuePanel, "QueueRow_" + i, new Vector2(342, 54), new Vector2(0, ry), WeaponTabCol);
            qRowObjs[i] = rowRt.gameObject;
            qRowBgs[i]  = rowRt.GetComponent<Image>();
            qRowTxts[i] = MakeText(rowRt, "RowText", "THRUST - 14 Focus", 16, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(276, 46), new Vector2(-24, 0));
            qCancelBtns[i] = MakeButton(rowRt, "BtnCancel", "X", 16, new Vector2(42, 42), new Vector2(142, 0), GoBtnCol, out _);
        }

        // --- Г. ПРАВАЯ КОЛОНКА: MENU И SKIP TURN / GO ---
        var btnMenu = MakeButton(bottomHud, "BtnMenu", "MENU (ОТСТУПИТЬ)", 20, new Vector2(300, 84), new Vector2(795, 90), DarkSteel, out _);
        var btnGo   = MakeButton(bottomHud, "BtnSkipOrGo", "SKIP TURN", 28, new Vector2(300, 174), new Vector2(795, -46), GoBtnCol, out TMP_Text goLabel);

        // ====================================================================
        // 4. ПРИВЯЗКА КО ВСЕМ ПОЛЯМ BATTLE MANAGER НА СЦЕНЕ
        // ====================================================================
        var bm = Object.FindFirstObjectByType<BattleManager>();
        if (bm == null)
        {
            var bmGo = new GameObject("BattleManager");
            bm = bmGo.AddComponent<BattleManager>();
            Undo.RegisterCreatedObjectUndo(bmGo, "Create BattleManager");
        }

        Undo.RecordObject(bm, "Bind Vampire's Fall Battle UI");
        bm.battleScreen2D = root.gameObject;
        bm.backgroundImage2D = bgImg;
        bm.playerImage2D = pImg;
        bm.playerWeaponImage2D = wImg;
        bm.enemyImage2D = eImg;

        bm.playerHpFill = pHpFill;
        bm.playerHpText = pHpTxt;
        bm.playerNameText = pNameTxt;
        bm.playerFocusFill = pFocusFill;
        bm.playerFocusText = pFocusTxt;

        bm.enemyHpFill = eHpFill;
        bm.enemyHpText = eHpTxt;
        bm.enemyNameText = eNameTxt;
        bm.roundBannerText = roundTxt;
        bm.floatingLogText = floatLogTxt;

        bm.btnTabControl = btnControl;
        bm.btnTabInstinct = btnInstinct;
        bm.btnTabWeapons = btnWeapons;
        bm.headerLeftLabel = headerLeftTxt;
        bm.headerRightLabel = headerRightTxt;

        bm.cardButtons = cardBtns;
        bm.cardBgs = cardBgs;
        bm.cardTitles = cardTitles;
        bm.cardEffects = cardDmgs;
        bm.cardCosts = cardCosts;

        bm.queueRowObjects = qRowObjs;
        bm.queueRowBgs = qRowBgs;
        bm.queueRowTexts = qRowTxts;
        bm.queueCancelButtons = qCancelBtns;

        bm.btnMenuFlee = btnMenu;
        bm.btnGoOrSkip = btnGo;
        bm.btnGoOrSkipLabel = goLabel;

        EditorUtility.SetDirty(bm);

        // Оставляем экран выключенным в сцене, чтобы не перекрывал 3D-город до начала боя
        root.gameObject.SetActive(false);

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("2D Экран боя в стиле Vampire's Fall успешно построен и привязан к BattleManager!");
    }

    // ------------------------------------------------------------------ UI Хелперы
    private static Image MakeDynamicFillBar(RectTransform parent, string name, Color color)
    {
        var pad = NewRect(name + "_Container", parent, parent.sizeDelta - new Vector2(8, 8), Vector2.zero);
        var fillRt = NewRect(name, pad, Vector2.zero, Vector2.zero);
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var img = fillRt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private RectTransform MakeBox(RectTransform parent, string name, Vector2 size, Vector2 pos, Color bgColor)
    {
        var rt = NewRect(name, parent, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
        }
        img.color = bgColor;

        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = PanelBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return rt;
    }

    private Button MakeButton(RectTransform parent, string name, string caption, float fontSize, Vector2 size, Vector2 pos, Color bgColor, out TMP_Text labelOut)
    {
        var rt = MakeBox(parent, name, size, pos, bgColor);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        labelOut = MakeText(rt, "Label", caption, fontSize, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, size - new Vector2(10, 6), Vector2.zero);
        return btn;
    }

    private TMP_Text MakeText(RectTransform parent, string name, string text, float size, Color color,
                              TextAlignmentOptions align, FontStyles style, Vector2 box, Vector2 pos)
    {
        var rt = NewRect(name, parent, box, pos);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        return t;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas c = Object.FindFirstObjectByType<Canvas>();
        if (c == null)
        {
            var go = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }
        return c;
    }
}
#endif