using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace QuestItemRequirementsDisplay
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private Harmony harmony;

        private Item _currentItem = null;
        private bool _isDetailShown = false;

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
            harmony = new Harmony("DisplayRequiredItemCount");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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
            // Return if detail is already shown or no current item or text is not active
            if (_isDetailShown || _currentItem == null || !Text.gameObject.activeSelf) return;

            // Update the UI if shift is held
            var isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShiftHeld) UpdateItemUI(true, _currentItem);
        }

        private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item)
        {
            _currentItem = item;
            _isDetailShown = false;

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

            // Check if shift is held
            var isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            UpdateItemUI(isShiftHeld, item);
        }

        /// <summary>
        /// Update the item UI with required item amount.
        /// </summary>
        /// <param name="isShiftHeld"></param>
        /// <param name="item"></param>
        private void UpdateItemUI(bool isShiftHeld, Item item)
        {
            _isDetailShown = isShiftHeld;

            // Clear previous text
            Text.text = "";

            // Get required item information
            (var requiredQuestText, var requiredQuestItemAmount) = GetRequiredQuestText(item);
            (var requiredSubmittingQuestText, var requiredSubmittingQuestItemAmount) = GetRequiredSubmittingQuestText(item);
            (var requiredPerkText, var requiredPerkItemAmount) = GetRequiredPerkText(item);
            (var requiredBuildingText, var requiredBuildingItemAmount) = GetRequiredBuildingText(item);

            // Calculate total required item count
            var totalRequiredItemAmount = requiredQuestItemAmount + requiredSubmittingQuestItemAmount + requiredPerkItemAmount + requiredBuildingItemAmount;
            if (totalRequiredItemAmount == 0)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            // Get total required item count text
            var itemAmountInCharacterInventory = GetItemAmount.InCharacterInventory(item.TypeID) + GetItemAmount.InPetInventory(item.TypeID);
            var itemAmountInPlayerStorage = GetItemAmount.InPlayerStorage(item.TypeID);
            var totalItemAmount = itemAmountInCharacterInventory + itemAmountInPlayerStorage;

            // Determine color based on whether the player has enough items
            var colorOfTotalitemAmount = totalItemAmount >= totalRequiredItemAmount ? "green" : "red";

            // Show Text [Total required amount of this item: N]
            var totalRequiredItemAmountText = LocalizedText.Get(nameof(totalRequiredItemAmount));
            Text.text += $"{totalRequiredItemAmountText} <color={colorOfTotalitemAmount}>{totalItemAmount}</color> / {totalRequiredItemAmount}";

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
                Text.text += $"\n\t<color=yellow><size=17>----- {LocalizedText.Get("pressShift", false)} -----<size=17></color>";
            }


            //// Show item amounts
            //Text.text += "\n";
            //Text.text += $"\n In Character Inventory: {itemAmountInCharacterInventory}";
            //Text.text += $"\n In Player Storage: {itemAmountInPlayerStorage}";
            //Text.text += $"\n In Inventory Items: {itemAmountInInventoryItems}";
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
            var requiredQuests = GetRequiredItemAmount.GetRequiredQuests(item);
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
            var requiredSubmitItems = GetRequiredItemAmount.GetRequiredSubmitItems(item);
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
            var amount = 0L;
            var requiredPerkEntries = GetRequiredItemAmount.GetRequiredPerkEntries(item);
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
            var amount = 0L;
            var requiredBuildings = GetRequiredItemAmount.GetRequiredBuildings(item);
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