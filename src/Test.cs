using Duckov.Achievements;
using Duckov.Buildings;
using Duckov.Modding;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using UnityEngine;

namespace QuestItemRequirementsDisplay
{
    internal static class Test
    {
#if DEBUG
        // Test quest: 26 - 信号塔
        private static readonly List<int> TEST_REQUIRED_ITEM_QUEST_IDS = new List<int> { 26 };
        private static readonly List<int> TEST_REQUIRED_SUBMIT_ITEMS_QUEST_IDS = new List<int> { };
        // Test quest: 906 - 药物试验
        private static readonly List<int> TEST_REQUIRED_USE_ITEM_QUEST_IDS = new List<int> { 906 };
        private static readonly List<int> TEST_REQUIRED_PERK_ITEM_IDS = new List<int> { };
        private static readonly List<int> TEST_REQUIRED_BUILDING_ITEM_IDS = new List<int> { };

        public enum TestType
        {
            Quest,
            SubmittingQuest,
            UseQuest,
            Perk,
            Building,
            Unknown
        }

        public static void RunTests()
        {
            Debug.Log("---------------------------------------- Running tests... ----------------------------------------");
            //if (ModB == null)
            //{
            //    Debug.LogError("ModBehaviour is null. Cannot run tests.");
            //    return;
            //}
            //Debug.Log($"Mod name: {ModB.name}");
            Debug.Log("-------------------- Print Quest File --------------------");
            PrintAllQuest();
            Debug.Log("-------------------- End Print Quest File --------------------");
            Debug.Log("-------------------- Print Quest Relation --------------------");
            PrintQuestRelation();
            Debug.Log("-------------------- End Print Quest Relation --------------------");
            Debug.Log("---------------------------------------- All tests ended. ----------------------------------------");
        }

        public class QuestInfo
        {
            public int QuestId;
            public string DisplayName;
            public string Description;
            public TestType TestType;
            public List<ItemInfo> Items;
        }
        public class ItemInfo
        {
            public int ItemId;
            public string DisplayName;
            public int Amount;
        }
        public static void PrintAllQuest()
        {
            Debug.Log("-------------------- Get All Quest Info --------------------");
            var questList = GetAllQuestInfo();
            Debug.Log("-------------------- Get All Perk Info --------------------");
            questList.AddRange(GetAllPerkInfo());
            Debug.Log("-------------------- Get All Building Info --------------------");
            questList.AddRange(GetAllBuildingInfo());
            var filePath = Path.Combine(Application.dataPath, "Mods", "QuestItemRequirementsDisplay", "AllQuestInfo_Quest.xml");
            var serializer = new XmlSerializer(typeof(List<QuestInfo>));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(stream, questList);
            }
        }
        private static List<QuestInfo> GetAllQuestInfo()
        {
            var questRelation = GameplayDataSettings.QuestRelation;
            
            var questList = new List<QuestInfo>();
            var totalQuests = GetRequiredItemAmount.TotalQuests;
            if (totalQuests == null)
            {
                Debug.LogError("GetRequiredItemAmount.TotalQuests is null. Cannot get quest info.");
                return questList;
            }
            foreach (var quest in totalQuests)
            {
                if (quest == null) continue;
                (var items, var testType) = GetItemInfos(quest);
                var questInfo = new QuestInfo
                {
                    QuestId = quest.ID,
                    DisplayName = $"DisplayName: {quest.DisplayName} - QuestGiverID: {quest.QuestGiverID} - GetRequiredIDs: {string.Join(", ", questRelation.GetRequiredIDs(quest.ID))} - RequireLevel: {quest.RequireLevel}",
                    Description = quest.Description,
                    Items = items,
                    TestType = testType
                };
                questList.Add(questInfo);
            }
            Debug.Log($"-------------------- Total quest info: {questList.Count} --------------------");
            return questList;
        }
        private static (List<ItemInfo>, TestType) GetItemInfos(Quest quest)
        {
            var questInfo = new List<ItemInfo>();
            var testType = TestType.Unknown;

            try
            {
                if (quest.RequiredItemID != 0)
                {
                    testType = TestType.Quest;
                    questInfo.Add(new ItemInfo
                    {
                        ItemId = quest.RequiredItemID,
                        DisplayName = GetItemDisplayName(quest.RequiredItemID),
                        Amount = quest.RequiredItemCount
                    });
                }
                else if (quest.Tasks != null && quest.Tasks.OfType<SubmitItems>().Any())
                {
                    testType = TestType.SubmittingQuest;
                    var requiredAmountRef = AccessTools.FieldRefAccess<int>(typeof(SubmitItems), "requiredAmount");
                    questInfo.AddRange(quest.Tasks
                        .Where(task => task != null)
                        .OfType<SubmitItems>()
                        .Where(submitItem => submitItem != null)
                        .Select(submitItem => new ItemInfo
                        {
                            ItemId = submitItem.ItemTypeID,
                            Amount = requiredAmountRef(submitItem),
                            DisplayName = GetItemDisplayName(submitItem.ItemTypeID)
                        })
                    );
                }
                else if (quest.Tasks != null && quest.Tasks.OfType<QuestTask_UseItem>().Any())
                {
                    testType = TestType.UseQuest;
                    var itemTypeIDRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "itemTypeID");
                    var ItemDisplayNameRef = AccessTools.FieldRefAccess<string>(typeof(QuestTask_UseItem), "itemDisplayName");
                    var requireAmountRef = AccessTools.FieldRefAccess<int>(typeof(QuestTask_UseItem), "requireAmount");
                    questInfo.AddRange(quest.Tasks
                        .Where(task => task != null)
                        .OfType<QuestTask_UseItem>()
                        .Where(useItem => useItem != null)
                        .Select(useItem => {
                            return new ItemInfo {
                                ItemId = itemTypeIDRef(useItem),
                                DisplayName = ItemDisplayNameRef(useItem),
                                Amount = requireAmountRef(useItem)
                            };
                        })
                    );
                }
                else
                {
                    testType = TestType.Unknown;
                };
                return (questInfo, testType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting item infos for quest ID: {quest.ID}, TestType: {testType}, err: {e.Message}");
                return (questInfo, testType);
            }
        }
        private static List<QuestInfo> GetAllPerkInfo()
        {
            var questList = new List<QuestInfo>();
            if (PerkTreeManager.Instance == null) return questList;
            questList.AddRange(PerkTreeManager.Instance.perkTrees
                .Where(perkTree => perkTree.Perks != null)
                .SelectMany(perkTree => perkTree.Perks
                    .Where(perk => perk != null && perk.Requirement != null && perk.Requirement.cost.items != null)
                    .Select(perk => new QuestInfo
                    {
                        QuestId = -1,
                        DisplayName = $"perk.DisplayName: {perk.DisplayName} - perkTree.DisplayName: {perkTree.DisplayName} - perkTree.ID: {perkTree.ID}",
                        Description = perk.Description,
                        TestType = TestType.Perk,
                        Items = perk.Requirement.cost.items
                            .Select(itemEntry => new ItemInfo
                            {
                                ItemId = itemEntry.id,
                                DisplayName = $"itemEntry.id: {itemEntry.id} - Displayname: {GetItemDisplayName(itemEntry.id)}",
                                Amount = (int)itemEntry.amount
                            }).ToList()
                    })
                )
            );
            Debug.Log($"-------------------- Total perk info: {questList.Count} --------------------");
            return questList;
        }
        private static List<QuestInfo> GetAllBuildingInfo()
        {
            var questList = new List<QuestInfo>();
            if (BuildingDataCollection.Instance == null) return questList;
            questList.AddRange(BuildingDataCollection.Instance.Infos
                .Where(info => info.cost.items != null)
                .Select(info => new QuestInfo
                {
                    QuestId = -2,
                    DisplayName = $"info.DisplayName: {info.DisplayName} - info.id: {info.id} - info.requireBuildings: {string.Join(", ", info.requireBuildings)}",
                    Description = info.Description,
                    TestType = TestType.Building,
                    Items = info.cost.items
                        .Select(itemEntry => new ItemInfo
                        {
                            ItemId = itemEntry.id,
                            DisplayName = GetItemDisplayName(itemEntry.id),
                            Amount = (int)itemEntry.amount
                        }).ToList()
                })
            );
            Debug.Log($"-------------------- Total building info: {questList.Count} --------------------");
            return questList;
        }
        private static string GetItemDisplayName(int itemID)
        {
            var displayName = string.Empty;
            if (ItemAssetsCollection.Instance == null) return displayName;
            var itemMetaData = ItemAssetsCollection.GetMetaData(itemID);
            displayName = itemMetaData.DisplayName;
            return displayName;
        }
        private static void PrintQuestRelation()
        {
            var questChildParentsDict = new Dictionary<int, IList<int>>();
            var rootQuests = new List<Quest>();
            var totalQuests = GetRequiredItemAmount.TotalQuests;
            foreach (var quest in totalQuests)
            {
                var parentIds = GetParentIds(quest.ID);
                if (parentIds.Count == 0) rootQuests.Add(quest);
                else questChildParentsDict.Add(quest.ID, GetParentIds(quest.ID));
            }

            var questParentChildrenDict = new Dictionary<int, IList<int>>();
            foreach (var kvp in questChildParentsDict)
            {
                var childId = kvp.Key;
                var parentIds = kvp.Value;
                foreach (var parentId in parentIds)
                {
                    if (!questParentChildrenDict.ContainsKey(parentId))
                    {
                        questParentChildrenDict[parentId] = new List<int>();
                    }
                    questParentChildrenDict[parentId].Add(childId);
                }
            }

            var filePath = Path.Combine(Application.dataPath, "Mods", "QuestItemRequirementsDisplay", "QuestsRelation.txt");
            using (var writer = new StreamWriter(filePath, false))
            {
                var t = string.Empty;
                t = $"-------------------- rootQuests count: {rootQuests.Count} --------------------";
                Debug.Log(t);
                writer.WriteLine(t);

                t = $"root Quests List:\n{string.Join("\n", rootQuests.Select(q => q.DisplayName + "(" + q.ID + ")"))}";
                Debug.Log(t);
                writer.WriteLine(t);

                foreach (var rootQuest in rootQuests)
                {
                    t = $"Root Quest: {rootQuest.DisplayName}({rootQuest.ID})";
                    Debug.Log(t);
                    writer.WriteLine(t);
                    PrintQuestTree(writer, questParentChildrenDict, rootQuest.ID);
                }
                writer.Close();
                Debug.Log($"Quest relation tree printed to: {filePath}");
            }
        }
        private static void PrintQuestTree(StreamWriter writer, Dictionary<int, IList<int>> questParentChildrenDict, int questId, int depth = 1)
        {
            var children = questParentChildrenDict.TryGetValue(questId, out var v) ? v : new List<int>();
            var t = $"{new string(' ', depth * 2)}- {GetQuestDisplayName(questId)}({questId})";
            Debug.Log(t);
            writer.WriteLine(t);

            foreach (var childId in children)
            {
                PrintQuestTree(writer, questParentChildrenDict, childId, depth + 1);
            }
        }
        private static IList<int> GetParentIds(int questId)
        {
            var questRelation = GameplayDataSettings.QuestRelation;
            if (questRelation == null) return new List<int>();
            var requiredIDs = questRelation.GetRequiredIDs(questId);
            return requiredIDs;
        }
        private static string GetQuestDisplayName(int questID)
        {
            var quest = GetRequiredItemAmount.TotalQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null) return string.Empty;
            return quest.DisplayName;
        }

        //private static string GetText(Item itemID)
        //{
        //    // Get required item information
        //    (var requiredQuestText, var requiredQuestItemAmount) = ModB.GetRequiredQuestText(item);
        //    (var requiredSubmitQuestText, var requiredSubmittingQuestItemAmount) = ModB.GetRequiredSubmittingQuestText(item);
        //    (var requiredUseQuestText, var requiredUseQuestItemAmount) = ModB.GetRequiredUseQuestText(item);
        //    (var requiredPerkText, var requiredPerkItemAmount) = ModB.GetRequiredPerkText(item);
        //    (var requiredBuildingText, var requiredBuildingItemAmount) = ModB.GetRequiredBuildingText(item);
        //}
#endif
    }
}
