using ballban;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using System;
using System.Linq;
using System.Reflection;
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

            // Setup the text UI
            Text.gameObject.SetActive(true);
            Text.transform.SetParent(uiInstance.LayoutParent);
            Text.transform.localScale = Vector3.one;
            Text.text = "";
            Text.fontSize = 20f;

            // Get the current language
            var currentLanguage = LocalizationManager.CurrentLanguage;

            // Append required item information
            Text.text += GetRequiredQuestText(item, currentLanguage);
            Text.text += GetRequiredSubmittingQuestText(item, currentLanguage);
            Text.text += GetRequiredPerkText(item, currentLanguage);
            Text.text += GetRequiredBuildingText(item, currentLanguage);
        }

        /// <summary>
        /// Get the display text for quests that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentLanguage"></param>
        /// <returns></returns>
        string GetRequiredQuestText(Item item, SystemLanguage currentLanguage)
        {
            var text = string.Empty;
            var requiredQuests = Utility.GetRequiredQuests(item);
            if (requiredQuests.Count > 0)
            {
#if DEBUG
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => $"{x.DisplayName} - isActiveAndEnabled: {x.isActiveAndEnabled}, enabled: {x.enabled}"));
#else
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => x.DisplayName));
#endif
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name, currentLanguage);
                text += $"\n\t{questDisplayNames}";
            }
            return text;
        }

        /// <summary>
        /// Get the display text for quests that require submitting the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentLanguage"></param>
        /// <returns></returns>
        string GetRequiredSubmittingQuestText(Item item, SystemLanguage currentLanguage)
        {
            var text = string.Empty;
            var requiredSubmitItems = Utility.GetRequiredSubmitItems(item);
            if (requiredSubmitItems.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name, currentLanguage);
                foreach (var kv in requiredSubmitItems)
                {
                    text += $"\n\t{kv.Value}  -  {kv.Key.Master.DisplayName}";
#if DEBUG
                    text += $"- isActiveAndEnabled: {kv.Key.Master.isActiveAndEnabled}, enabled: {kv.Key.Master.enabled}";
#endif
                }
            }
            return text;
        }

        /// <summary>
        /// Get the display text for perks that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentLanguage"></param>
        /// <returns></returns>
        string GetRequiredPerkText(Item item, SystemLanguage currentLanguage)
        {
            var text = string.Empty;
            var requiredPerkEntries = Utility.GetRequiredPerkEntries(item);
            if (requiredPerkEntries.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name, currentLanguage);
                foreach (var entry in requiredPerkEntries)
                {
                    text += $"\n\t{entry.Amount}  -  {entry.PerkTreeName}/{entry.PerkName}";
#if DEBUG
                    text += $"- {entry.Test}";
#endif
                }
            }
            return text;
        }

        /// <summary>
        /// Get the display text for buildings that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="currentLanguage"></param>
        /// <returns></returns>
        string GetRequiredBuildingText(Item item, SystemLanguage currentLanguage)
        {
            var text = string.Empty;
            var requiredBuildings = Utility.GetRequiredBuildings(item);
            if (requiredBuildings.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name, currentLanguage);
                foreach (var entry in requiredBuildings)
                {
                    text += $"\n\t{entry.Amount}  -  {entry.BuildingName}";
                }
            }
            return text;
        }
    }
}