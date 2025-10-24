using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using Duckov.Buildings;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ballban
{
    static public class Utility
    {
        public static List<Quest> TotalQuests = InitTotalQuests();

        /// <summary>
        /// Initialize the total quests list, excluding testing quests.
        /// </summary>
        /// <returns></returns>
        private static List<Quest> InitTotalQuests()
        {
            var totalQuests = new List<Quest>(GameplayDataSettings.QuestCollection.Count);
            foreach (var quest in GameplayDataSettings.QuestCollection)
            {
                if (IsTestingObjectDisplayName(quest.DisplayName)) continue;
                totalQuests.Add(quest);
            }

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

        public struct RequiredPerk
        {
            public long Amount;
            public string PerkTreeName;
            public string PerkName;
            public string Test;
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
#if DEBUG
                                Test = $"isActiveAndEnabled: {perkTree.isActiveAndEnabled}, enabled: {perkTree.enabled} {perkTree.RelationGraphOwner}"
#endif
                            })
                        )
                    ).ToList();
            return requiredPerkEntries;
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
            var requiredQuests = new List<Quest>();
            foreach (var quest in TotalQuests)
            {
                // Skip if the quest is already completed
                if (finishedQuestsId.Contains(quest.ID)) continue;

                // If the quest requires not matching this item, skip it
                if (quest.RequiredItemID == item.TypeID)
                {
                    requiredQuests.Add(quest);
                }
            }

            return requiredQuests;
        }

        /// <summary>
        /// Get a dictionary of SubmitItems tasks that require the specified item, along with the required amount.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        static public Dictionary<SubmitItems, string> GetRequiredSubmitItems(Item item)
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
                    .Select(submitItem =>
                    {
                        // Extract the amount from the description using regex
                        var match = Regex.Match(submitItem.Description, @"(\d+)[^\d]+\d+\s?$");
                        var amountStr = match.Success ? match.Groups[1].Value : "err";
                        return new { submitItem, amountStr };
                    })
                )
                .ToDictionary(x => x.submitItem, x => x.amountStr);

            return requiredSubmitItems;
        }
    }
}
