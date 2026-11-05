using System;
using UnityEngine;

public enum DialogueAction
{
    None,           // просто перейти к следующей реплике
    StartQuest,     // взять квест (поле quest)
    CompleteQuest,  // сдать квест (поле quest) — сработает, только если он ReadyToTurnIn
    StartBattle,    // закрыть диалог и начать 2D-бой с этим NPC
    OpenShop        // закрыть диалог и открыть магазин этого NPC
}

/// <summary>Условие показа реплики / выбора варианта диалога. Пустое условие = всегда true.</summary>
[Serializable]
public class DialogueCondition
{
    [Tooltip("Если задан — проверяем состояние этого квеста")]
    public QuestData quest;
    public QuestState state = QuestState.Active;

    [Tooltip("Флаг должен быть установлен")]
    public string requiredFlag;
    [Tooltip("Флаг НЕ должен быть установлен")]
    public string forbiddenFlag;

    public bool IsMet()
    {
        var qm = QuestManager.Instance;

        if (quest != null)
        {
            QuestState st = qm != null ? qm.GetState(quest) : QuestState.NotStarted;
            if (st != state) return false;
        }
        if (!string.IsNullOrEmpty(requiredFlag) && (qm == null || !qm.HasFlag(requiredFlag))) return false;
        if (!string.IsNullOrEmpty(forbiddenFlag) && qm != null && qm.HasFlag(forbiddenFlag)) return false;
        return true;
    }
}

[Serializable]
public class DialogueChoice
{
    public string text = "Ответ";
    [Tooltip("ID следующего узла. Пусто = закрыть диалог")]
    public string nextNodeId;

    [Tooltip("Показывать вариант только если условие выполнено")]
    public DialogueCondition condition = new DialogueCondition();

    [Tooltip("Стоимость выбора в золоте (кнопка недоступна, если не хватает)")]
    public int goldCost = 0;
    [Tooltip("Установить флаг при выборе (для ветвления сюжета)")]
    public string setFlag;

    public DialogueAction action = DialogueAction.None;
    [Tooltip("Квест для StartQuest / CompleteQuest")]
    public QuestData quest;
}

[Serializable]
public class DialogueNode
{
    public string id = "start";
    [Tooltip("Пусто = имя NPC. Подстановки: {player}, {gold}")]
    public string speaker;
    [TextArea(3, 8)] public string text;

    [Tooltip("Куда идти, если выборов нет (кнопка 'Далее'). Пусто = закрыть")]
    public string nextNodeId;
    public DialogueChoice[] choices;
}

/// <summary>Вариант диалога для NPC: первый с выполненным условием побеждает.</summary>
[Serializable]
public class DialogueVariant
{
    public DialogueCondition condition = new DialogueCondition();
    public DialogueData dialogue;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "RPG/Dialogue (Диалог)")]
public class DialogueData : ScriptableObject
{
    public string startNodeId = "start";
    public DialogueNode[] nodes;

    public DialogueNode FindNode(string id)
    {
        if (nodes == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i] != null && nodes[i].id == id) return nodes[i];
        return null;
    }

    public DialogueNode GetStartNode()
    {
        var n = FindNode(startNodeId);
        if (n == null && nodes != null && nodes.Length > 0) n = nodes[0];
        return n;
    }
}
