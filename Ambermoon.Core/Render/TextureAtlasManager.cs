﻿/*
 * TextureAtlasManager.cs - Manages texture atlases
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Render
{
    public class TextureAtlasManager
    {
        static TextureAtlasManager instance = null;
        static ITextureAtlasBuilderFactory factory = null;
        readonly Dictionary<Layer, ITextureAtlasBuilder> atlasBuilders = new Dictionary<Layer, ITextureAtlasBuilder>();
        readonly Dictionary<Layer, ITextureAtlas> atlas = new Dictionary<Layer, ITextureAtlas>();

        public static TextureAtlasManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new TextureAtlasManager();

                return instance;
            }
        }

        TextureAtlasManager()
        {

        }

        public static void RegisterFactory(ITextureAtlasBuilderFactory factory)
        {
            TextureAtlasManager.factory = factory;
        }

        // Note: Animation frames must be added as one large compound graphic.
        void AddTexture(Layer layer, uint index, Graphic texture)
        {
            if (factory == null)
                throw new AmbermoonException(ExceptionScope.Application, "No TextureAtlasBuilderFactory was registered.");

            if (atlas.ContainsKey(layer))
                throw new AmbermoonException(ExceptionScope.Application, $"Texture atlas already created for layer {layer}.");

            if (!atlasBuilders.ContainsKey(layer))
                atlasBuilders.Add(layer, factory.Create());

            atlasBuilders[layer].AddTexture(index, texture);
        }

        public ITextureAtlas GetOrCreate(Layer layer)
        {
            if (!atlas.ContainsKey(layer))
                atlas.Add(layer, atlasBuilders[layer].Create());

            return atlas[layer];
        }

        public void AddAll(IGameData gameData, IGraphicProvider graphicProvider)
        {
            if (gameData == null)
                throw new ArgumentNullException(nameof(gameData));

            if (graphicProvider == null)
                throw new ArgumentNullException(nameof(graphicProvider));

            #region Map

            //gameData.Files["1Icon"]

            //AddTexture(layer, )

            #endregion

            #region Player

            var playerGraphics = graphicProvider.GetGraphics(GraphicType.Player);

            if (playerGraphics.Count != 3 * 17)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong number of player graphics.");

            // There are 3 player characters (one for each world, first lyramion, second forest moon, third morag).
            // Each has 17 frames: 3 back, 3 right, 3 front, 3 left, 1 sit back, 1 sit right, 1 sit front, 1 sit left, 1 bed/sleep.
            // All have a dimension of 16x32 pixels.
            for (int i = 0; i < 3; ++i)
            {
                var playerGraphic = Graphic.CreateCompoundGraphic(playerGraphics.Skip(i * 17).Take(17));

                AddTexture(Layer.Player, (uint)i, playerGraphic);
            }

            #endregion

        }
    }
}
