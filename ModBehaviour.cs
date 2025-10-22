using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using SodaCraft.Localizations;

namespace QuestItemRequirementsDisplay
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        TextMeshProUGUI _text = null;
        TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    _text = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
                }
                return _text;
            }
        }
        void Awake()
        {
            Debug.Log("DisplayRequiredItemCount Loaded!!!");
        }
        void OnDestroy()
        {
            if (_text != null)
                Destroy(_text);
        }
        void OnEnable()
        {
            ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta += OnSetupMeta;
        }
        void OnDisable()
        {
            ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta -= OnSetupMeta;
        }

        private void OnSetupMeta(ItemHoveringUI uI, ItemMetaData data)
        {
            Text.gameObject.SetActive(false);
        }

        private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item)
        {
            if (item == null)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            Text.gameObject.SetActive(true);
            Text.transform.SetParent(uiInstance.LayoutParent);
            Text.transform.localScale = Vector3.one;
            Text.text = "";
            Text.fontSize = 20f;

            var currentLanguage = LocalizationManager.CurrentLanguage;

            var totalQuests = GameplayDataSettings.QuestCollection;
            var questManager = QuestManager.Instance;
            var finishedQuestsId = questManager.HistoryQuests.Select(q => q.ID);

            // Quests that require this item to be prepared
            var requiredQuests = new List<Quest>();
            foreach (var quest in totalQuests)
            {
                // Skip if the quest is already completed
                if (finishedQuestsId.Contains(quest.ID)) continue;

                // If the quest requires not matching this item, skip it
                if (quest.RequiredItemID == item.TypeID)
                {
                    requiredQuests.Add(quest);
                }
            }
            if (requiredQuests.Count != 0)
            {
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => x.DisplayName));
                switch (currentLanguage)
                {
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                        Text.text += $"需要准备该物品的任务:\n\t{questDisplayNames}";
                        break;
                    case SystemLanguage.ChineseTraditional:
                        Text.text += $"需要準備該物品的任務:\n\t{questDisplayNames}";
                        break;
                    case SystemLanguage.Japanese:
                        Text.text += $"このアイテムが必要なクエスト:\n\t{questDisplayNames}";
                        break;
                    case SystemLanguage.Korean:
                        Text.text += $"이아이템이 필요한 퀘스트:\n\t{questDisplayNames}";
                        break;
                    default:
                        Text.text += $"Quests required this item:\n\t{questDisplayNames}";
                        break;
                }
            }

            // Quests that require submitting item
            var submitItemsDir = new Dictionary<SubmitItems, string>();
            foreach (Quest quest in totalQuests)
            {
                // Skip if the quest is already completed
                if (finishedQuestsId.Contains(quest.ID)) continue;
                if (quest.Tasks == null) continue;

                foreach (var task in quest.Tasks)
                {
                    // If the task is a SubmitItems task and unfinished and matches the item type ID
                    if (task is SubmitItems submitItems && !submitItems.IsFinished() && submitItems.ItemTypeID == item.TypeID)
                    {
                        submitItemsDir.Add(submitItems, submitItems.Description);
                    }
                }
            }

            var count = 0;
            if (submitItemsDir.Count > 0)
            {
                switch (currentLanguage)
                {
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                        Text.text += "\n需要提交该物品的任务:";
                        break;
                    case SystemLanguage.ChineseTraditional:
                        Text.text += "\n需要提交該物品的任務:";
                        break;
                    case SystemLanguage.Japanese:
                        Text.text += "\nこのアイテム納品が必要なクエスト:";
                        break;
                    case SystemLanguage.Korean:
                        Text.text += "\n이아이템 재출이 필요한 퀘스트:";
                        break;
                    default:
                        Text.text += "\nQuests required submit this item:";
                        break;
                }
            }
            foreach (var kvp in submitItemsDir)
            {
                var submitItems = kvp.Key;
                var description = kvp.Value;

                var match = Regex.Match(description, @"(\d+)[^\d]+\d+\s?$");
                if (match.Success)
                {
                    var countStr = match.Groups[1].Value;
                    count += int.TryParse(countStr, out var c) ? c : 0;
                    // Text.text += $"\n{description}";
                    Text.text += $"\n\t{countStr}  -  {submitItems.Master.DisplayName}";
                }
                else
                {
                    Text.text += $"\nregex err. {description}";
                }
            }
        }
    }
}