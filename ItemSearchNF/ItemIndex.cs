﻿using Blish_HUD;
using Gma.DataStructures.StringSearch;
using Gw2Sharp.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiModels = Gw2Sharp.WebApi.V2.Models;

namespace ItemSearch
{
    internal static class DictionaryExtensions
    {
        public static void AddOrUpdate(this Dictionary<int, List<InventoryItem>> dict, int id, InventoryItem item)
        {
            List<InventoryItem> existing;
            if (!dict.TryGetValue(id, out existing))
            {
                existing = new List<InventoryItem>();
                dict[id] = existing;
            }
            existing.Add(item);
        }
    }

    internal class ItemIndex
    {
        private static readonly Logger Logger = Logger.GetLogger<ItemSearchModule>();

        private UkkonenTrie<int> m_searchTrie;
        private Dictionary<int, List<InventoryItem>> m_playerItems;
        private IGw2WebApiClient m_apiClient;
        private List<ApiModels.TokenPermission> m_permissions;

        public async Task InitializeIndex(IGw2WebApiClient client, List<ApiModels.TokenPermission> permissions)
        {
            m_apiClient = client;
            m_permissions = permissions;
            Logger.Info("Token permissions: " + String.Join(", ", permissions));

            await BuildSearchTrie();
            m_playerItems = await GetPlayerItems();
        }

        public async Task BuildSearchTrie()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            m_searchTrie = new UkkonenTrie<int>(3);
            foreach (var kv in StaticItemInfo.AllItems)
            {
                m_searchTrie.Add(kv.Value.Name.ToLowerInvariant(), kv.Key);
            }

            Logger.Info($"BuildSearchTrie: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task<List<InventoryItem>> Search(string query)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var results = m_searchTrie.Retrieve(query);
            Logger.Info($"TrieSearch: {stopwatch.ElapsedMilliseconds}");

            List<InventoryItem> playerMatchingItems = new List<InventoryItem>();
            foreach (var id in results)
            {
                List<InventoryItem> items;
                if (m_playerItems.TryGetValue(id, out items))
                {
                    foreach (var item in m_playerItems[id])
                    {
                        playerMatchingItems.Add(item);
                    }
                }
            }
            Logger.Info($"Matched against player items: {stopwatch.ElapsedMilliseconds}");

            return playerMatchingItems;
        }

        public async Task<Dictionary<int, List<InventoryItem>>> GetPlayerItems()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Dictionary<int, List<InventoryItem>> allPlayerItems = new Dictionary<int, List<InventoryItem>>();
            var webApiClient = m_apiClient.V2;

            Action<InventoryItem> addItemToAllItems = (InventoryItem item) =>
            {
                allPlayerItems.AddOrUpdate(item.Id, item);
                if (item.Skin.HasValue && item.Skin.Value > 0)
                {
                    allPlayerItems.AddOrUpdate(item.Skin.Value, item);
                }
                if (item.Infusions != null)
                {
                    foreach (var infusionId in item.Infusions)
                    {
                        allPlayerItems.AddOrUpdate(infusionId, item);
                    }
                }
                if (item.Upgrades != null)
                {
                    foreach (var upgradeId in item.Upgrades)
                    {
                        allPlayerItems.AddOrUpdate(upgradeId, item);
                    }
                }
            };

            if (m_permissions.Contains(ApiModels.TokenPermission.Inventories))
            {
                var bankItems = await ApiHelper.Fetch(() => webApiClient.Account.Bank.GetAsync());
                if (bankItems != null)
                {
                    foreach (var item in bankItems)
                    {
                        if (item != null)
                        {
                            addItemToAllItems(new InventoryItem(item, InventoryItemSource.Bank));
                        }
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve bank items.");
                }

                var sharedInventoryItems = await ApiHelper.Fetch(() => webApiClient.Account.Inventory.GetAsync());
                if (sharedInventoryItems != null)
                {
                    foreach (var item in sharedInventoryItems)
                    {
                        if (item != null)
                        {
                            addItemToAllItems(new InventoryItem(item, InventoryItemSource.SharedInventory));
                        }
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve shared inventory items.");
                }

                var materials = await ApiHelper.Fetch(() => webApiClient.Account.Materials.GetAsync());
                if (materials != null)
                {
                    foreach (var item in materials)
                    {
                        addItemToAllItems(new InventoryItem(item));
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve materials.");
                }

                var characters = await ApiHelper.Fetch(() => webApiClient.Characters.AllAsync());
                if (characters != null)
                {
                    foreach (var character in characters)
                    {
                        if (character.Bags != null)
                        {
                            foreach (var bag in character.Bags)
                            {
                                if (bag != null)
                                {
                                    addItemToAllItems(new InventoryItem(bag.Id, InventoryItemSource.CharacterInventory, character.Name));
                                    foreach (var item in bag.Inventory)
                                    {
                                        if (item != null)
                                        {
                                            addItemToAllItems(new InventoryItem(item, InventoryItemSource.CharacterInventory, character.Name));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Warn("Failed to retrieve character bags");
                        }

                        if (character.EquipmentTabs != null)
                        {
                            foreach (var tab in character.EquipmentTabs)
                            {
                                if (tab != null)
                                {
                                    foreach (var equipItem in tab.Equipment)
                                    {
                                        if (equipItem != null)
                                        {
                                            addItemToAllItems(new InventoryItem(equipItem, InventoryItemSource.CharacterEquipment, character.Name));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Warn("Failed to retrieve character equipment");
                        }
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve characters.");
                }
            }

            if (m_permissions.Contains(ApiModels.TokenPermission.Tradingpost))
            {
                var tpBox = (await ApiHelper.Fetch(() => webApiClient.Commerce.Delivery.GetAsync()));
                if (tpBox != null)
                {
                    var tpBoxItems = tpBox.Items;
                    foreach (var item in tpBoxItems)
                    {
                        addItemToAllItems(new InventoryItem(item));
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve trading post delivery box items.");
                }

                var tpSellItems = await ApiHelper.Fetch(() => webApiClient.Commerce.Transactions.Current.Sells.GetAsync());
                if (tpSellItems != null)
                {
                    foreach (var item in tpSellItems)
                    {
                        addItemToAllItems(new InventoryItem(item));
                    }
                }
                else
                {
                    Logger.Warn("Failed to retrieve trading post sell orders.");
                }
            }

            Logger.Info($"GetPlayerItems: {stopwatch.ElapsedMilliseconds}");

            return allPlayerItems;
        }
    }
}
