using SodaCraft.Localizations;
using System.Collections.Generic;
using UnityEngine;

namespace ballban
{
    public static class LocalizedText
    {
        /// <summary>
        /// Dictionary of localized strings for different languages.
        /// </summary>
        public static readonly Dictionary<string, string> Dic = new Dictionary<string, string>
        {
            { "GetRequiredQuestText_ChineseSimplified", "需要准备该物品的任务:" },
            { "GetRequiredQuestText_ChineseTraditional", "需要準備該物品的任務:" },
            { "GetRequiredQuestText_Japanese", "このアイテムが必要なクエスト:" },
            { "GetRequiredQuestText_Korean", "이아이템이 필요한 퀘스트:" },
            { "GetRequiredQuestText_Default", "Quests required this item:" },

            { "GetRequiredSubmittingQuestText_ChineseSimplified", "需要提交该物品的任务:" },
            { "GetRequiredSubmittingQuestText_ChineseTraditional", "需要提交該物品的任務:" },
            { "GetRequiredSubmittingQuestText_Japanese", "このアイテム納品が必要なクエスト:" },
            { "GetRequiredSubmittingQuestText_Korean", "이아이템 재출이 필요한 퀘스트:" },
            { "GetRequiredSubmittingQuestText_Default", "Quests required submit this item:" },

            { "GetRequiredPerkText_ChineseSimplified", "需要该物品解锁的天赋:" },
            { "GetRequiredPerkText_ChineseTraditional", "需要該物品解鎖的天賦:" },
            { "GetRequiredPerkText_Japanese", "このアイテムが必要なスキル:" },
            { "GetRequiredPerkText_Korean", "이 아이템이 필요한 스킬:" },
            { "GetRequiredPerkText_Default", "Perks required this item:" },

            { "GetRequiredBuildingText_ChineseSimplified", "需要该物品解锁的建筑:" },
            { "GetRequiredBuildingText_ChineseTraditional", "需要該物品解鎖的建築:" },
            { "GetRequiredBuildingText_Japanese", "このアイテムが必要な建物:" },
            { "GetRequiredBuildingText_Korean", "이 아이템이 필요한 건물:" },
            { "GetRequiredBuildingText_Default", "Buildings required this item:" },

            { "totalRequiredItemCount_ChineseSimplified", "该物品总需求个数:" },
            { "totalRequiredItemCount_ChineseTraditional", "該物品總需求個數:" },
            { "totalRequiredItemCount_Japanese", "このアイテムの合計必要数:" },
            { "totalRequiredItemCount_Korean", "이 아이템의 필요 개수:" },
            { "totalRequiredItemCount_Default", "Total required amount of this item:" },

            { "pressShift_ChineseSimplified", "按下 Shift" },
            { "pressShift_ChineseTraditional", "按下 Shift" },
            { "pressShift_Japanese", "Shift キーを押して" },
            { "pressShift_Korean", "Shift 를 눌러주세요" },
            { "pressShift_Default", "Press Shift" },
        };

        /// <summary>
        /// Get the localized string for the given key and language.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(string key, bool isChangLine = true)
        {
            var currentLanguage = LocalizationManager.CurrentLanguage;
            var systemLanguageStr = currentLanguage switch
            {
                SystemLanguage.Chinese => "ChineseSimplified",
                SystemLanguage.ChineseSimplified => currentLanguage.ToString(),
                SystemLanguage.ChineseTraditional => currentLanguage.ToString(),
                SystemLanguage.Japanese => currentLanguage.ToString(),
                SystemLanguage.Korean => currentLanguage.ToString(),
                _ => "Default",
            };
            var text = Dic.TryGetValue($"{key}_{systemLanguageStr}", out var value) ? value : "";
            text = isChangLine ? $"\n{text}" : text;
            return text;
        }
    }
}
