using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Вариант завершения квеста. Ветка выбирается, как только выполнены все её цели,
    /// после этого остальные ветки квеста больше не засчитываются.
    /// </summary>
    [Serializable]
    public class QuestBranch
    {
        [Tooltip("Стабильный ID ветки: по нему диалоги могут проверить, какой путь выбрал игрок.")]
        [SerializeField] private string id;

        [SerializeField] private string title;

        [Tooltip("Цели этой ветки. Ветка без целей никогда не будет выбрана.")]
        [SerializeField] private List<QuestObjective> objectives = new();

        [Tooltip("Квесты, которые выдаются, если квест завершён по этой ветке.")]
        [SerializeField] private List<QuestData> nextQuests = new();

        public string Id => id;
        public string Title => string.IsNullOrEmpty(title) ? id : title;
        public IReadOnlyList<QuestObjective> Objectives => objectives;
        public IReadOnlyList<QuestData> NextQuests => nextQuests;
    }
}
