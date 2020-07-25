﻿/*
 * RenderMap2D.cs - Handles 2D map rendering
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    internal class RenderMap2D : IRenderMap
    {
        public const int TILE_WIDTH = 16;
        public const int TILE_HEIGHT = 16;
        public const int NUM_VISIBLE_TILES_X = 11; // maps will always be at least 11x11 in size
        public const int NUM_VISIBLE_TILES_Y = 9; // maps will always be at least 11x11 in size
        const int NUM_TILES = NUM_VISIBLE_TILES_X * NUM_VISIBLE_TILES_Y;
        public Map Map { get; private set; } = null;
        Map[] adjacentMaps = null;
        Tileset tileset = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        ITextureAtlas textureAtlas = null;
        readonly List<IAnimatedSprite> backgroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        readonly List<IAnimatedSprite> foregroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        uint ticksPerFrame = 0;
        bool worldMap = false;
        uint lastFrame = 0;

        public uint ScrollX { get; private set; } = 0;
        public uint ScrollY { get; private set; } = 0;

        public RenderMap2D(Map map, IMapManager mapManager, IRenderView renderView,
            uint initialScrollX = 0, uint initialScrollY = 0)
        {
            this.mapManager = mapManager;
            this.renderView = renderView;

            var spriteFactory = renderView.SpriteFactory;

            for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var backgroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 0, 0);
                    var foregroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 0, 0);

                    backgroundSprite.Visible = true;
                    backgroundSprite.X = Global.MapViewX + column * TILE_WIDTH;
                    backgroundSprite.Y = Global.MapViewY + row * TILE_HEIGHT;
                    foregroundSprite.Visible = false;
                    foregroundSprite.X = Global.MapViewX + column * TILE_WIDTH;
                    foregroundSprite.Y = Global.MapViewY + row * TILE_HEIGHT;

                    backgroundTileSprites.Add(backgroundSprite);
                    foregroundTileSprites.Add(foregroundSprite);
                }
            }

            SetMap(map, initialScrollX, initialScrollY);
        }

        public void UpdateAnimations(uint ticks)
        {
            uint frame = ticks / ticksPerFrame;

            if (frame != lastFrame)
            {
                int index = 0;

                for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
                {
                    for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                    {
                        if (backgroundTileSprites[index].NumFrames != 1)
                            backgroundTileSprites[index].CurrentFrame = frame;
                        if (foregroundTileSprites[index].NumFrames != 1)
                            foregroundTileSprites[index].CurrentFrame = frame;
                        ++index;
                    }
                }

                lastFrame = frame;
            }
        }

        public void TriggerEvents(IRenderPlayer player, MapEventTrigger trigger, uint x, uint y, IMapManager mapManager, uint ticks)
        {
            if (x >= Map.Width)
            {
                if (y >= Map.Height)
                    adjacentMaps[2].TriggerEvents(player, trigger, x - (uint)Map.Width, y - (uint)Map.Height, mapManager, ticks);
                else
                    adjacentMaps[0].TriggerEvents(player, trigger, x - (uint)Map.Width, y, mapManager, ticks);
            }
            else if (y >= Map.Height)
            {
                adjacentMaps[1].TriggerEvents(player, trigger, x, y - (uint)Map.Height, mapManager, ticks);
            }
            else
            {
                Map.TriggerEvents(player, trigger, x, y, mapManager, ticks);
            }
        }

        public Position GetCenterPosition()
        {
            return new Position((int)ScrollX + NUM_VISIBLE_TILES_X / 2, (int)ScrollY + NUM_VISIBLE_TILES_Y / 2);
        }

        public Map.Tile this[uint x, uint y]
        {
            get
            {
                if (x >= Map.Width)
                {
                    if (y >= Map.Height)
                        return adjacentMaps[2].Tiles[x - Map.Width, y - Map.Height];
                    else
                        return adjacentMaps[0].Tiles[x - Map.Width, y];
                }
                else if (y >= Map.Height)
                {
                    return adjacentMaps[1].Tiles[x, y - Map.Height];
                }
                else
                {
                    return Map.Tiles[x, y];
                }
            }
        }

        void UpdateTiles()
        {
            var backLayer = renderView.GetLayer((Layer)((uint)Layer.MapBackground1 + tileset.Index - 1));
            var frontLayer = renderView.GetLayer((Layer)((uint)Layer.MapForeground1 + tileset.Index - 1));
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate((Layer)((uint)Layer.MapBackground1 + tileset.Index - 1));
            int index = 0;

            for (uint row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (uint column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var tile = this[ScrollX + column, ScrollY + row];

                    backgroundTileSprites[index].Layer = backLayer;
                    backgroundTileSprites[index].TextureAtlasWidth = textureAtlas.Texture.Width;
                    backgroundTileSprites[index].PaletteIndex = (byte)Map.PaletteIndex;
                    foregroundTileSprites[index].Layer = frontLayer;
                    foregroundTileSprites[index].TextureAtlasWidth = textureAtlas.Texture.Width;
                    foregroundTileSprites[index].PaletteIndex = (byte)Map.PaletteIndex;

                    if (tile.BackTileIndex == 0)
                    {
                        backgroundTileSprites[index].Visible = false;
                    }
                    else
                    {
                        var backTile = tileset.Tiles[(int)tile.BackTileIndex - 1];
                        var backGraphicIndex = backTile.GraphicIndex;
                        backgroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(backGraphicIndex - 1);
                        backgroundTileSprites[index].NumFrames = (uint)backTile.NumAnimationFrames;
                        backgroundTileSprites[index].CurrentFrame = 0;
                        backgroundTileSprites[index].Visible = true;
                    }

                    if (tile.FrontTileIndex == 0)
                    {
                        foregroundTileSprites[index].Visible = false;
                    }
                    else
                    {
                        var frontTile = tileset.Tiles[(int)tile.FrontTileIndex - 1];
                        var frontGraphicIndex = frontTile.GraphicIndex;
                        foregroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(frontGraphicIndex - 1);
                        foregroundTileSprites[index].NumFrames = (uint)frontTile.NumAnimationFrames;
                        foregroundTileSprites[index].CurrentFrame = 0;
                        foregroundTileSprites[index].Visible = true;
                    }

                    ++index;
                }
            }

            UpdateAnimations(0);
        }

        public void SetMap(Map map, uint initialScrollX = 0, uint initialScrollY = 0)
        {
            if (Map == map)
                return;

            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to load a 3D map into a 2D render map.");

            Map = map;
            tileset = mapManager.GetTilesetForMap(map);
            ticksPerFrame = map.TicksPerAnimationFrame;

            if (map.IsWorldMap)
            {
                worldMap = true;
                adjacentMaps = new Map[3]
                {
                    mapManager.GetMap(map.RightMapIndex.Value),
                    mapManager.GetMap(map.DownMapIndex.Value),
                    mapManager.GetMap(map.DownRightMapIndex.Value)
                };
            }
            else
            {
                worldMap = false;
                adjacentMaps = null;
            }

            ScrollTo(initialScrollX, initialScrollY, true); // also updates tiles etc
        }

        public bool Scroll(int x, int y)
        {
            int newScrollX = (int)ScrollX + x;
            int newScrollY = (int)ScrollY + y;

            if (worldMap)
            {
                if (newScrollX < 0 || newScrollY < 0 || newScrollX >= Map.Width || newScrollY >= Map.Height)
                {
                    Map newMap;

                    if (newScrollX < 0)
                        newMap = mapManager.GetMap(Map.LeftMapIndex.Value);
                    else if (newScrollX >= Map.Width)
                        newMap = mapManager.GetMap(Map.RightMapIndex.Value);
                    else
                        newMap = Map;

                    if (newScrollY < 0)
                        newMap = mapManager.GetMap(newMap.UpMapIndex.Value);
                    else if (newScrollY >= Map.Height)
                        newMap = mapManager.GetMap(newMap.DownMapIndex.Value);

                    uint newMapScrollX = newScrollX < 0 ? (uint)(Map.Width + newScrollX) : (uint)(newScrollX % Map.Width);
                    uint newMapScrollY = newScrollY < 0 ? (uint)(Map.Height + newScrollY) : (uint)(newScrollY % Map.Height);

                    SetMap(newMap, newMapScrollX, newMapScrollY);

                    return true;
                }
            }
            else
            {
                if (newScrollX < 0 || newScrollY < 0 || newScrollX > Map.Width - NUM_VISIBLE_TILES_X || newScrollY > Map.Height - NUM_VISIBLE_TILES_Y)
                    return false;
            }

            ScrollTo((uint)newScrollX, (uint)newScrollY);

            return true;
        }

        public void ScrollTo(uint x, uint y, bool forceUpdate = false)
        {
            if (!forceUpdate && ScrollX == x && ScrollY == y)
                return;

            ScrollX = x;
            ScrollY = y;

            if (!worldMap)
            {
                // check scroll offset for non-world maps
                if (ScrollX > Map.Width - NUM_VISIBLE_TILES_X)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll x position is outside the map bounds.");
                if (ScrollY > Map.Height - NUM_VISIBLE_TILES_Y)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll y position is outside the map bounds.");
            }

            UpdateTiles();
        }
    }
}