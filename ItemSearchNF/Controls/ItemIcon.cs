﻿using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemSearch.Controls
{
    public class ItemIcon : Control
    {
        private static readonly Logger Logger = Logger.GetLogger<ItemSearchModule>();

        private static readonly BitmapFont s_NumberFont = GameService.Content.DefaultFont16;
        private static readonly Color s_NumberColor = Color.LemonChiffon;
        private static readonly Thickness s_NumberMargin = new Thickness(0, -5, 0, 0);

        private static Dictionary<ItemRarity, Texture2D> s_rarityToBorder = new Dictionary<ItemRarity, Texture2D>();
        public static async Task LoadIconResources()
        {
            await Task.Run(() =>
            {
                s_rarityToBorder.Add(ItemRarity.Junk, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\JunkBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Basic, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\BasicBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Fine, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\FineBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Masterwork, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\MasterworkBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Rare, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\RareBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Exotic, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\ExoticBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Ascended, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\AscendedBorder.png"));
                s_rarityToBorder.Add(ItemRarity.Legendary, ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\LegendaryBorder.png"));
            });
        }

        AsyncTexture2D m_image;
        InventoryItem m_item;
        StaticItemInfo m_itemInfo = null;
        string m_number = "";

        public ItemIcon(InventoryItem item)
        {
            m_item = item;
            this.Size = new Point(61, 61);
            m_image = ItemSearchModule.Instance.ContentsManager.GetTexture(@"Textures\EmptyItem.png");
            Tooltip = new Tooltip(new ItemTooltipView(m_item));
            if (m_item.Count > 1 || (m_item.Charges != null && m_item.Charges > 1))
            {
                m_number = Math.Max(m_item.Count, m_item.Charges ?? 1).ToString();
            }
            LoadItemImage();
        }

        public async Task LoadItemImage()
        {
            if (StaticItemInfo.AllItems.TryGetValue(m_item.Id, out m_itemInfo))
            {
                try
                {
                    var imageBytes = await ItemSearchModule.Instance.Gw2ApiManager.Gw2ApiClient.Render.DownloadToByteArrayAsync(m_itemInfo.IconUrl);
                    using (var textureStream = new MemoryStream(imageBytes))
                    {
                        var loadedTexture = InternalTextureUtil.FromStreamPremultiplied(textureStream);
                        m_image.SwapTexture(loadedTexture);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed loading item image {m_itemInfo.IconUrl}");
                }
                Invalidate();
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, m_image, bounds);
            if (m_itemInfo != null)
            {
                spriteBatch.DrawOnCtrl(this, s_rarityToBorder[m_itemInfo.Rarity], bounds);

                if (m_number.Length > 0)
                {
                    spriteBatch.DrawStringOnCtrl(this, m_number, s_NumberFont, bounds.WithPadding(s_NumberMargin), s_NumberColor, false, true, 1, HorizontalAlignment.Right, VerticalAlignment.Top);
                }
            }
        }
    }
}