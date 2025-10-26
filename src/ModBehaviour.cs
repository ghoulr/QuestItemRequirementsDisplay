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
        private Item? _currenItem = null;
        private bool? _lastShiftHeld = null;
        private bool _isShiftHeld = false;

        TextMeshProUGUI? _text = null;
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

        void Update()
        {
            // Check if shift key is held
            _isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // If the text UI is not active, do nothing
            if (_currenItem == null || !Text.gameObject.activeSelf) return;

            // If shift key state changed, update the UI
            if (_isShiftHeld != _lastShiftHeld)
            {
                UpdateItemUI(_isShiftHeld, _currenItem);
                _lastShiftHeld = _isShiftHeld;
            }
        }

        private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item)
        {
            _currenItem = item;
            if (item == null)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            // Setup the text UI
            Text.gameObject.SetActive(true);
            Text.transform.SetParent(uiInstance.LayoutParent);
            Text.transform.localScale = Vector3.one;
            Text.fontSize = 20f;

            UpdateItemUI(_isShiftHeld, item);
        }

        /// <summary>
        /// Update the item UI with required item counts.
        /// </summary>
        /// <param name="isShiftHeld"></param>
        /// <param name="item"></param>
        private void UpdateItemUI(bool isShiftHeld, Item item)
        {
            // Clear previous text
            Text.text = "";

            // Get required item information
            (var requiredQuestText, var requiredQuestItemAmount) = GetRequiredQuestText(item);
            (var requiredSubmittingQuestText, var requiredSubmittingQuestItemAmount) = GetRequiredSubmittingQuestText(item);
            (var requiredPerkText, var requiredPerkItemAmount) = GetRequiredPerkText(item);
            (var requiredBuildingText, var requiredBuildingItemAmount) = GetRequiredBuildingText(item);

            // Calculate total required item count
            var totalRequiredItemCount = requiredQuestItemAmount + requiredSubmittingQuestItemAmount + requiredPerkItemAmount + requiredBuildingItemAmount;
            if (totalRequiredItemCount == 0)
            {
                Text.gameObject.SetActive(false);
                return;
            }
            var totalRequiredItemCountText = LocalizedText.Get(nameof(totalRequiredItemCount));
            // Total required amount of this item: N
            Text.text += $"{totalRequiredItemCountText} {totalRequiredItemCount}";

            // If shift is held, show detailed information
            if (isShiftHeld)
            {
                Text.text += requiredQuestText;
                Text.text += requiredSubmittingQuestText;
                Text.text += requiredPerkText;
                Text.text += requiredBuildingText;
            }
            else
            {
                // ----- Press Shift -----
                Text.text += $"\n\t----- {LocalizedText.Get("pressShift", false)} -----\n";
            }
        }

        /// <summary>
        /// Get the display text for quests that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        (string, int) GetRequiredQuestText(Item item)
        {
            var text = string.Empty;
            var amount = 0;
            var requiredQuests = Utility.GetRequiredQuests(item);
            if (requiredQuests.Count > 0)
            {
#if DEBUG
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => $"{x.DisplayName} - isActiveAndEnabled: {x.isActiveAndEnabled}, enabled: {x.enabled}"));
#else
                var questDisplayNames = String.Join("\n\t", requiredQuests.Select(x => $"{x.RequiredItemCount}  -  {x.DisplayName}"));
                amount = requiredQuests.Sum(x => x.RequiredItemCount);
#endif
                text = LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
                text += $"\n\t{questDisplayNames}";
            }
            return (text, amount);
        }

        /// <summary>
        /// Get the display text for quests that require submitting the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        (string, int) GetRequiredSubmittingQuestText(Item item)
        {
            var text = string.Empty;
            var amount = 0;
            var requiredSubmitItems = Utility.GetRequiredSubmitItems(item);
            if (requiredSubmitItems.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
                foreach (var kv in requiredSubmitItems)
                {
                    text += $"\n\t{kv.Value}  -  {kv.Key.Master.DisplayName}";
                    amount += int.TryParse(kv.Value, out var result) ? result : 0;
#if DEBUG
                    text += $"- isActiveAndEnabled: {kv.Key.Master.isActiveAndEnabled}, enabled: {kv.Key.Master.enabled}";
#endif
                }
            }
            return (text, amount);
        }

        /// <summary>
        /// Get the display text for perks that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        (string, int) GetRequiredPerkText(Item item)
        {
            var text = string.Empty;
            var amount = 0l;
            var requiredPerkEntries = Utility.GetRequiredPerkEntries(item);
            if (requiredPerkEntries.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
                foreach (var entry in requiredPerkEntries)
                {
                    text += $"\n\t{entry.Amount}  -  {entry.PerkTreeName}/{entry.PerkName}";
                    amount += entry.Amount;
#if DEBUG
                    text += $"- {entry.Test}";
#endif
                }
            }
            return (text, (int)amount);
        }

        /// <summary>
        /// Get the display text for buildings that require the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        (string, int) GetRequiredBuildingText(Item item)
        {
            var text = string.Empty;
            var amount = 0l;
            var requiredBuildings = Utility.GetRequiredBuildings(item);
            if (requiredBuildings.Count > 0)
            {
                text += LocalizedText.Get(MethodBase.GetCurrentMethod().Name);
                foreach (var entry in requiredBuildings)
                {
                    text += $"\n\t{entry.Amount}  -  {entry.BuildingName}";
                    amount += entry.Amount;
                }
            }
            return (text, (int)amount);
        }
    }
}