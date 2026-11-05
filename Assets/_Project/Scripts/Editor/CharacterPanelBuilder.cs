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
/// Верхнее меню Unity: RPG -> UI -> Построить инвентарь (3 окна).
/// Выровненная вёрстка с учётом широких 9-slice рамок с заклёпками.
/// </summary>
public class CharacterPanelBuilder : EditorWindow
{
    private Sprite frameSprite, slotSprite, selectedSprite, dollSprite;
    private TMP_FontAsset font;

    private static readonly Color Orange       = new Color(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Color PanelBgColor = new Color(0.13f, 0.14f, 0.17f, 0.98f);
    private static readonly Color InnerBgColor = new Color(0.08f, 0.09f, 0.11f, 0.92f);
    private static readonly Color SlotBgColor  = new Color(0.18f, 0.20f, 0.24f, 1f);
    private static readonly Color BtnBgColor   = new Color(0.20f, 0.22f, 0.27f, 1f);
    private static readonly Color MutedText    = new Color(0.62f, 0.65f, 0.70f, 1f);

    private struct SlotPos
    {
        public EquipSlot slot; public Vector2 pos;
        public SlotPos(EquipSlot s, float x, float y) { slot = s; pos = new Vector2(x, y); }
    }

    // Выровненные координаты 11 слотов внутри левого окна (540 x 800) с запасом под рамку с заклёпками
    private static readonly SlotPos[] Layout =
    {
        // левая колонка
        new SlotPos(EquipSlot.Jacket,          -182,  205),
        new SlotPos(EquipSlot.Pants,           -182,   80),
        new SlotPos(EquipSlot.Shoes,           -182,  -45),
        new SlotPos(EquipSlot.Chain,           -182, -170),
        // правая колонка
        new SlotPos(EquipSlot.Backpack,         182,  205),
        new SlotPos(EquipSlot.Watch,            182,   80),
        new SlotPos(EquipSlot.Phone,            182,  -45),
        new SlotPos(EquipSlot.SecondaryWeapon,  182, -170),
        // нижний ряд (поднят выше, чтобы подписи не липли к нижней рамке)
        new SlotPos(EquipSlot.Weapon,          -122, -285),
        new SlotPos(EquipSlot.Head,               0, -285),
        new SlotPos(EquipSlot.Ring,             122, -285),
    };

    [MenuItem("RPG/UI/Построить инвентарь (3 окна)")]
    [MenuItem("GameObject/RPG UI/Построить инвентарь (3 окна)", false, 10)]
    static void Open()
    {
        GetWindow<CharacterPanelBuilder>("Сборщик Инвентаря");
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Собирает выровненный экран «ИНВЕНТАРЬ И СНАРЯЖЕНИЕ» из 3 окон.\n" +
            "Крупная рамка используется только на главных окнах, чтобы мелкие кнопки и поля поиска не сплющивались.",
            MessageType.Info);

        frameSprite = (Sprite)EditorGUILayout.ObjectField("Рамка окна", frameSprite, typeof(Sprite), false);
        slotSprite = (Sprite)EditorGUILayout.ObjectField("Слот", slotSprite, typeof(Sprite), false);
        selectedSprite = (Sprite)EditorGUILayout.ObjectField("Рамка выбора (необяз.)", selectedSprite, typeof(Sprite), false);
        dollSprite = (Sprite)EditorGUILayout.ObjectField("Кукла персонажа (необяз.)", dollSprite, typeof(Sprite), false);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("Шрифт TMP (необяз.)", font, typeof(TMP_FontAsset), false);

        GUILayout.Space(12);
        if (GUILayout.Button("ПОСТРОИТЬ ВЕСЬ ИНВЕНТАРЬ (3 ОКНА)", GUILayout.Height(40)))
        {
            BuildFullInventory();
        }
    }

    private void BuildFullInventory()
    {
        var canvas = GetOrCreateCanvas();

        var old = canvas.transform.Find("InventoryScreen");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var screenRt = NewRect("InventoryScreen", canvas.transform, new Vector2(1920, 1080), Vector2.zero);
        Undo.RegisterCreatedObjectUndo(screenRt.gameObject, "Build Full Inventory UI");

        var screenView = screenRt.gameObject.AddComponent<InventoryScreenView>();

        var root = NewRect("WindowRoot", screenRt, new Vector2(1920, 1080), Vector2.zero);
        screenView.windowRoot = root.gameObject;

        // Затемнение фона
        var dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0.03f, 0.03f, 0.05f, 0.78f);
        dim.raycastTarget = true;

        // ====================================================================
        // 1. ВЕРХНЯЯ ШАПКА И КНОПКА [X] ЗАКРЫТЬ
        // ====================================================================
        var headerRt = MakeMainFrame(root, "HeaderBanner", new Vector2(640, 72), new Vector2(0, 476));
        MakeText(headerRt, "Title", "ИНВЕНТАРЬ И СНАРЯЖЕНИЕ", 28, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(580, 34), new Vector2(0, 10));
        MakeText(headerRt, "SubTitle", "ЭКИПИРОВКА СБОРЩИКА", 15, MutedText, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(400, 20), new Vector2(0, -16));

        var closeBtn = MakeFlatButton(root, "BtnClose", "[X] ЗАКРЫТЬ", 18, new Vector2(190, 50), new Vector2(815, 476), BtnBgColor);
        screenView.closeButton = closeBtn;

        // Все 3 окна стоят строго в одну линию: Y = 22, Высота = 800 (верх = 422, низ = -378)
        const float panelY = 22f;
        const float panelH = 800f;

        // ====================================================================
        // 2. ЛЕВОЕ ОКНО: СБОРЩИК (540 x 800)
        // ====================================================================
        var leftRt = MakeMainFrame(root, "CharacterPanel", new Vector2(540, panelH), new Vector2(-640, panelY));
        var charView = leftRt.gameObject.AddComponent<CharacterPanelView>();
        screenView.characterPanel = charView;

        // Шапка с отступом внутрь от заклёпок
        charView.nameText = MakeText(leftRt, "Name", "СБОРЩИК", 34, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(260, 44), new Vector2(-95, 336));
        charView.classText = MakeText(leftRt, "Class", "КОЛЛЕКТОР", 18, UITheme.Parchment, TextAlignmentOptions.MidlineRight, FontStyles.Normal, new Vector2(210, 24), new Vector2(120, 348));
        charView.rankText = MakeText(leftRt, "Rank", "СИГМА НА СУЕТЕ", 15, Orange, TextAlignmentOptions.MidlineRight, FontStyles.Bold, new Vector2(210, 22), new Vector2(120, 326));

        // Подиум куклы — аккуратная тёмная ниша без толстой рамы с заклёпками
        var dollBg = MakeInnerBox(leftRt, "DollBackdrop", new Vector2(220, 430), new Vector2(0, 35), InnerBgColor);
        dollBg.GetComponent<Image>().raycastTarget = false;

        var dollRt = NewRect("Doll", leftRt, new Vector2(210, 420), new Vector2(0, 35));
        var dollImg = dollRt.gameObject.AddComponent<Image>();
        dollImg.sprite = dollSprite;
        dollImg.preserveAspect = true;
        dollImg.raycastTarget = false;
        dollImg.enabled = dollSprite != null;
        charView.doll = dollImg;

        var equipSlots = new EquipSlotView[Layout.Length];
        for (int i = 0; i < Layout.Length; i++)
            equipSlots[i] = MakeEquipSlot(leftRt, Layout[i]);
        charView.slots = equipSlots;

        // ====================================================================
        // 3. СРЕДНЕЕ ОКНО: СУМКА 5x5 + ФИЛЬТРЫ + ПОИСК + ВЕС (680 x 800)
        // ====================================================================
        var midRt = MakeMainFrame(root, "BagPanel", new Vector2(680, panelH), new Vector2(0, panelY));

        // Вкладки фильтров (ширина 596px по центру окна, отступ от верхней рамки)
        string[] tabNames = { "ВСЕ", "ОБОРУДОВАНИЕ", "РАСХОДНИКИ", "МАТЕРИАЛЫ", "КВЕСТЫ" };
        float[] tabWidths = { 76f, 148f, 132f, 126f, 90f };
        float tabX = -298f;
        screenView.filterButtons = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            float w = tabWidths[i];
            var b = MakeFlatButton(midRt, "Tab_" + tabNames[i], tabNames[i], 13, new Vector2(w, 34), new Vector2(tabX + w * 0.5f, 340), i == 0 ? Orange : BtnBgColor);
            screenView.filterButtons[i] = b;
            tabX += w + 6f;
        }

        // Поле поиска и сортировка (чистые внутренние плашки без сплюснутых заклёпок)
        screenView.searchInput = MakeSearchField(midRt, new Vector2(400, 36), new Vector2(-98, 294));
        var sortBox = MakeInnerBox(midRt, "SortBox", new Vector2(184, 36), new Vector2(206, 294), InnerBgColor);
        MakeText(sortBox, "SortLabel", "Новое   ▼", 15, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(170, 32), Vector2.zero);

        // Сетка 5x5 (внутренняя тёмная ниша 596x576)
        var gridBg = MakeInnerBox(midRt, "GridContainer", new Vector2(596, 576), new Vector2(0, -24), InnerBgColor);
        int cols = 5, rows = 5;
        screenView.bagSlots = new EquipSlotView[cols * rows];
        screenView.bagCountLabels = new TMP_Text[cols * rows];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                float x = (c - 2) * 112f;
                float y = (2 - r) * 110f;
                screenView.bagSlots[idx] = MakeBagSlot(gridBg, idx, new Vector2(x, y), out TMP_Text countLbl);
                screenView.bagCountLabels[idx] = countLbl;
            }
        }

        // Полоска ВЕС (с отступом от нижней рамки)
        const float weightY = -346f;
        MakeText(midRt, "WeightLabel", "ВЕС", 16, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, new Vector2(70, 26), new Vector2(-262, weightY));
        var barBg = MakeInnerBox(midRt, "WeightBarBg", new Vector2(360, 16), new Vector2(-32, weightY), InnerBgColor);
        var barFillRt = NewRect("WeightBarFill", barBg, new Vector2(354, 10), Vector2.zero);
        var barFill = barFillRt.gameObject.AddComponent<Image>();
        barFill.color = Orange;
        barFill.type = Image.Type.Simple; // Управляется через rectTransform.anchorMax в InventoryScreenView
        screenView.weightFillBar = barFill;

        screenView.weightValueText = MakeText(midRt, "WeightValue", "0.0 / 50 кг", 16, UITheme.Parchment, TextAlignmentOptions.MidlineRight, FontStyles.Bold, new Vector2(140, 26), new Vector2(228, weightY));

        // ====================================================================
        // 4. ПРАВОЕ ОКНО: ОПИСАНИЕ И КНОПКИ (540 x 800)
        // ====================================================================
        var rightRt = MakeMainFrame(root, "DetailsPanel", new Vector2(540, panelH), new Vector2(640, panelY));

        // Заглушка, когда в сумке пусто и ничего не выбрано
        var emptyHint = MakeText(rightRt, "EmptyStateHint",
            "ПРЕДМЕТ НЕ ВЫБРАН\n\n<size=16><color=#8E929B>Выберите предмет в сумке или в слотах экипировки,\nчтобы посмотреть его характеристики.</color></size>",
            20, MutedText, TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(440, 160), new Vector2(0, 40));
        screenView.emptyStateObject = emptyHint.gameObject;

        var contentRt = NewRect("DetailsContent", rightRt, new Vector2(464, 720), Vector2.zero);
        screenView.detailsContent = contentRt.gameObject;

        // Большой слот иконки предмета
        var bigSlot = MakeSlotFrame(contentRt, "ItemPreviewBox", new Vector2(136, 136), new Vector2(-160, 275));
        var bigIcon = MakeSlotIcon(bigSlot, new Vector2(106, 106));
        screenView.detailIcon = bigIcon;

        screenView.detailTitle = MakeText(contentRt, "DetailTitle", "БАТИНА МОНТИРОВКА", 22, Orange, TextAlignmentOptions.TopLeft, FontStyles.Bold, new Vector2(290, 58), new Vector2(72, 314));
        screenView.detailSubtitle = MakeText(contentRt, "DetailSubtitle", "(Обычное)\nОружие ближнего боя", 16, MutedText, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(290, 52), new Vector2(72, 254));

        screenView.detailDescription = MakeText(contentRt, "DetailDescription",
            "Описание предмета.",
            16, UITheme.Parchment, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(456, 135), new Vector2(0, 118));

        var statsBg = MakeInnerBox(contentRt, "StatsBackdrop", new Vector2(460, 145), new Vector2(0, -32), InnerBgColor);
        screenView.detailStats = MakeText(statsBg, "DetailStats", "ХАРАКТЕРИСТИКИ", 16, UITheme.Parchment, TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(432, 128), Vector2.zero);

        // 4 кнопки действий (аккуратные кнопки внутри правой панели)
        screenView.btnEquip = MakeFlatButton(contentRt, "BtnEquip", "ЭКИПИРОВАТЬ", 18, new Vector2(456, 44), new Vector2(0, -152), BtnBgColor, out screenView.btnEquipLabel);
        screenView.btnUse = MakeFlatButton(contentRt, "BtnUse", "ИСПОЛЬЗОВАТЬ", 18, new Vector2(456, 44), new Vector2(0, -206), BtnBgColor);
        screenView.btnDrop = MakeFlatButton(contentRt, "BtnDrop", "ВЫБРОСИТЬ", 18, new Vector2(456, 44), new Vector2(0, -260), BtnBgColor);
        screenView.btnCompare = MakeFlatButton(contentRt, "BtnCompare", "СРАВНИТЬ", 18, new Vector2(456, 44), new Vector2(0, -314), BtnBgColor);

        // ====================================================================
        // 5. НИЖНЯЯ ПАНЕЛЬ ПОДСКАЗОК КЛАВИШ (Сдвинута левее, чтобы не задевать деньги!)
        // ====================================================================
        var hintsBar = MakeInnerBox(root, "BottomHints", new Vector2(540, 44), new Vector2(-180, -482), PanelBgColor);
        MakeText(hintsBar, "HintsText", "[E] Экипировать    [R] Сравнить    [X] Выбросить    [Esc] Закрыть",
                 15, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(520, 36), Vector2.zero);

        var rpg = Object.FindFirstObjectByType<RPGGameManager>();
        if (rpg != null)
        {
            Undo.RecordObject(rpg, "Link InventoryScreenView to RPGGameManager");
            rpg.inventoryUI = screenView;
            EditorUtility.SetDirty(rpg);
        }

        Selection.activeGameObject = screenRt.gameObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Выровненный инвентарь (3 окна) успешно построен!");
    }

    // ------------------------------------------------------------------ слоты
    private EquipSlotView MakeEquipSlot(RectTransform parent, SlotPos sp)
    {
        var slotRt = MakeSlotFrame(parent, "Slot_" + sp.slot, new Vector2(88, 88), sp.pos);
        var sel = MakeSelectionFrame(slotRt, new Vector2(92, 92));
        var icon = MakeSlotIcon(slotRt, new Vector2(68, 68));
        var label = MakeText(slotRt, "Label", EquipSlotInfo.Label(sp.slot), 14, UITheme.Parchment,
                             TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(124, 20), new Vector2(0, -56));

        var v = slotRt.gameObject.AddComponent<EquipSlotView>();
        v.slot = sp.slot;
        v.icon = icon;
        v.selectedFrame = sel;
        v.label = label;
        return v;
    }

    private EquipSlotView MakeBagSlot(RectTransform parent, int index, Vector2 pos, out TMP_Text countLabel)
    {
        var slotRt = MakeSlotFrame(parent, "BagSlot_" + index, new Vector2(102, 102), pos);
        var sel = MakeSelectionFrame(slotRt, new Vector2(106, 106));
        var icon = MakeSlotIcon(slotRt, new Vector2(78, 78));
        countLabel = MakeText(slotRt, "Count", "", 15, UITheme.Parchment, TextAlignmentOptions.BottomRight, FontStyles.Bold, new Vector2(84, 24), new Vector2(0, -30));

        var v = slotRt.gameObject.AddComponent<EquipSlotView>();
        v.slot = EquipSlot.None;
        v.icon = icon;
        v.selectedFrame = sel;
        return v;
    }

    private RectTransform MakeSlotFrame(RectTransform parent, string name, Vector2 size, Vector2 pos)
    {
        var rt = NewRect(name, parent, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        if (slotSprite != null)
        {
            img.sprite = slotSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = SlotBgColor;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.38f, 0.44f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return rt;
    }

    private Image MakeSelectionFrame(RectTransform slotRt, Vector2 size)
    {
        var selRt = NewRect("Selected", slotRt, size, Vector2.zero);
        var sel = selRt.gameObject.AddComponent<Image>();
        if (selectedSprite != null)
        {
            sel.sprite = selectedSprite;
            sel.type = Image.Type.Sliced;
            sel.color = Color.white;
        }
        else if (slotSprite != null)
        {
            sel.sprite = slotSprite;
            sel.type = Image.Type.Sliced;
            sel.fillCenter = false;
            sel.color = Orange;
        }
        else
        {
            sel.color = new Color(Orange.r, Orange.g, Orange.b, 0.35f);
        }
        sel.raycastTarget = false;
        sel.enabled = false;
        return sel;
    }

    private static Image MakeSlotIcon(RectTransform slotRt, Vector2 size)
    {
        var iconRt = NewRect("Icon", slotRt, size, Vector2.zero);
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        return icon;
    }

    // ------------------------------------------------------------------ Панели и Кнопки
    // Главная стальная рамка с заклёпками (только для крупных окон!)
    private RectTransform MakeMainFrame(RectTransform parent, string name, Vector2 size, Vector2 pos)
    {
        var rt = NewRect(name, parent, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = PanelBgColor;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.38f, 0.43f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
        return rt;
    }

    // Внутренняя тёмная плашка с тонкой обводкой (для ниш, поиска, кнопок)
    private static RectTransform MakeInnerBox(RectTransform parent, string name, Vector2 size, Vector2 pos, Color bgColor)
    {
        var rt = NewRect(name, parent, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bgColor;
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.28f, 0.31f, 0.36f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return rt;
    }

    private Button MakeFlatButton(RectTransform parent, string name, string caption, float fontSize, Vector2 size, Vector2 pos, Color bgColor)
    {
        return MakeFlatButton(parent, name, caption, fontSize, size, pos, bgColor, out _);
    }

    private Button MakeFlatButton(RectTransform parent, string name, string caption, float fontSize, Vector2 size, Vector2 pos, Color bgColor, out TMP_Text labelOut)
    {
        var rt = MakeInnerBox(parent, name, size, pos, bgColor);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();
        labelOut = MakeText(rt, "Label", caption, fontSize, UITheme.Parchment, TextAlignmentOptions.Center, FontStyles.Bold, size - new Vector2(12, 4), Vector2.zero);
        return btn;
    }

    private TMP_InputField MakeSearchField(RectTransform parent, Vector2 size, Vector2 pos)
    {
        var rt = MakeInnerBox(parent, "SearchInput", size, pos, InnerBgColor);
        var textArea = NewRect("TextArea", rt, size - new Vector2(28, 8), Vector2.zero);
        var placeholder = MakeText(textArea, "Placeholder", "Поиск предметов...", 15, MutedText, TextAlignmentOptions.MidlineLeft, FontStyles.Italic, size - new Vector2(28, 8), Vector2.zero);
        var text = MakeText(textArea, "Text", "", 15, UITheme.Parchment, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, size - new Vector2(28, 8), Vector2.zero);

        var input = rt.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
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