using System.Collections.Generic;
using System.Linq;

namespace Dolgostroy.Quests
{
    /// <summary>Полученный игроком квест: ссылка на <see cref="QuestData"/> плюс состояние и прогресс.</summary>
    public class Quest
    {
        public Quest(QuestData data)
        {
            Data = data;
            Objectives = data.Objectives.Select(o => new QuestObjectiveProgress(o)).ToArray();
            Branches = data.Branches.Select(b => new QuestBranchProgress(b)).ToArray();
        }

        public QuestData Data { get; }
        public string Id => Data.Id;
        public QuestState State { get; internal set; } = QuestState.Active;

        public IReadOnlyList<QuestObjectiveProgress> Objectives { get; }
        public IReadOnlyList<QuestBranchProgress> Branches { get; }

        /// <summary>Ветка, по которой пошёл квест; null, пока ни одна не выполнена.</summary>
        public QuestBranchProgress ChosenBranch { get; internal set; }
        public string ChosenBranchId => ChosenBranch?.Data.Id;

        public bool IsReadyToComplete =>
            Objectives.All(o => o.IsDone) && (Branches.Count == 0 || ChosenBranch != null);
    }
}
