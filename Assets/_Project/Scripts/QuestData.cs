using System;
using UnityEngine;

public enum QuestState { NotStarted, Active, ReadyToTurnIn, Completed }
public enum ObjectiveType { TalkTo, Defeat, Collect }

[Serializable]
public class QuestObjective
{
    public ObjectiveType type = ObjectiveType.TalkTo;

    [Tooltip("npcId из WorldInteractable (для TalkTo и Defeat)")]
    public string targetId;

    [Tooltip("Предмет (только для Collect). Считается по инвентарю игрока")]
    public ItemData item;

    public int requiredCount = 1;

    [Tooltip("Текст в журнале. Пусто = сгенерируется автоматически")]
    public string description;
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "RPG/Quest (Дело)")]
public class QuestData : ScriptableObject
{
    [Header("Основное")]
    [Tooltip("Уникальный ID. Если пусто — берётся имя ассета")]
    public string questId;
    public string title = "Новое дело";
    [TextArea(3, 8)] public string description;
    [TextArea(2, 5)] public string completionText;

    [Header("Задачи (выполняются в любом порядке)")]
    public QuestObjective[] objectives;

    [Header("Сдача дела")]
    [Tooltip("true: после задач нужно вернуться и выбрать в диалоге действие 'Завершить квест'. false: закроется само")]
    public bool needsTurnIn = true;
    public string turnInHint = "Вернитесь к заказчику и сдайте дело.";
    [Tooltip("Забрать собранные предметы (Collect) при сдаче")]
    public bool consumeCollectedItems = true;

    [Header("Награда")]
    public int rewardXP = 50;
    public int rewardGold = 50;
    public ItemData[] rewardItems;

    [Header("Цепочка")]
    [Tooltip("Следующее дело в цепочке")]
    public QuestData nextQuest;
    [Tooltip("true: следующее дело начнётся автоматически после закрытия этого")]
    public bool autoStartNext = true;

    public string Id { get { return string.IsNullOrEmpty(questId) ? name : questId; } }
}
