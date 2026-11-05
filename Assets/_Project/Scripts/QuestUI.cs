using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Журнал дел (клавиша J), трекер в углу экрана и всплывающие уведомления.</summary>
public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;
    public static bool JournalOpen;

    public static void ToggleJournal() { JournalOpen = !JournalOpen; }
    public static void CloseJournal() { JournalOpen = false; }

    private class ToastItem { public string title, sub; public float t; }

    private readonly List<ToastItem> toasts = new List<ToastItem>();
    private bool showCompleted;
    private string selectedId;
    private Vector2 listScroll, detailScroll;

    void Awake() { Instance = this; JournalOpen = false; }
    void OnEnable() { QuestManager.Toast += AddToast; }
    void OnDisable() { QuestManager.Toast -= AddToast; }

    private void AddToast(string title, string sub)
    {
        toasts.Add(new ToastItem { title = title, sub = sub });
        if (toasts.Count > 4) toasts.RemoveAt(0);
    }

    private static bool Blocked()
    {
        bool dlg = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        bool battle = BattleManager.Instance != null && BattleManager.Instance.battleActive;
        return dlg || battle;
    }

    void Update()
    {
        bool j = false, esc = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            j = Keyboard.current.jKey.wasPressedThisFrame;
            esc = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#else
        j = Input.GetKeyDown(KeyCode.J);
        esc = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (Blocked()) JournalOpen = false;
        else if (j) JournalOpen = !JournalOpen;
        if (esc) JournalOpen = false;

        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            toasts[i].t += Time.unscaledDeltaTime;
            if (toasts[i].t > 4f) toasts.RemoveAt(i);
        }
    }

    // ======================================================================
    void OnGUI()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.battleActive) return;

        UITheme.Begin();
        GUI.depth = -50;

        if (JournalOpen) DrawJournal();
        else if (DialogueManager.Instance == null || !DialogueManager.Instance.IsOpen) DrawTracker();

        DrawToasts();
        UITheme.End();
    }

    // ---------------------------------------------------------------- всплывашки
    private void DrawToasts()
    {
        Color old = GUI.color;
        for (int i = 0; i < toasts.Count; i++)
        {
            var it = toasts[i];
            float fadeIn = Mathf.Clamp01(it.t / 0.25f);
            float a = Mathf.Clamp01(Mathf.Min(fadeIn, (4f - it.t) / 0.6f));
            float slide = (1f - fadeIn) * -16f;

            var r = new Rect(UITheme.W * 0.5f - 230f, 80f + i * 66f + slide, 460f, 58f);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Box(r, GUIContent.none, UITheme.Panel);
            GUI.Label(new Rect(r.x + 14f, r.y + 6f, r.width - 28f, 18f), it.title, UITheme.Kicker);
            GUI.Label(new Rect(r.x + 14f, r.y + 24f, r.width - 28f, 28f), it.sub, UITheme.Center);
        }
        GUI.color = old;
    }

    // ---------------------------------------------------------------- трекер
    private void DrawTracker()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        var sb = new StringBuilder();
        sb.Append("<color=#" + UITheme.MutedHex + ">ТЕКУЩИЕ ДЕЛА  [J]</color>");

        int count = 0;
        foreach (var rt in qm.Ordered)
        {
            if (rt.state != QuestState.Active && rt.state != QuestState.ReadyToTurnIn) continue;
            if (++count > 3) break;

            sb.Append("\n\n<color=#" + UITheme.GoldHex + "><b>" + rt.data.title + "</b></color>");

            if (rt.state == QuestState.ReadyToTurnIn)
            {
                sb.Append("\n<color=#" + UITheme.GoodHex + ">\u25BA " + rt.data.turnInHint + "</color>");
                continue;
            }

            var objs = rt.data.objectives;
            for (int j = 0; objs != null && j < objs.Length; j++)
            {
                bool done = qm.IsObjectiveDone(rt, j);
                int req = QuestManager.Required(objs[j]);
                string counter = req > 1 ? "  " + qm.GetProgress(rt, j) + "/" + req : "";
                sb.Append("\n<color=#" + (done ? UITheme.GoodHex : UITheme.TextHex) + ">"
                          + (done ? "\u25A0 " : "\u25A1 ") + QuestManager.ObjectiveLabel(objs[j]) + counter + "</color>");
            }
        }
        if (count == 0) return;

        string text = sb.ToString();
        float w = 310f;
        float th = UITheme.Small.CalcHeight(new GUIContent(text), w - 28f);
        var r = new Rect(14f, 64f, w, th + 24f);

        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.92f);
        GUI.Box(r, GUIContent.none, UITheme.Panel);
        GUI.color = old;
        GUI.Label(new Rect(r.x + 14f, r.y + 11f, w - 28f, th), text, UITheme.Small);
    }

    // ---------------------------------------------------------------- журнал
    private void DrawJournal()
    {
        var qm = QuestManager.Instance;
        float w = Mathf.Min(1000f, UITheme.W - 40f);
        float h = Mathf.Min(620f, UITheme.H - 120f);
        var r = new Rect((UITheme.W - w) * 0.5f, (UITheme.H - h) * 0.5f + 30f, w, h);

        GUI.Box(r, GUIContent.none, UITheme.Panel);
        GUI.Label(new Rect(r.x + 24f, r.y + 12f, 500f, 36f), "ПАПКА ДЕЛ", UITheme.Title);
        if (GUI.Button(new Rect(r.xMax - 52f, r.y + 12f, 38f, 34f), "X", UITheme.Btn)) JournalOpen = false;

        if (GUI.Button(new Rect(r.x + 24f, r.y + 54f, 140f, 30f), "Открытые", showCompleted ? UITheme.Btn : UITheme.BtnAccent)) showCompleted = false;
        if (GUI.Button(new Rect(r.x + 170f, r.y + 54f, 140f, 30f), "Закрытые", showCompleted ? UITheme.BtnAccent : UITheme.Btn)) showCompleted = true;
        UITheme.Line(new Rect(r.x + 20f, r.y + 92f, w - 40f, 1f), UITheme.GoldDim);

        if (qm == null) return;

        // ---- список
        var list = new List<QuestRuntime>();
        foreach (var rt in qm.Ordered)
            if ((rt.state == QuestState.Completed) == showCompleted) list.Add(rt);

        QuestRuntime sel = null;
        foreach (var rt in list) if (rt.data.Id == selectedId) sel = rt;
        if (sel == null && list.Count > 0) { sel = list[0]; selectedId = sel.data.Id; }

        GUILayout.BeginArea(new Rect(r.x + 20f, r.y + 100f, 290f, h - 116f));
        listScroll = GUILayout.BeginScrollView(listScroll);
        foreach (var rt in list)
        {
            string mark = rt.state == QuestState.ReadyToTurnIn ? "\u25BA " : "";
            bool isSel = rt == sel;
            if (GUILayout.Button(mark + rt.data.title, isSel ? UITheme.ChoiceAccent : UITheme.ChoiceBtn, GUILayout.Height(42f)))
                selectedId = rt.data.Id;
        }
        if (list.Count == 0) GUILayout.Label("<color=#" + UITheme.MutedHex + ">Здесь пока пусто.</color>", UITheme.Small);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // ---- подробности
        if (sel == null) return;
        var q = sel.data;

        GUILayout.BeginArea(new Rect(r.x + 330f, r.y + 100f, w - 350f, h - 116f));
        detailScroll = GUILayout.BeginScrollView(detailScroll);

        GUILayout.Label(q.title, UITheme.Title);

        string stText; string stHex;
        switch (sel.state)
        {
            case QuestState.ReadyToTurnIn: stText = "ГОТОВО К СДАЧЕ"; stHex = UITheme.GoodHex; break;
            case QuestState.Completed: stText = "ДЕЛО ЗАКРЫТО"; stHex = UITheme.MutedHex; break;
            default: stText = "В РАБОТЕ"; stHex = UITheme.GoldHex; break;
        }
        GUILayout.Label("<color=#" + stHex + "><b>" + stText + "</b></color>", UITheme.Small);
        GUILayout.Space(6f);
        GUILayout.Label(q.description, UITheme.Body);

        GUILayout.Space(12f);
        GUILayout.Label("ЗАДАЧИ", UITheme.Heading);
        var objs = q.objectives;
        for (int j = 0; objs != null && j < objs.Length; j++)
        {
            bool done = qm.IsObjectiveDone(sel, j) || sel.state == QuestState.Completed;
            int req = QuestManager.Required(objs[j]);
            int p = sel.state == QuestState.Completed ? req : qm.GetProgress(sel, j);

            GUILayout.Label("<color=#" + (done ? UITheme.GoodHex : UITheme.TextHex) + ">"
                            + (done ? "\u25A0  " : "\u25A1  ") + QuestManager.ObjectiveLabel(objs[j]) + "   " + p + "/" + req + "</color>", UITheme.Body);
            Rect br = GUILayoutUtility.GetRect(10f, 6f, GUILayout.ExpandWidth(true));
            UITheme.Bar(br, p / (float)req, done ? UITheme.Good : UITheme.Gold);
            GUILayout.Space(4f);
        }

        if (sel.state == QuestState.ReadyToTurnIn)
        {
            GUILayout.Space(8f);
            GUILayout.Label("<color=#" + UITheme.GoldHex + ">\u25BA " + q.turnInHint + "</color>", UITheme.Body);
        }

        if (sel.state == QuestState.Completed && !string.IsNullOrEmpty(q.completionText))
        {
            GUILayout.Space(8f);
            GUILayout.Label("<i>" + q.completionText + "</i>", UITheme.Body);
        }

        GUILayout.Space(12f);
        GUILayout.Label("НАГРАДА", UITheme.Heading);
        var rw = new StringBuilder();
        if (q.rewardXP > 0) rw.Append(q.rewardXP + " XP    ");
        if (q.rewardGold > 0) rw.Append(q.rewardGold + " зол.    ");
        if (q.rewardItems != null)
            foreach (var it in q.rewardItems) if (it != null) rw.Append(it.itemName + "    ");
        if (rw.Length == 0) rw.Append("—");
        GUILayout.Label(rw.ToString(), UITheme.Body);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
