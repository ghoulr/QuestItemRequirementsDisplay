using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Utilities;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ballban
{
    static public class Utility
    {
        public static QuestCollection TotalQuests = GameplayDataSettings.QuestCollection;
        public static QuestManager QuestMangaer = QuestManager.Instance;

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
                        .Where(perk => !perk.Unlocked)
                        .SelectMany(perk => perk.Requirement.cost.items
                            .Where(itemEntry => itemEntry.id == item.TypeID)
                            .Select(itemEntry => new RequiredPerk
                            {
                                Amount = itemEntry.amount,
                                PerkTreeName = perkTree.DisplayName,
                                PerkName = perk.DisplayName
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
            var finishedQuestsId = QuestMangaer.HistoryQuests.Select(q => q.ID);

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
            var requiredSubmitItems = new Dictionary<SubmitItems, string>();
            var finishedQuestsId = QuestMangaer.HistoryQuests.Select(q => q.ID);

            foreach (Quest quest in TotalQuests)
            {
                // Skip if the quest is already completed
                if (finishedQuestsId.Contains(quest.ID)) continue;
                if (quest.Tasks == null) continue;

                foreach (var task in quest.Tasks)
                {
                    // If the task is a SubmitItems task and unfinished and matches the item type ID
                    if (task is SubmitItems submitItem && !submitItem.IsFinished() && submitItem.ItemTypeID == item.TypeID)
                    {
                        // Extract the amount from the description using regex
                        var amountStr = "err";
                        var match = Regex.Match(submitItem.Description, @"(\d+)[^\d]+\d+\s?$");
                        if (match.Success)
                        {
                            amountStr = match.Groups[1].Value;
                        }

                        requiredSubmitItems.Add(submitItem, amountStr);
                    }
                }
            }

            return requiredSubmitItems;
        }
    }
}
