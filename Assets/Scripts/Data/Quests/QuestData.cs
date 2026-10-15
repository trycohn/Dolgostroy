using System.Collections.Generic;
using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Описание квеста (ассет). Создаётся через Create → ДОЛГострой → Квест.
    /// Квест завершается, когда выполнены все общие цели и, если есть ветки, выбрана одна из них.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest", menuName = "ДОЛГострой/Квест")]
    public class QuestData : ScriptableObject
    {
        [Tooltip("Стабильный ID для сохранений и проверок. Пусто — используется имя ассета.")]
        [SerializeField] private string id;

        [SerializeField] private string title;

        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("Цели")]
        [Tooltip("Общие цели: нужно выполнить все.")]
        [SerializeField] private List<QuestObjective> objectives = new();

        [Header("Ветвление")]
        [Tooltip("Пусто — квест линейный. Иначе засчитывается ветка, все цели которой выполнены первыми.")]
        [SerializeField] private List<QuestBranch> branches = new();

        [Header("Продолжение")]
        [Tooltip("Квесты, которые выдаются после завершения по любой ветке.")]
        [SerializeField] private List<QuestData> nextQuests = new();

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => string.IsNullOrEmpty(title) ? Id : title;
        public string Description => description;
        public IReadOnlyList<QuestObjective> Objectives => objectives;
        public IReadOnlyList<QuestBranch> Branches => branches;
        public IReadOnlyList<QuestData> NextQuests => nextQuests;
    }
}
