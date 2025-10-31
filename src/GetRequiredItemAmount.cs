using Duckov.Buildings;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

namespace QuestItemRequirementsDisplay
{
    static public class GetRequiredItemAmount
    {
        // Trees to exclude from perk calculations:
        // - "Blueprint": weapon/blueprint unlocks; not considered character perk progression
        // - "PerkTree_Farming": farming-related tree currently out of scope
        private static readonly HashSet<string> ExcludedPerkTreeIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "Blueprint",
            "PerkTree_Farming",
        };

        public static List<Quest> TotalQuests = InitTotalQuests();

        /// <summary>
        /// Initialize the total quests list, excluding unimplemented quests and their children, as well as testing quests.
        /// </summary>
        /// <returns></returns>
        private static List<Quest> InitTotalQuests()
        {
            var unimplementedLevelRequirement = 999;
            var questCollection = GameplayDataSettings.QuestCollection
                .ConvertTo<List<Quest>>()
                .Where(q => q != null);

            // Build a parent-to-children mapping
            var parentToChildren = new Dictionary<int, List<int>>();
            var excludedQuests = new HashSet<int>();
            foreach (var quest in questCollection)
            {
                var parentIds = GetParentQuestIds(quest.ID);
                // First exclude quests with a high level requirement (unimplemented quests)
                if (quest.RequireLevel >= unimplementedLevelRequirement)
                {
                    excludedQuests.Add(quest.ID);
                }
                // Build parent-to-children mapping
                foreach (var parentId in parentIds)
                {
                    if (!parentToChildren.TryGetValue(parentId, out var children))
                        parentToChildren[parentId] = children = new List<int>();
                    children.Add(quest.ID);
                }
            }

            // Recursive function to exclude all children of a given quest
            void ExcludeChildren(int questId)
            {
                if (parentToChildren.TryGetValue(questId, out var children))
                {
                    foreach (var childId in children)
                    {
                        if (excludedQuests.Add(childId)) ExcludeChildren(childId);
                    }
                }
            }

            // Recursively exclude all children of excluded quests
            foreach (var questId in excludedQuests.ToList())
            {
                ExcludeChildren(questId);
            }

            var filteredQuests = questCollection
                // Filter out excluded quests
                .Where(q => !excludedQuests.Contains(q.ID))
                // Filter out testing quests
                .Where(q => !IsTestingObjectDisplayName(q.DisplayName))
                .ToList();

            return filteredQuests;
        }

        /// <summary>
        /// Get the parent quest IDs for the specified quest ID.
        /// </summary>
        /// <param name="questId"></param>
        /// <returns></returns>
        private static List<int> GetParentQuestIds(int questId)
        {
            var questRelation = GameplayDataSettings.QuestRelation;
            if (questRelation == null) return new List<int>();
            var parentQuestIds = questRelation.GetRequiredIDs(questId);
            return parentQuestIds;
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
                // Exclude specified perk trees
                .Where(perkTree => !ExcludedPerkTreeIds.Contains(perkTree.ID))
                .SelectMany(perkTree => perkTree.Perks
                    // Select only locked perks with requirements
                    .Where(perk => perk != null && !perk.Unlocked && perk.Requirement != null && perk.Requirement.cost.items != null)
                    .SelectMany(perk => perk.Requirement.cost.items
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
            // PetHouse is excluded because it's not been implemented yet
            var excludedBuildingId = "PetHouse";
            var collection = BuildingDataCollection.Instance;
            if (collection == null)
            {
                return new List<RequiredBuilding>();
            }

            var result = collection.Infos
                // Check building's id is not null or empty
                .Where(info => info.Valid)
                // Check if the building is not yet placed
                .Where(info => info.CurrentAmount == 0)
                // Exclude testing buildings
                .Where(info => !IsTestingObjectDisplayName(info.DisplayName))
                // Exclude the buildings which requireBuildings is set and contains the excludedBuildingId
                // Pass the buildings that do not have requireBuildings set
                .Where(info => info.requireBuildings == null || info.requireBuildings.Length == 0 || !info.requireBuildings.Contains(excludedBuildingId))
                .SelectMany(info => info.cost.items
                    .Where(itemEntry => itemEntry.id == item.TypeID)
                    .Select(itemEntry => new RequiredBuilding
                    {
                        Amount = itemEntry.amount,
                        BuildingName = info.DisplayName
                    }))
                .ToList();

            return result;
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
