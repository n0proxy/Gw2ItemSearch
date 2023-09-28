using Blish_HUD;
using Gw2Sharp.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemSearch
{
    public enum ItemRarity
    {
        Unknown = 0,
        Junk = 1,
        Basic,
        Fine,
        Masterwork,
        Rare,
        Exotic,
        Ascended,
        Legendary,
    };

    public enum AttributeName
    {
        Power,
        Precision,
        Toughness,
        Vitality,
        ConditionDamage,
        ConditionDuration,
        Healing,
        BoonDuration,
        CritDamage,
        AgonyResistance,
    }

    public class StaticItemInfo
    {
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public string ChatCode { get; set; }
        public ItemRarity Rarity { get; set; }
        public ItemType Type { get; set; }

        // Optional Fields
        [JsonProperty("SubType", NullValueHandling = NullValueHandling.Ignore)]
        public ItemSubType SubType { get; set; }

        [JsonProperty("StatAttributes", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<AttributeName, int> StatAttributes { get; set; }

        private static readonly Logger Logger = Logger.GetLogger<ItemSearchModule>();

        public static async Task Initialize(string cachePath, IGw2WebApiClient client)
        {
            Logger.Info($"Initializing StaticItemInfo");
            await ReadAllFromFile(cachePath);
            bool updated = await UpdateFromApi(client);
            if (updated)
            {
                Logger.Info($"Updated static items from API. Writing back to cache.");
                await WriteAllToFile(cachePath);
            }
        }

        public static async Task ReadAllFromFile(string path)
        {
            await Task.Run(() =>
            {
                AllItems = JsonConvert.DeserializeObject<Dictionary<int, StaticItemInfo>>(System.IO.File.ReadAllText(path));
            });
        }

        public static async Task WriteAllToFile(string path)
        {
            await Task.Run(() =>
            {
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(AllItems));
            });
        }

        public static async Task<bool> UpdateFromApi(IGw2WebApiClient client)
        {
            bool updated = false;
            var ids = await ApiHelper.Fetch(() => client.V2.Items.IdsAsync());

            if (ids == null)
            {
                Logger.Warn("Failed to update static items from API. No IDs.");
                return false;
            }

            List<int> idsToRead = new List<int>();
            foreach (var id in ids)
            {
                if (!AllItems.ContainsKey(id))
                {
                    idsToRead.Add(id);
                }

                // 200 items per request max, 190 to be safe
                if (idsToRead.Count > 190)
                {
                    if (!await ProcessIds(client, idsToRead))
                    {
                        break;
                    }
                    else
                    {
                        updated = true;
                    }
                }
            }

            // Process any remaining
            if (idsToRead.Count > 0)
            {
                if (await ProcessIds(client, idsToRead))
                {
                    updated = true;
                }
            }

            return updated;
        }

        private static async Task<bool> ProcessIds(IGw2WebApiClient client, List<int> ids)
        {
            Logger.Info($"Updating {ids.Count} static items");
            var items = await ApiHelper.Fetch(() => client.V2.Items.ManyAsync(ids));

            if (items == null)
            {
                Logger.Warn("Failed to fetch updated static items.");
                return false;
            }

            foreach (var item in items)
            {
                var staticItem = new StaticItemInfo();
                staticItem.Name = item.Name;
                staticItem.IconUrl = item.Icon.Url.ToString();
                staticItem.Rarity = RarityStringToRarity(item.Rarity.ToString());
                staticItem.ChatCode = item.ChatLink;
                AllItems.Add(item.Id, staticItem);
            }

            ids.Clear();
            return true;
        }

        private static ItemRarity RarityStringToRarity(string s)
        {
            switch (s)
            {
                case "Junk": return ItemRarity.Junk;
                case "Basic": return ItemRarity.Basic;
                case "Fine": return ItemRarity.Fine;
                case "Masterwork": return ItemRarity.Masterwork;
                case "Rare": return ItemRarity.Rare;
                case "Exotic": return ItemRarity.Exotic;
                case "Ascended": return ItemRarity.Ascended;
                case "Legendary": return ItemRarity.Legendary;
                default: return ItemRarity.Unknown;
            }
        }

        public static Dictionary<int, StaticItemInfo> AllItems { get; private set; }
    }

    #region Item enums

    public enum ItemType
    {
        Unknown,
        Armor,
        Back,
        Bag,
        Consumable,
        Container,
        CraftingMaterial,
        Gathering,
        Gizmo,
        Key,
        MiniPet,
        Tool,
        Trait,
        Trinket,
        Trophy,
        UpgradeComponent,
        Weapon,
        JadeBotCore,
        JadeBotChip,
        JadeBotModule,
    }

    public enum ItemSubType
    {
        Unknown = 0,
        Armor_Boots = 100,
        Armor_Coat,
        Armor_Gloves,
        Armor_Helm,
        Armor_HelmAquatic,
        Armor_Leggings,
        Armor_Shoulders,
        Consumable_AppearanceChange = 200,
        Consumable_Booze,
        Consumable_ContractNpc,
        Consumable_Currency,
        Consumable_Food,
        Consumable_Generic,
        Consumable_Halloween,
        Consumable_Immediate,
        Consumable_MountRandomUnlock,
        Consumable_RandomUnlock,
        Consumable_Transmutation,
        Consumable_Unlock,
        Consumable_UpgradeRemoval,
        Consumable_Utility,
        Consumable_TeleportToFriend,
        Container_Default = 300,
        Container_GiftBox,
        Container_Immediate,
        Container_OpenUI,
        Gathering_Foraging = 400,
        Gathering_Logging,
        Gathering_Mining,
        Gathering_Fishing,
        Gizmo_Default = 500,
        Gizmo_ContainerKey,
        Gizmo_RentableContractNpc,
        Gizmo_UnlimitedConsumable,
        Trinket_Accessory = 600,
        Trinket_Amulet,
        Trinket_Ring,
        UpgradeComponent_Default = 700,
        UpgradeComponent_Gem,
        UpgradeComponent_Rune,
        UpgradeComponent_Sigil,
        Weapon_Axe = 800,
        Weapon_Dagger,
        Weapon_Mace,
        Weapon_Pistol,
        Weapon_Scepter,
        Weapon_Sword,
        Weapon_Focus,
        Weapon_Shield,
        Weapon_Torch,
        Weapon_Warhorn,
        Weapon_Greatsword,
        Weapon_Hammer,
        Weapon_LongBow,
        Weapon_Rifle,
        Weapon_ShortBow,
        Weapon_Staff,
        Weapon_Harpoon,
        Weapon_Speargun,
        Weapon_Trident,
        Weapon_LargeBundle,
        Weapon_SmallBundle,
        Weapon_Toy,
        Weapon_ToyTwoHanded,
    }

    #endregion
}
