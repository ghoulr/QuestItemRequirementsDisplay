using Duckov.Buildings;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;

namespace QuestItemRequirementsDisplay
{
    static public class GetRequiredItemAmount
    {
        private static readonly HashSet<string> LoggedQuestLabels = new HashSet<string>();
        private static readonly object LogSync = new object();

        private static readonly Dictionary<int, IList<int>> QuestParentsById = new Dictionary<int, IList<int>>();
        private static readonly Dictionary<int, List<int>> QuestChildrenById = new Dictionary<int, List<int>>();

        // Trees to exclude from perk calculations:
        // - "Blueprint": weapon/blueprint unlocks; not considered character perk progression
        // - "PerkTree_Farming": farming-related tree currently out of scope
        private static readonly HashSet<string> DeniedPerkTreeIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "Blueprint",
            "PerkTree_Farming",
        };

        public static List<Quest> TotalQuests = InitTotalQuests();

        /// <summary>
        /// Initialize the total quests list, excluding testing quests.
        /// </summary>
        /// <returns></returns>
        private static List<Quest> InitTotalQuests()
        {
            var allQuests = GameplayDataSettings.QuestCollection
                .ConvertTo<List<Quest>>();

            RebuildQuestGraphCache(allQuests);


            var excludedByGraph = ComputeExcludedIdsByRootRule(allQuests);

            var graphFiltered = allQuests
                .Where(q => q != null && !excludedByGraph.Contains(q.ID))
                .ToList();

            return graphFiltered;
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
                .Where(perkTree => !DeniedPerkTreeIds.Contains(perkTree.ID))
                .SelectMany(perkTree => perkTree.Perks
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

        private static object? GetQuestRelation()
        {
            var relationProperty = typeof(GameplayDataSettings).GetProperty("QuestRelation", BindingFlags.Public | BindingFlags.Static);
            return relationProperty?.GetValue(null);
        }

        private static IList<int> GetParentIds(int questId)
        {
            var relation = GetQuestRelation();
            if (relation == null) return new List<int>();
            var getRequiredIdsMethod = relation.GetType().GetMethod("GetRequiredIDs");
            var parentsObj = getRequiredIdsMethod?.Invoke(relation, new object[] { questId }) as System.Collections.IEnumerable;
            if (parentsObj == null) return new List<int>();
            return parentsObj.Cast<object>().Select(o => Convert.ToInt32(o)).ToList();
        }

        private static IList<int> GetChildrenIds(int questId)
        {
            if (QuestChildrenById.TryGetValue(questId, out var children) && children != null)
            {
                return new List<int>(children);
            }
            return new List<int>();
        }

        private static HashSet<int> ComputeExcludedIdsByRootRule(IEnumerable<Quest> quests)
        {
            RebuildQuestGraphCache(quests);

            var excluded = new HashSet<int>();

            foreach (var quest in quests)
            {
                if (quest == null)
                {
                    continue;
                }

                var parents = QuestParentsById.TryGetValue(quest.ID, out var cachedParents) ? cachedParents : new List<int>();
                var isRoot = parents == null || parents.Count == 0;
                if (!isRoot) continue;

                if (quest.RequireLevel >= 100)
                {
                    var stack = new Stack<int>();
                    stack.Push(quest.ID);

                    while (stack.Count > 0)
                    {
                        var cur = stack.Pop();
                        if (!excluded.Add(cur))
                        {
                            continue;
                        }

                        if (!QuestChildrenById.TryGetValue(cur, out var children) || children == null)
                        {
                            continue;
                        }

                        foreach (var child in children)
                        {
                            if (!excluded.Contains(child))
                            {
                                stack.Push(child);
                            }
                        }
                    }
                }
            }

            return excluded;
        }

        private static void RebuildQuestGraphCache(IEnumerable<Quest> quests)
        {
            QuestParentsById.Clear();
            QuestChildrenById.Clear();

            foreach (var quest in quests)
            {
                if (quest == null)
                {
                    continue;
                }

                var parents = GetParentIds(quest.ID);
                QuestParentsById[quest.ID] = parents;

                foreach (var parent in parents)
                {
                    if (!QuestChildrenById.TryGetValue(parent, out var children))
                    {
                        children = new List<int>();
                        QuestChildrenById[parent] = children;
                    }

                    if (!children.Contains(quest.ID))
                    {
                        children.Add(quest.ID);
                    }
                }
            }
        }

        private static string FormatTaskSummary(Duckov.Quests.Task task)
        {
            if (task == null)
            {
                return "<null>";
            }

            var typeName = task.GetType().Name;

            if (task is SubmitItems submitItems)
            {
                var requiredAmountRef = AccessTools.FieldRefAccess<int>(typeof(SubmitItems), "requiredAmount");
                var submittedAmountRef = AccessTools.FieldRefAccess<int>(typeof(SubmitItems), "submittedAmount");
                return $"{typeName}(ItemTypeID={submitItems.ItemTypeID}, RequiredAmount={requiredAmountRef(submitItems)}, SubmittedAmount={submittedAmountRef(submitItems)})";
            }

            if (task is QuestTask_UseItem questTaskUseItem)
            {
                var itemTypeIDRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "itemTypeID");
                var requireAmountRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "requireAmount");
                return $"{typeName}(ItemTypeID={itemTypeIDRef(questTaskUseItem)}, RequiredAmount={requireAmountRef(questTaskUseItem)})";
            }

            return typeName;
        }
    }
}
