using System;
using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Цель квеста: событие <see cref="Type"/> с целью <see cref="TargetId"/> должно произойти
    /// <see cref="RequiredAmount"/> раз. Например, Collect «bottle» × 3.
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        [Tooltip("Текст для журнала, например «Поговорить с шефом».")]
        [SerializeField] private string description;

        [SerializeField] private QuestObjectiveType type;

        [Tooltip("ID NPC, места, врага, предмета или варианта ответа. Пусто — подходит любая цель этого типа.")]
        [SerializeField] private string targetId;

        [Min(1)]
        [SerializeField] private int requiredAmount = 1;

        public string Description => string.IsNullOrEmpty(description) ? $"{type} {targetId}" : description;
        public QuestObjectiveType Type => type;
        public string TargetId => targetId;
        public int RequiredAmount => Mathf.Max(1, requiredAmount);

        public bool Matches(QuestObjectiveType eventType, string eventTargetId) =>
            type == eventType && (string.IsNullOrEmpty(targetId) || targetId == eventTargetId);
    }
}
