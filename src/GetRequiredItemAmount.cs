using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using Duckov.Buildings;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.VisualScripting;

namespace QuestItemRequirementsDisplay
{
    static public class GetRequiredItemAmount
    {
        public static List<Quest> TotalQuests = InitTotalQuests();

        /// <summary>
        /// Initialize the total quests list, excluding testing quests.
        /// </summary>
        /// <returns></returns>
        private static List<Quest> InitTotalQuests()
        {
             var totalQuests = GameplayDataSettings.QuestCollection
                .ConvertTo<List<Quest>>()
                .Where(q => !IsTestingObjectDisplayName(q.DisplayName))
                .ToList();

            return totalQuests;
        }

        /// <summary>
        /// Check if the object's display name is a testing name (starts and ends with '*')
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool IsTestingObjectDisplayName(string name)
        {
            return name.StartsWith("*") && name.EndsWith("*");
        }

        /// <summary>
        /// Get a list of quests that require the specified item to be prepared.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<Quest> GetRequiredQuests(Item item)
        {
            var finishedQuestsId = QuestManager.Instance.HistoryQuests.Select(q => q.ID);

            // Quests that require this item to be prepared
            var requiredQuests = TotalQuests
                // Get only unfinished quests that require this item
                .Where(q => !finishedQuestsId.Contains(q.ID) && q.RequiredItemID == item.TypeID)
                .ToList();

            return requiredQuests;
        }

        public struct RequiredPerk
        {
            public long Amount;
            public string PerkTreeName;
            public string PerkName;
        }
        /// <summary>
        /// Get a list of perks that require the specified item to unlock.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<RequiredPerk> GetRequiredPerkEntries(Item item)
        {
            var requiredPerkEntries = PerkTreeManager.Instance.perkTrees
                    .SelectMany(perkTree => perkTree.Perks
                        // Only consider locked perks
                        .Where(perk => !perk.Unlocked)
                        // Get required items for each perk
                        .SelectMany(perk => perk.Requirement.cost.items
                            // Get entries that match the item type ID
                            .Where(itemEntry => itemEntry.id == item.TypeID)
                            .Select(itemEntry => new RequiredPerk
                            {
                                Amount = itemEntry.amount,
                                PerkTreeName = perkTree.DisplayName,
                                PerkName = perk.DisplayName,
                            })
                        )
                    ).ToList();
            return requiredPerkEntries;
        }

        /// <summary>
        /// Get a dictionary of SubmitItems tasks that require the specified item, along with the required amount.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public List<SubmitItems> GetRequiredSubmitItems(Item item)
        {
            var finishedQuestsId = new HashSet<int>(QuestManager.Instance.HistoryQuests.Select(q => q.ID));

            var requiredSubmitItems = TotalQuests
                // Skip if the quest is already completed
                .Where(quest => !finishedQuestsId.Contains(quest.ID) && quest.Tasks != null)
                .SelectMany(quest => quest.Tasks
                    // Select only SubmitItems tasks
                    .OfType<SubmitItems>()
                    // Filter unfinished tasks that match the item type ID
                    .Where(submitItem => !submitItem.IsFinished() && submitItem.ItemTypeID == item.TypeID)
                    .Select(submitItem => submitItem)
                ).ToList();

            return requiredSubmitItems;
        }

        public struct RequiredBuilding
        {
            public long Amount;
            public string BuildingName;
        }
        /// <summary>
        /// Get a list of buildings that require the specified item to unlock.
        /// WARNING: not UNLOCKED, but UNPLACED buildings
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static List<RequiredBuilding> GetRequiredBuildings(Item item)
        {
            return BuildingDataCollection.Instance.Infos
                // Only consider buildings that are not yet placed
                .Where(info => info.CurrentAmount == 0 && !IsTestingObjectDisplayName(info.DisplayName))
                .SelectMany(info => info.cost.items
                    .Where(itemEntry => itemEntry.id == item.TypeID)
                    .Select(itemEntry => new RequiredBuilding
                    {
                        Amount = itemEntry.amount,
                        BuildingName = info.DisplayName
                    })
                ).ToList();
        }

        /// <summary>
        /// Get quest that require the specified item, along with the required amount.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Dictionary<QuestTask_UseItem, int> GetRequiredUseItems(Item item)
        {
            var itemTypeIDRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "itemTypeID");
            var requireAmountRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "requireAmount");

            var finishedQuestsId = new HashSet<int>(QuestManager.Instance.HistoryQuests.Select(q => q.ID));

            var requiredUseItems = TotalQuests
                .Where(quest => !finishedQuestsId.Contains(quest.ID) && quest.Tasks != null)
                .SelectMany(quest => quest.Tasks
                    .Where(task => task != null)
                    .OfType<QuestTask_UseItem>()
                    .Where(useItem => {
                        var itemTypeID = itemTypeIDRef(useItem);
                        return !useItem.IsFinished() && itemTypeID == item.TypeID;
                    })
                    .Select(useItem => {
                        var requireAmount = requireAmountRef(useItem);
                        return new { useItem, requireAmount };
                    })
                )
                .ToDictionary(x => x.useItem, x => x.requireAmount);
            return requiredUseItems;
        }
    }
}
