using UnityEngine;

/// <summary>
/// Единый стиль интерфейса: «тёмный бархат + кровь + старое золото».
/// Текстуры генерируются кодом — никаких ассетов не нужно.
/// Чтобы перекрасить всю игру — меняйте палитру ниже.
/// </summary>
public static class UITheme
{
    // ---------------- ПАЛИТРА ----------------
    public static readonly Color Ink       = new Color(0.07f, 0.05f, 0.08f, 0.96f); // фон окон
    public static readonly Color InkLight  = new Color(0.14f, 0.10f, 0.13f, 0.98f); // фон кнопок
    public static readonly Color InkHover  = new Color(0.22f, 0.13f, 0.16f, 1f);
    public static readonly Color Blood     = new Color(0.46f, 0.07f, 0.10f, 1f);
    public static readonly Color BloodHi   = new Color(0.66f, 0.12f, 0.16f, 1f);
    public static readonly Color Gold      = new Color(0.79f, 0.64f, 0.30f, 1f);
    public static readonly Color GoldDim   = new Color(0.45f, 0.36f, 0.17f, 1f);
    public static readonly Color Parchment = new Color(0.93f, 0.89f, 0.80f, 1f);
    public static readonly Color Good      = new Color(0.55f, 0.80f, 0.43f, 1f);

    // для rich text
    public const string GoldHex = "C9A44D";
    public const string GoodHex = "8CCB6E";
    public const string TextHex = "EBE3CC";
    public const string MutedHex = "9E8F8F";

    // ---------------- СТИЛИ ----------------
    public static GUIStyle Panel, Box, Btn, BtnAccent, ChoiceBtn, ChoiceAccent;
    public static GUIStyle Title, Heading, Body, Small, Center, Kicker, NamePlate;

    private static Texture2D tPanel, tBtn, tBtnHi, tBtnDown, tAcc, tAccHi, tAccDown, tPlate;

    // Виртуальный размер экрана после масштабирования (используйте внутри Begin/End)
    public static float W, H;
    private static Matrix4x4 prevMatrix;

    public static void Init()
    {
        if (Panel != null && tPanel != null) return; // уже создано

        tPanel   = Make(Ink, GoldDim);
        tBtn     = Make(InkLight, GoldDim);
        tBtnHi   = Make(InkHover, Gold);
        tBtnDown = Make(Ink, Gold);
        tAcc     = Make(Blood, Gold);
        tAccHi   = Make(BloodHi, Gold);
        tAccDown = Make(Ink, BloodHi);
        tPlate   = Make(Blood, Gold);

        var border = new RectOffset(3, 3, 3, 3);

        Panel = new GUIStyle(GUI.skin.box) { border = border, padding = new RectOffset(14, 14, 12, 12) };
        Bg(Panel, tPanel, tPanel, tPanel);

        // «Старые» стили HUD (Box / Btn) — ими же перекрашивается инвентарь, магазин и карта
        Box = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.UpperCenter, border = border, richText = true };
        Bg(Box, tPanel, tPanel, tPanel);
        Col(Box, Parchment, Parchment);

        Btn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14, alignment = TextAnchor.MiddleCenter, border = border,
            padding = new RectOffset(10, 10, 6, 6), richText = true, wordWrap = true
        };
        Bg(Btn, tBtn, tBtnHi, tBtnDown);
        Col(Btn, Parchment, Color.white);

        BtnAccent = new GUIStyle(Btn);
        Bg(BtnAccent, tAcc, tAccHi, tAccDown);
        Col(BtnAccent, Parchment, Color.white);

        ChoiceBtn = new GUIStyle(Btn) { fontSize = 17, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 14, 6, 6) };
        ChoiceAccent = new GUIStyle(BtnAccent) { fontSize = 17, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 14, 6, 6) };

        Title = Label(24, FontStyle.Bold, TextAnchor.MiddleLeft, Gold);
        Heading = Label(14, FontStyle.Bold, TextAnchor.MiddleLeft, Gold);
        Body = Label(17, FontStyle.Normal, TextAnchor.UpperLeft, Parchment);
        Small = Label(14, FontStyle.Normal, TextAnchor.UpperLeft, Parchment);
        Center = Label(16, FontStyle.Normal, TextAnchor.MiddleCenter, Parchment);
        Kicker = Label(12, FontStyle.Bold, TextAnchor.MiddleCenter, Gold);

        NamePlate = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = border
        };
        Bg(NamePlate, tPlate, tPlate, tPlate);
        Col(NamePlate, Parchment, Parchment);
    }

    // ---------------- МАСШТАБ ПОД РАЗНЫЕ ЭКРАНЫ ----------------
    public static void Begin()
    {
        Init();
        prevMatrix = GUI.matrix;
        float s = Mathf.Clamp(Screen.height / 810f, 0.75f, 3f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        W = Screen.width / s;
        H = Screen.height / s;
    }

    public static void End() { GUI.matrix = prevMatrix; }

    // ---------------- РИСОВАЛКИ ----------------
    public static void Bar(Rect r, float t, Color fill)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f * old.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(fill.r, fill.g, fill.b, old.a);
        GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(t), r.height - 2), Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Line(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // ---------------- ВНУТРЕННЕЕ ----------------
    private static GUIStyle Label(int size, FontStyle fs, TextAnchor a, Color c)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, alignment = a, wordWrap = true, richText = true };
        Col(s, c, c);
        return s;
    }

    private static Texture2D Make(Color fill, Color border)
    {
        const int size = 8;
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = (x == 0 || y == 0 || x == size - 1 || y == size - 1) ? border : fill;
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    private static void Bg(GUIStyle s, Texture2D n, Texture2D h, Texture2D a)
    {
        s.normal.background = n; s.focused.background = n; s.hover.background = h; s.active.background = a;
        s.onNormal.background = n; s.onFocused.background = n; s.onHover.background = h; s.onActive.background = a;
    }

    private static void Col(GUIStyle s, Color n, Color h)
    {
        s.normal.textColor = n; s.focused.textColor = n; s.hover.textColor = h; s.active.textColor = h;
        s.onNormal.textColor = n; s.onFocused.textColor = n; s.onHover.textColor = h; s.onActive.textColor = h;
    }
}
