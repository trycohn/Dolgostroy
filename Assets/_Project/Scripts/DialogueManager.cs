using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public bool IsOpen { get; private set; }

    [Tooltip("Символов в секунду (эффект печатной машинки)")]
    public float typewriterSpeed = 90f;

    private WorldInteractable npc;
    private DialogueData data;
    private DialogueNode node;

    private string fullText = "";
    private int totalVisible;
    private float shown;
    private readonly List<DialogueChoice> visible = new List<DialogueChoice>();

    void Awake() { Instance = this; }

    // ------------------------------------------------------------------ открыть / закрыть
    public void Open(WorldInteractable n)
    {
        if (n == null) return;
        OpenData(n, n.PickDialogue());
    }

    public void OpenData(WorldInteractable n, DialogueData d)
    {
        if (n == null || d == null || d.nodes == null || d.nodes.Length == 0) return;

        npc = n;
        data = d;
        IsOpen = true;

        // Разговор засчитывается ДО перехода к первой реплике, но ПОСЛЕ выбора варианта диалога
        if (QuestManager.Instance != null) QuestManager.Instance.ReportTalk(n.npcId);

        GoTo(d.GetStartNode());
    }

    public void Close()
    {
        IsOpen = false;
        npc = null;
        data = null;
        node = null;
    }

    // ------------------------------------------------------------------ логика узлов
    private void GoTo(DialogueNode n)
    {
        if (n == null) { Close(); return; }

        node = n;
        shown = 0f;
        fullText = Format(n.text);
        totalVisible = CountVisible(fullText);

        visible.Clear();
        if (n.choices != null)
            foreach (var c in n.choices)
                if (c != null && (c.condition == null || c.condition.IsMet())) visible.Add(c);
    }

    private void Advance()
    {
        if (node == null) return;
        if (string.IsNullOrEmpty(node.nextNodeId)) Close();
        else GoTo(data.FindNode(node.nextNodeId));
    }

    private void Choose(DialogueChoice c)
    {
        var rpg = RPGGameManager.Instance;
        var qm = QuestManager.Instance;

        if (c.goldCost > 0)
        {
            if (rpg == null || rpg.gold < c.goldCost) return;
            rpg.gold -= c.goldCost;
        }

        if (!string.IsNullOrEmpty(c.setFlag) && qm != null) qm.SetFlag(c.setFlag);

        switch (c.action)
        {
            case DialogueAction.StartQuest:
                if (qm != null) qm.StartQuest(c.quest);
                break;

            case DialogueAction.CompleteQuest:
                if (qm != null) qm.TryCompleteQuest(c.quest);
                break;

            case DialogueAction.StartBattle:
                {
                    var n = npc;
                    Close();
                    if (n != null && BattleManager.Instance != null)
                        BattleManager.Instance.StartBattle(n.gameObject, n.titleName, n.enemyHP, n.difficulty,
                                                           n.enemyBattleSprite, n.customBattleBackground);
                    return;
                }

            case DialogueAction.OpenShop:
                {
                    var n = npc;
                    Close();
                    if (rpg != null) rpg.OpenShop(n);
                    return;
                }
        }

        if (string.IsNullOrEmpty(c.nextNodeId)) Close();
        else GoTo(data.FindNode(c.nextNodeId));
    }

    // ------------------------------------------------------------------ Update
    void Update()
    {
        if (!IsOpen) return;
        if (npc == null) { Close(); return; } // NPC уничтожили

        if (shown < totalVisible) shown += Time.unscaledDeltaTime * typewriterSpeed;
    }

    // ------------------------------------------------------------------ рисование
    void OnGUI()
    {
        if (!IsOpen || node == null) return;
        if (BattleManager.Instance != null && BattleManager.Instance.battleActive) return;

        UITheme.Begin();
        GUI.depth = -100; // поверх остального интерфейса

        bool done = shown >= totalVisible;
        int rows = visible.Count > 0 ? visible.Count : 1;

        float pw = Mathf.Min(1100f, UITheme.W - 60f);
        float textH = 120f, rowH = 46f;
        float ph = 28f + textH + 14f + rows * rowH + 14f;
        var panel = new Rect((UITheme.W - pw) * 0.5f, UITheme.H - ph - 24f, pw, ph);

        DialogueChoice picked = null;
        bool advance = false;

        // ---- ввод
        Event e = Event.current;
        if (e.type == EventType.MouseDown && !done && panel.Contains(e.mousePosition))
        {
            shown = totalVisible;
            e.Use();
        }
        if (e.type == EventType.KeyDown)
        {
            bool confirm = e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter;
            if (confirm && !done) { shown = totalVisible; e.Use(); }
            else if (confirm && done && visible.Count == 0) { advance = true; e.Use(); }
            else if (done && e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            {
                int idx = e.keyCode - KeyCode.Alpha1;
                if (idx < visible.Count && CanAfford(visible[idx])) { picked = visible[idx]; e.Use(); }
            }
        }

        // ---- окно
        GUI.Box(panel, GUIContent.none, UITheme.Panel);

        string speaker = string.IsNullOrEmpty(node.speaker) ? npc.titleName : Format(node.speaker);
        float plateW = Mathf.Max(220f, UITheme.NamePlate.CalcSize(new GUIContent(speaker)).x + 44f);
        GUI.Box(new Rect(panel.x + 24f, panel.y - 22f, plateW, 40f), speaker, UITheme.NamePlate);

        GUI.Label(new Rect(panel.x + 28f, panel.y + 28f, pw - 56f, textH), Typed(fullText, (int)shown), UITheme.Body);

        // ---- варианты ответа (появляются, когда текст допечатан)
        if (done)
        {
            float y0 = panel.y + 28f + textH + 14f;

            if (visible.Count == 0)
            {
                string lbl = string.IsNullOrEmpty(node.nextNodeId) ? "Закрыть" : "Далее  >";
                if (GUI.Button(new Rect(panel.x + 28f, y0, pw - 56f, 40f), lbl, UITheme.ChoiceBtn)) advance = true;
            }
            else
            {
                for (int i = 0; i < visible.Count; i++)
                {
                    var c = visible[i];
                    string label = (i + 1) + ".  " + Format(c.text);
                    if (c.goldCost > 0) label += "   <color=#" + UITheme.GoldHex + ">(-" + c.goldCost + " зол.)</color>";

                    bool battle = c.action == DialogueAction.StartBattle;
                    GUI.enabled = CanAfford(c);
                    if (GUI.Button(new Rect(panel.x + 28f, y0 + i * rowH, pw - 56f, 40f), label,
                                   battle ? UITheme.ChoiceAccent : UITheme.ChoiceBtn))
                        picked = c;
                    GUI.enabled = true;
                }
            }
        }

        UITheme.End();

        // Меняем состояние только ПОСЛЕ отрисовки, чтобы не ломать список во время цикла
        if (picked != null) Choose(picked);
        else if (advance) Advance();
    }

    // ------------------------------------------------------------------ утилиты
    private bool CanAfford(DialogueChoice c)
    {
        if (c.goldCost <= 0) return true;
        return RPGGameManager.Instance != null && RPGGameManager.Instance.gold >= c.goldCost;
    }

    private string Format(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var rpg = RPGGameManager.Instance;
        if (rpg != null)
            s = s.Replace("{player}", rpg.characterName).Replace("{gold}", rpg.gold.ToString());
        return s;
    }

    // Считаем только видимые символы (rich-text теги не считаются)
    private static int CountVisible(string s)
    {
        int count = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '<')
            {
                int j = s.IndexOf('>', i);
                if (j > 0) { i = j; continue; }
            }
            count++;
        }
        return count;
    }

    // Печатная машинка: целиком пропускает теги, чтобы не показать «половину» тега
    private static string Typed(string s, int n)
    {
        var sb = new StringBuilder();
        int c = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '<')
            {
                int j = s.IndexOf('>', i);
                if (j > 0) { sb.Append(s, i, j - i + 1); i = j; continue; }
            }
            if (c >= n) break;
            sb.Append(s[i]);
            c++;
        }
        return sb.ToString();
    }
}
