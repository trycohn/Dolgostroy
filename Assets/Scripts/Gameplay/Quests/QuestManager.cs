using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Журнал квестов: выдаёт квесты, принимает игровые события, ведёт прогресс и ветвление.
    /// Игровой код (бой, диалоги, подбор предметов) только сообщает о событиях через <see cref="Report"/>,
    /// а UI или отладочный лог подписываются на события менеджера и сами правил не знают.
    /// </summary>
    // Awake раньше остальных скриптов, чтобы Instance был готов к их OnEnable.
    [DefaultExecutionOrder(-100)]
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Квесты, которые выдаются при старте игры.")]
        [SerializeField] private List<QuestData> startingQuests = new();

        private readonly List<Quest> quests = new();
        private readonly Dictionary<string, Quest> questsById = new();

        public event Action<Quest> QuestStarted;
        public event Action<Quest, QuestObjectiveProgress> ObjectiveProgressed;
        public event Action<Quest, QuestBranchProgress> BranchChosen;
        public event Action<Quest> QuestCompleted;
        public event Action<Quest> QuestFailed;

        /// <summary>Все полученные квесты в порядке выдачи, включая завершённые.</summary>
        public IReadOnlyList<Quest> Quests => quests;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // Журнал переживает смену сцен: город → бой → город.
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Instance != this)
                return;

            foreach (var quest in startingQuests)
                StartQuest(quest);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public Quest GetQuest(string questId) =>
            questsById.TryGetValue(questId, out var quest) ? quest : null;

        public QuestState GetState(string questId) =>
            GetQuest(questId)?.State ?? QuestState.NotStarted;

        /// <summary>Выдаёт квест. Один и тот же квест второй раз не выдаётся.</summary>
        /// <returns>true, если квест выдан.</returns>
        public bool StartQuest(QuestData data)
        {
            if (data == null || questsById.ContainsKey(data.Id))
                return false;

            var quest = new Quest(data);
            quests.Add(quest);
            questsById.Add(quest.Id, quest);

            foreach (var branch in quest.Branches)
            {
                if (branch.Objectives.Count == 0)
                    Debug.LogWarning($"[Квесты] У ветки «{branch.Data.Title}» квеста «{data.Title}» нет целей, её нельзя выбрать.", data);
            }

            QuestStarted?.Invoke(quest);
            // Квест без целей завершается сразу.
            TryComplete(quest);
            return true;
        }

        /// <summary>
        /// Сообщает о событии в игре и продвигает все подходящие цели активных квестов.
        /// Например: <c>Report(QuestObjectiveType.Defeat, "petrovich")</c> после победы над Петровичем.
        /// </summary>
        public void Report(QuestObjectiveType type, string targetId, int amount = 1)
        {
            if (amount <= 0)
                return;

            // Перебираем копию: завершение квеста выдаёт новые, и на них это событие не распространяется.
            foreach (var quest in quests.ToArray())
            {
                if (quest.State != QuestState.Active)
                    continue;

                Advance(quest, quest.Objectives, type, targetId, amount);

                if (quest.ChosenBranch == null)
                {
                    foreach (var branch in quest.Branches)
                        Advance(quest, branch.Objectives, type, targetId, amount);

                    TryChooseBranch(quest);
                }

                TryComplete(quest);
            }
        }

        /// <summary>Проваливает активный квест, например если должник сбежал.</summary>
        /// <returns>true, если квест был активен и провален.</returns>
        public bool FailQuest(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null || quest.State != QuestState.Active)
                return false;

            quest.State = QuestState.Failed;
            QuestFailed?.Invoke(quest);
            return true;
        }

        private void Advance(Quest quest, IReadOnlyList<QuestObjectiveProgress> objectives,
            QuestObjectiveType type, string targetId, int amount)
        {
            foreach (var objective in objectives)
            {
                if (objective.TryAdvance(type, targetId, amount))
                    ObjectiveProgressed?.Invoke(quest, objective);
            }
        }

        private void TryChooseBranch(Quest quest)
        {
            foreach (var branch in quest.Branches)
            {
                if (!branch.IsDone)
                    continue;

                quest.ChosenBranch = branch;
                BranchChosen?.Invoke(quest, branch);
                return;
            }
        }

        private void TryComplete(Quest quest)
        {
            if (quest.State != QuestState.Active || !quest.IsReadyToComplete)
                return;

            quest.State = QuestState.Completed;
            QuestCompleted?.Invoke(quest);

            foreach (var next in quest.Data.NextQuests)
                StartQuest(next);

            if (quest.ChosenBranch != null)
            {
                foreach (var next in quest.ChosenBranch.Data.NextQuests)
                    StartQuest(next);
            }
        }
    }
}
