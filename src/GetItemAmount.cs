using HarmonyLib;
using ItemStatsSystem;
using System.Collections.Generic;
using System.Linq;

namespace QuestItemRequirementsDisplay
{
    [HarmonyPatch(typeof(PlayerStorage), "OnDestroy")]
    public class PlayerStorageOnDestroyPatch
    {
        static void Prefix(PlayerStorage __instance)
        {
            GetItemAmount.ClonePlayerStorageInventory();
        }
    }

    public class GetItemAmount
    {
        // Cached copy of the player's storage inventory
        public static List<Item> PlayerStorageInventoryItems = new List<Item>();

        /// <summary>
        /// Clone the player's storage inventory to avoid null reference issues
        /// </summary>
        public static void ClonePlayerStorageInventory()
        {
            if (PlayerStorage.Inventory == null || PlayerStorage.Inventory.Content == null) return;
            PlayerStorageInventoryItems.Clear();
            PlayerStorageInventoryItems.AddRange(PlayerStorage.Inventory.Content.FindAll(item => item != null));
            PlayerStorageInventoryItems.AddRange(PlayerStorage.Inventory.Content
                .FindAll(item => item != null && item.Slots != null && item.Slots.list != null)
                .SelectMany(item => item.Slots.list
                    .Where(slot => slot != null && slot.Content != null)
                    .Select(slot => slot.Content)
                )
            );
        }

        /// <summary>
        /// Get the total amount of the specified item type ID across all inventories.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int GetTotalItemAmount(int typeID)
        {
            var totalAmount = InCharacterInventory(typeID)
                            + InPetInventory(typeID)
                            + InPlayerStorage(typeID);
            return totalAmount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the character's inventory.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InCharacterInventory(int typeID)
        {
            var inventory = LevelManager.Instance.MainCharacter.CharacterItem.Inventory;
            var amount = FromInventory(inventory, typeID);
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the pet's inventory.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InPetInventory(int typeID)
        {
            var inventory = LevelManager.Instance.PetProxy.Inventory;
            var amount = FromInventory(inventory, typeID);
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the player's storage.
        /// </summary>
        /// <param name="typeID"></param>
        /// <returns></returns>
        public static int InPlayerStorage(int typeID)
        {
            var amount = 0;
            if (PlayerStorage.Inventory == null)
            {
                amount += FromInventory(PlayerStorageInventoryItems, typeID);
            }
            else
            {
                amount += FromInventory(PlayerStorage.Inventory, typeID);
            }
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the given inventory.
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="typeID"></param>
        /// <returns></returns>
        private static int FromInventory(Inventory inventory, int typeID)
        {
            if (inventory == null) return 0;

            return FromInventory(inventory.FindAll(item => item != null), typeID);
        }
        private static int FromInventory(List<Item> inventory, int typeID)
        {
            var items = inventory.FindAll(item => item.TypeID == typeID);
            var amount = items.Sum(item => item.StackCount);
            amount += FromItemsSlots(inventory, typeID);
            return amount;
        }

        /// <summary>
        /// Get the total amount of the specified item type ID in the given inventory's item slots.
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="typeID"></param>
        /// <returns></returns>
        private static int FromItemsSlots(List<Item> inventory, int typeID)
        {
            var amount = inventory
                .Where(item => item.Slots != null && item.Slots.list != null)
                .SelectMany(item => item.Slots.list.FindAll(slot => slot != null && slot.Content != null))
                .Where(slot => slot.Content.TypeID == typeID)
                .Sum(slot => slot.Content.StackCount);
            return amount;
        }
    }
}
