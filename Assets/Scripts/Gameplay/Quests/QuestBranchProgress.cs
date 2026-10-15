using System.Collections.Generic;
using System.Linq;

namespace Dolgostroy.Quests
{
    /// <summary>Прогресс одной ветки квеста в текущей игре.</summary>
    public class QuestBranchProgress
    {
        public QuestBranchProgress(QuestBranch data)
        {
            Data = data;
            Objectives = data.Objectives.Select(o => new QuestObjectiveProgress(o)).ToArray();
        }

        public QuestBranch Data { get; }
        public IReadOnlyList<QuestObjectiveProgress> Objectives { get; }

        // Ветку без целей выбрать нельзя, иначе она срабатывала бы сразу.
        public bool IsDone => Objectives.Count > 0 && Objectives.All(o => o.IsDone);
    }
}
