using ballban;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;


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

            // Display quests that require this item
            var currentLanguage = LocalizationManager.CurrentLanguage;
            var requiredQuests = Utility.GetRequiredQuests(item);
            if (requiredQuests.Count > 0)
            {
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => x.DisplayName));
                switch (currentLanguage)
                {
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                        Text.text += $"\n需要准备该物品的任务:";
                        break;
                    case SystemLanguage.ChineseTraditional:
                        Text.text += $"\n需要準備該物品的任務:";
                        break;
                    case SystemLanguage.Japanese:
                        Text.text += $"\nこのアイテムが必要なクエスト:";
                        break;
                    case SystemLanguage.Korean:
                        Text.text += $"\n이아이템이 필요한 퀘스트:";
                        break;
                    default:
                        Text.text += $"\nQuests required this item:";
                        break;
                }
                Text.text += $"\n\t{questDisplayNames}";
            }

            // Display quests that require submitting this item
            var requiredSubmitItems = Utility.GetRequiredSubmitItems(item);
            if (requiredSubmitItems.Count > 0)
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
                foreach (var kv in requiredSubmitItems)
                {
                    Text.text += $"\n\t{kv.Value}  -  {kv.Key.Master.DisplayName}";
                }

            }

            // Display perks that require this item
            var requiredPerkEntries = Utility.GetRequiredPerkEntries(item);
            if (requiredPerkEntries.Count > 0)
            {
                switch (currentLanguage)
                {
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                        Text.text += "\n需要该物品解锁的天赋:";
                        break;
                    case SystemLanguage.ChineseTraditional:
                        Text.text += "\n需要該物品解鎖的天賦:";
                        break;
                    case SystemLanguage.Japanese:
                        Text.text += "\nこのアイテムが必要なスキル:";
                        break;
                    case SystemLanguage.Korean:
                        Text.text += "\n이아이템이 필요한 스킬:";
                        break;
                    default:
                        Text.text += "\nPerks required this item:";
                        break;
                }
                foreach (var entry in requiredPerkEntries)
                {
                    Text.text += $"\n\t{entry.Amount}  -  {entry.PerkTreeName}/{entry.PerkName}";
                }
            }

            // Display buildings that require this item
            var requiredBuildings = Utility.GetRequiredBuildings(item);
            if (requiredBuildings.Count > 0)
            {
                switch (currentLanguage)
                {
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified:
                        Text.text += "\n需要该物品解锁的建筑:";
                        break;
                    case SystemLanguage.ChineseTraditional:
                        Text.text += "\n需要該物品解鎖的建築:";
                        break;
                    case SystemLanguage.Japanese:
                        Text.text += "\nこのアイテムが必要な建物:";
                        break;
                    case SystemLanguage.Korean:
                        Text.text += "\n이아이템으로 해금이 필요한 건물:";
                        break;
                    default:
                        Text.text += "\nBuildings required this item:";
                        break;
                }
                foreach (var entry in requiredBuildings)
                {
                    Text.text += $"\n\t{entry.Amount}  -  {entry.BuildingName}";
                }
            }
        }
    }
}