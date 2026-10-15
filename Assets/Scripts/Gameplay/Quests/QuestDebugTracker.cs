using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dolgostroy.Quests
{
    /// <summary>
    /// Временное отслеживание квестов через консоль вместо UI.
    /// Пишет в лог все изменения журнала, по клавише выводит журнал целиком,
    /// а горячие клавиши отправляют тестовые события, пока нет боёв и диалогов.
    /// Работает только в редакторе и Development Build.
    /// </summary>
    public class QuestDebugTracker : MonoBehaviour
    {
        [Serializable]
        private class DebugHotkey
        {
            public Key key = Key.None;
            public QuestObjectiveType type;
            public string targetId;
            public int amount = 1;
        }

        [Tooltip("Пусто — берётся QuestManager.Instance.")]
        [SerializeField] private QuestManager manager;

        [SerializeField] private Key printJournalKey = Key.J;

        [Tooltip("Тестовые события по клавишам.")]
        [SerializeField] private DebugHotkey[] hotkeys = Array.Empty<DebugHotkey>();

        private void OnEnable()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            if (manager == null)
                manager = QuestManager.Instance;

            if (manager == null)
            {
                Debug.LogWarning("[Квесты] QuestDebugTracker не нашёл QuestManager.", this);
                enabled = false;
                return;
            }

            manager.QuestStarted += OnQuestStarted;
            manager.ObjectiveProgressed += OnObjectiveProgressed;
            manager.BranchChosen += OnBranchChosen;
            manager.QuestCompleted += OnQuestCompleted;
            manager.QuestFailed += OnQuestFailed;
        }

        private void OnDisable()
        {
            if (manager == null)
                return;

            manager.QuestStarted -= OnQuestStarted;
            manager.ObjectiveProgressed -= OnObjectiveProgressed;
            manager.BranchChosen -= OnBranchChosen;
            manager.QuestCompleted -= OnQuestCompleted;
            manager.QuestFailed -= OnQuestFailed;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (printJournalKey != Key.None && keyboard[printJournalKey].wasPressedThisFrame)
                PrintJournal();

            foreach (var hotkey in hotkeys)
            {
                if (hotkey.key == Key.None || !keyboard[hotkey.key].wasPressedThisFrame)
                    continue;

                Debug.Log($"[Квесты] Тестовое событие: {hotkey.type} «{hotkey.targetId}» ×{hotkey.amount}");
                manager.Report(hotkey.type, hotkey.targetId, hotkey.amount);
            }
        }

        [ContextMenu("Показать журнал")]
        public void PrintJournal()
        {
            var questManager = manager != null ? manager : QuestManager.Instance;
            if (questManager == null)
                return;

            var text = new StringBuilder($"[Квесты] Журнал, квестов: {questManager.Quests.Count}\n");
            foreach (var quest in questManager.Quests)
            {
                text.AppendLine($"{StateName(quest.State)}: «{quest.Data.Title}»");
                AppendObjectives(text, quest.Objectives, "    ");

                if (quest.ChosenBranch != null)
                {
                    text.AppendLine($"    Путь «{quest.ChosenBranch.Data.Title}» (выбран):");
                    AppendObjectives(text, quest.ChosenBranch.Objectives, "        ");
                    continue;
                }

                foreach (var branch in quest.Branches)
                {
                    text.AppendLine($"    Путь «{branch.Data.Title}»:");
                    AppendObjectives(text, branch.Objectives, "        ");
                }
            }

            Debug.Log(text.ToString());
        }

        private static void AppendObjectives(StringBuilder text, IReadOnlyList<QuestObjectiveProgress> objectives, string indent)
        {
            foreach (var objective in objectives)
                text.AppendLine($"{indent}[{(objective.IsDone ? "x" : " ")}] {objective.Data.Description} {objective.Current}/{objective.Required}");
        }

        private static string StateName(QuestState state) => state switch
        {
            QuestState.Active => "Активен",
            QuestState.Completed => "Выполнен",
            QuestState.Failed => "Провален",
            _ => "Не начат"
        };

        private void OnQuestStarted(Quest quest) =>
            Debug.Log($"[Квесты] Новый квест: «{quest.Data.Title}». {quest.Data.Description}", quest.Data);

        private void OnObjectiveProgressed(Quest quest, QuestObjectiveProgress objective) =>
            Debug.Log($"[Квесты] «{quest.Data.Title}»: {objective.Data.Description} {objective.Current}/{objective.Required}" +
                      (objective.IsDone ? " (готово)" : ""));

        private void OnBranchChosen(Quest quest, QuestBranchProgress branch) =>
            Debug.Log($"[Квесты] «{quest.Data.Title}»: выбран путь «{branch.Data.Title}»");

        private void OnQuestCompleted(Quest quest) =>
            Debug.Log($"[Квесты] Квест выполнен: «{quest.Data.Title}»" +
                      (quest.ChosenBranch != null ? $", путь «{quest.ChosenBranch.Data.Title}»" : ""));

        private void OnQuestFailed(Quest quest) =>
            Debug.Log($"[Квесты] Квест провален: «{quest.Data.Title}»");
    }
}
