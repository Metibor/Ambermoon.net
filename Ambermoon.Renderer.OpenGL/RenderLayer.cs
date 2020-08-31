﻿/*
 * RenderLayer.cs - Render layer implementation
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

using Ambermoon.Render;
using Silk.NET.OpenGL;
using System;

namespace Ambermoon.Renderer
{
    public class RenderLayer : IRenderLayer, IDisposable
    {
        public Layer Layer { get; } = Layer.None;

        public bool Visible
        {
            get;
            set;
        }

        public PositionTransformation PositionTransformation
        {
            get;
            set;
        } = null;

        public SizeTransformation SizeTransformation
        {
            get;
            set;
        } = null;

        public Render.Texture Texture
        {
            get;
            set;
        } = null;

        internal RenderBuffer RenderBuffer { get; } = null;

        readonly State state = null;
        readonly RenderBuffer renderBufferColorRects = null;
        readonly Texture palette = null;
        bool disposed = false;

        // The back map layers and the front layers plus characters have a range of 0.3f for object y-ordering).
        // The UI background has a range of 0.1f for UI layers.
        // The battle monster rows use range of 0.01f (more right monsters are drawn above their left neighbors).
        // The UI foreground (like controls and borders) has a range of 0.2f for UI layers.
        // Items use basically the same layer (range 0.01f) as they won't overlap (except for the dragged one).
        // Texts are used in UI (below dragged items or as item amount text, tool tips, popup texts, etc).
        // Therefore items, texts and popups share the same base z-value and can use display layers within that
        // range to handle overlapping correctly.
        // The order for these 3 should be:
        // - Normal items and texts (including item amount text).
        // - Item tooltips
        // - Dragged item + its amount text
        // - Popup background
        // - Popup UI elements and text
        private static readonly float[] LayerBaseZ = new float[]
        {
            0.00f,  // Map3D
            0.00f,  // Billboards3D
            0.01f,  // MapBackground1
            0.01f,  // MapBackground2
            0.01f,  // MapBackground3
            0.01f,  // MapBackground4
            0.01f,  // MapBackground5
            0.01f,  // MapBackground6
            0.01f,  // MapBackground7
            0.01f,  // MapBackground8
            0.31f,  // Characters
            0.31f,  // MapForeground1
            0.31f,  // MapForeground2
            0.31f,  // MapForeground3
            0.31f,  // MapForeground4
            0.31f,  // MapForeground5
            0.31f,  // MapForeground6
            0.31f,  // MapForeground7
            0.31f,  // MapForeground8
            0.61f,  // UIBackground
            0.71f,  // BattleMonsterRowFarthest
            0.72f,  // BattleMonsterRowFar
            0.73f,  // BattleMonsterRowCenter
            0.74f,  // BattleMonsterRowNear
            0.75f,  // BattleMonsterRowNearest
            0.76f,  // UIForeground
            0.96f,  // Items
            0.96f,  // Popup
            0.96f,  // Text
            0.99f   // Cursor
        };

        public RenderLayer(State state, Layer layer, Texture texture, Texture palette)
        {
            if (layer == Layer.None)
                throw new AmbermoonException(ExceptionScope.Application, "Layer.None should never be used.");

            this.state = state;
            bool masked = false; // TODO: do we need this for some layer?
            bool supportAnimations = layer >= Global.First2DLayer && layer <= Global.Last2DLayer; // TODO
            bool layered = layer > Global.Last2DLayer; // map is not layered, drawing order depends on y-coordinate and not given layer

            RenderBuffer = new RenderBuffer(state, layer == Layer.Map3D || layer == Layer.Billboards3D,
                masked, supportAnimations, layered, false, layer == Layer.Billboards3D, layer == Layer.Text);

            // UI Background uses color-filled areas.
            // The popup layer is used to create effects like black fading map transitions.
            if (layer == Layer.UIBackground || layer == Layer.Popup)
                renderBufferColorRects = new RenderBuffer(state, false, false, false, true, true);

            Layer = layer;
            Texture = texture;
            this.palette = palette;
        }

        public void Render()
        {
            if (!Visible)
                return;

            if (renderBufferColorRects != null)
            {
                var colorShader = renderBufferColorRects.ColorShader;

                colorShader.UpdateMatrices(state);
                colorShader.SetZ(LayerBaseZ[(int)Layer]);

                renderBufferColorRects.Render();
            }

            if (Texture != null)
            {
                if (!(Texture is Texture texture))
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid texture for this renderer.");

                if (Layer == Layer.Map3D)
                {
                    Texture3DShader shader = RenderBuffer.Texture3DShader;

                    shader.UpdateMatrices(state);

                    shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                    state.Gl.ActiveTexture(GLEnum.Texture0);
                    texture.Bind();

                    if (palette != null)
                    {
                        shader.SetPalette(1);
                        state.Gl.ActiveTexture(GLEnum.Texture1);
                        palette.Bind();
                    }

                    shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                }
                else if (Layer == Layer.Billboards3D)
                {
                    Billboard3DShader shader = RenderBuffer.Billboard3DShader;

                    shader.UpdateMatrices(state);

                    shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                    state.Gl.ActiveTexture(GLEnum.Texture0);
                    texture.Bind();

                    if (palette != null)
                    {
                        shader.SetPalette(1);
                        state.Gl.ActiveTexture(GLEnum.Texture1);
                        palette.Bind();
                    }

                    shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                }
                else if (Layer == Layer.Text)
                {
                    TextShader shader = RenderBuffer.TextShader;

                    shader.UpdateMatrices(state);

                    shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                    state.Gl.ActiveTexture(GLEnum.Texture0);
                    texture.Bind();

                    if (palette != null)
                    {
                        shader.SetPalette(1);
                        state.Gl.ActiveTexture(GLEnum.Texture1);
                        palette.Bind();
                    }

                    shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                    shader.SetZ(LayerBaseZ[(int)Layer]);
                }
                else
                {
                    TextureShader shader = RenderBuffer.Masked ? RenderBuffer.MaskedTextureShader : RenderBuffer.TextureShader;

                    shader.UpdateMatrices(state);

                    shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                    state.Gl.ActiveTexture(GLEnum.Texture0);
                    texture.Bind();

                    if (palette != null)
                    {
                        shader.SetPalette(1);
                        state.Gl.ActiveTexture(GLEnum.Texture1);
                        palette.Bind();
                    }

                    shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                    shader.SetZ(LayerBaseZ[(int)Layer]);
                }
            }

            RenderBuffer.Render();
        }

        public int GetDrawIndex(ISprite sprite, Position maskSpriteTextureAtlasOffset = null, byte? textColorIndex = null)
        {
            return RenderBuffer.GetDrawIndex(sprite, PositionTransformation, SizeTransformation,
                maskSpriteTextureAtlasOffset, textColorIndex);
        }

        public int GetDrawIndex(ISurface3D surface)
        {
            return RenderBuffer.GetDrawIndex(surface);
        }

        public void FreeDrawIndex(int index)
        {
            RenderBuffer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, ISprite sprite)
        {
            RenderBuffer.UpdatePosition(index, sprite, sprite.BaseLineOffset, PositionTransformation, SizeTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            RenderBuffer.UpdateTextureAtlasOffset(index, sprite, maskSpriteTextureAtlasOffset);
        }

        public void UpdatePosition(int index, ISurface3D surface)
        {
            RenderBuffer.UpdatePosition(index, surface);
        }

        public void UpdateTextureAtlasOffset(int index, ISurface3D surface)
        {
            RenderBuffer.UpdateTextureAtlasOffset(index, surface);
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            RenderBuffer.UpdateDisplayLayer(index, displayLayer);
        }

        public void UpdatePaletteIndex(int index, byte paletteIndex)
        {
            RenderBuffer.UpdatePaletteIndex(index, paletteIndex);
        }

        public void UpdateTextColorIndex(int index, byte textColorIndex)
        {
            RenderBuffer.UpdateTextColorIndex(index, textColorIndex);
        }

        public int GetColoredRectDrawIndex(ColoredRect coloredRect)
        {
            return renderBufferColorRects.GetDrawIndex(coloredRect, PositionTransformation, SizeTransformation);
        }

        public void FreeColoredRectDrawIndex(int index)
        {
            renderBufferColorRects.FreeDrawIndex(index);
        }

        public void UpdateColoredRectPosition(int index, ColoredRect coloredRect)
        {
            renderBufferColorRects.UpdatePosition(index, coloredRect, 0, PositionTransformation, SizeTransformation);
        }

        public void UpdateColoredRectColor(int index, Render.Color color)
        {
            renderBufferColorRects.UpdateColor(index, color);
        }

        public void UpdateColoredRectDisplayLayer(int index, byte displayLayer)
        {
            renderBufferColorRects.UpdateDisplayLayer(index, displayLayer);
        }

        public void TestNode(IRenderNode node)
        {
            if (!(node is RenderNode))
                throw new AmbermoonException(ExceptionScope.Render, "The given render node is not valid for this renderer.");

            if (node is ColoredRect && renderBufferColorRects == null)
                throw new AmbermoonException(ExceptionScope.Render, "This layer does not support colored rects.");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    RenderBuffer?.Dispose();
                    renderBufferColorRects?.Dispose();
                    if (Texture is Texture texture)
                        texture?.Dispose();
                    Visible = false;

                    disposed = true;
                }
            }
        }
    }

    public class RenderLayerFactory : IRenderLayerFactory
    {
        public State State { get; }

        public RenderLayerFactory(State state)
        {
            State = state;
        }

        public IRenderLayer Create(Layer layer, Render.Texture texture, Render.Texture palette)
        {
            if (texture != null && !(texture is Texture))
                throw new AmbermoonException(ExceptionScope.Render, "The given texture is not valid for this renderer.");
            if (palette != null && !(palette is Texture))
                throw new AmbermoonException(ExceptionScope.Render, "The given palette is not valid for this renderer.");

            return layer switch
            {
                Layer.None => throw new AmbermoonException(ExceptionScope.Render, $"Cannot create render layer for layer {Enum.GetName(layer)}"),
                _ => new RenderLayer(State, layer, texture as Texture, palette as Texture),
            };
        }
    }
}
