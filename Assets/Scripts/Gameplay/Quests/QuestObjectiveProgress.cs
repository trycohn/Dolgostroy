using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>Прогресс одной цели в текущей игре.</summary>
    public class QuestObjectiveProgress
    {
        public QuestObjectiveProgress(QuestObjective data) => Data = data;

        public QuestObjective Data { get; }
        public int Current { get; private set; }
        public int Required => Data.RequiredAmount;
        public bool IsDone => Current >= Required;

        /// <returns>true, если событие подошло и прогресс изменился.</returns>
        internal bool TryAdvance(QuestObjectiveType type, string targetId, int amount)
        {
            if (IsDone || !Data.Matches(type, targetId))
                return false;

            Current = Mathf.Min(Current + amount, Required);
            return true;
        }
    }
}
