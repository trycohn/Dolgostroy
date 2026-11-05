using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestRuntime
{
    public QuestData data;
    public QuestState state;
    public int[] progress;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    /// <summary>(заголовок, подзаголовок) — QuestUI показывает это как всплывашку.</summary>
    public static event Action<string, string> Toast;

    [Header("Квесты, которые выдаются сразу при старте игры (необязательно)")]
    public QuestData[] startingQuests;

    private readonly Dictionary<string, QuestRuntime> map = new Dictionary<string, QuestRuntime>();
    private readonly List<QuestRuntime> ordered = new List<QuestRuntime>();
    private readonly HashSet<string> flags = new HashSet<string>();
    private float refreshTimer;

    public IReadOnlyList<QuestRuntime> Ordered { get { return ordered; } }

    void Awake() { Instance = this; }

    void Start()
    {
        if (startingQuests != null)
            foreach (var q in startingQuests) StartQuest(q);
    }

    void Update()
    {
        // Collect-задачи считаются по инвентарю, поэтому периодически перепроверяем
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < 0.5f) return;
        refreshTimer = 0f;

        for (int i = 0; i < ordered.Count; i++)
        {
            var rt = ordered[i];
            if ((rt.state == QuestState.Active || rt.state == QuestState.ReadyToTurnIn) && HasCollect(rt))
                Evaluate(rt);
        }
    }

    // ---------------------------------------------------------------- флаги
    public bool HasFlag(string f) { return !string.IsNullOrEmpty(f) && flags.Contains(f); }
    public void SetFlag(string f) { if (!string.IsNullOrEmpty(f)) flags.Add(f); }

    // ---------------------------------------------------------------- состояние
    public QuestState GetState(QuestData q)
    {
        if (q == null) return QuestState.NotStarted;
        QuestRuntime rt;
        return map.TryGetValue(q.Id, out rt) ? rt.state : QuestState.NotStarted;
    }

    public bool StartQuest(QuestData q)
    {
        if (q == null || map.ContainsKey(q.Id)) return false;

        var rt = new QuestRuntime
        {
            data = q,
            state = QuestState.Active,
            progress = new int[q.objectives != null ? q.objectives.Length : 0]
        };
        map[q.Id] = rt;
        ordered.Add(rt);

        Notify("НОВОЕ ДЕЛО", q.title);
        Evaluate(rt);
        return true;
    }

    /// <summary>Сдача квеста (вызывается из диалога). Работает только из состояния ReadyToTurnIn.</summary>
    public bool TryCompleteQuest(QuestData q)
    {
        if (q == null) return false;
        QuestRuntime rt;
        if (!map.TryGetValue(q.Id, out rt) || rt.state != QuestState.ReadyToTurnIn) return false;
        Complete(rt);
        return true;
    }

    // ---------------------------------------------------------------- события из мира
    public void ReportTalk(string npcId) { Report(ObjectiveType.TalkTo, npcId); }

    public void ReportDefeat(string npcId)
    {
        if (!string.IsNullOrEmpty(npcId)) SetFlag("defeated_" + npcId);
        Report(ObjectiveType.Defeat, npcId);
    }

    private void Report(ObjectiveType type, string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        int count = ordered.Count; // квесты, добавленные по ходу цепочки, этим событием не затрагиваем
        for (int i = 0; i < count; i++)
        {
            var rt = ordered[i];
            if (rt.state != QuestState.Active) continue;

            bool changed = false;
            var objs = rt.data.objectives;
            for (int j = 0; objs != null && j < objs.Length; j++)
            {
                var o = objs[j];
                if (o.type != type || o.targetId != id) continue;
                if (rt.progress[j] >= Required(o)) continue;

                rt.progress[j]++;
                changed = true;
                Notify("ДЕЛО ОБНОВЛЕНО", rt.data.title + ": " + ObjectiveLabel(o) + " (" + rt.progress[j] + "/" + Required(o) + ")");
            }
            if (changed) Evaluate(rt);
        }
    }

    // ---------------------------------------------------------------- прогресс
    public static int Required(QuestObjective o) { return Mathf.Max(1, o.requiredCount); }

    public int GetProgress(QuestRuntime rt, int j)
    {
        var o = rt.data.objectives[j];
        if (o.type == ObjectiveType.Collect) return Mathf.Min(Required(o), CountItem(o.item));
        return Mathf.Min(Required(o), rt.progress[j]);
    }

    public bool IsObjectiveDone(QuestRuntime rt, int j)
    {
        return GetProgress(rt, j) >= Required(rt.data.objectives[j]);
    }

    public static string ObjectiveLabel(QuestObjective o)
    {
        if (!string.IsNullOrEmpty(o.description)) return o.description;
        switch (o.type)
        {
            case ObjectiveType.TalkTo: return "Поговорить: " + o.targetId;
            case ObjectiveType.Defeat: return "Победить: " + o.targetId;
            default: return "Собрать: " + (o.item != null ? o.item.itemName : "?");
        }
    }

    private bool HasCollect(QuestRuntime rt)
    {
        var objs = rt.data.objectives;
        if (objs == null) return false;
        foreach (var o in objs) if (o.type == ObjectiveType.Collect) return true;
        return false;
    }

    private void Evaluate(QuestRuntime rt)
    {
        bool all = true;
        int n = rt.data.objectives != null ? rt.data.objectives.Length : 0;
        for (int j = 0; j < n; j++)
            if (!IsObjectiveDone(rt, j)) { all = false; break; }

        if (rt.state == QuestState.Active && all)
        {
            if (!rt.data.needsTurnIn) Complete(rt);
            else
            {
                rt.state = QuestState.ReadyToTurnIn;
                Notify("ДЕЛО ГОТОВО К СДАЧЕ", rt.data.title);
            }
        }
        else if (rt.state == QuestState.ReadyToTurnIn && !all)
        {
            rt.state = QuestState.Active; // например, игрок продал собранные предметы
        }
    }

    // ---------------------------------------------------------------- завершение и награды
    private void Complete(QuestRuntime rt)
    {
        var q = rt.data;
        rt.state = QuestState.Completed;

        var rpg = RPGGameManager.Instance;
        if (rpg != null)
        {
            if (q.consumeCollectedItems && q.objectives != null)
                foreach (var o in q.objectives)
                    if (o.type == ObjectiveType.Collect) RemoveItems(o.item, Required(o));

            rpg.gold += q.rewardGold;
            if (q.rewardXP > 0) rpg.AddXP(q.rewardXP);
            if (q.rewardItems != null)
                foreach (var it in q.rewardItems) if (it != null) rpg.inventory.Add(it);
        }

        Notify("ДЕЛО ЗАКРЫТО", q.title + "   +" + q.rewardXP + " XP   +" + q.rewardGold + " зол.");

        if (q.nextQuest != null && q.autoStartNext) StartQuest(q.nextQuest);
    }

    // ---------------------------------------------------------------- инвентарь
    private int CountItem(ItemData item)
    {
        var rpg = RPGGameManager.Instance;
        if (item == null || rpg == null) return 0;
        int c = 0;
        foreach (var it in rpg.inventory) if (it == item) c++;
        return c;
    }

    private void RemoveItems(ItemData item, int count)
    {
        var rpg = RPGGameManager.Instance;
        if (item == null || rpg == null) return;
        for (int i = rpg.inventory.Count - 1; i >= 0 && count > 0; i--)
            if (rpg.inventory[i] == item) { rpg.inventory.RemoveAt(i); count--; }
    }

    private void Notify(string title, string sub)
    {
        if (Toast != null) Toast(title, sub);
    }
}
