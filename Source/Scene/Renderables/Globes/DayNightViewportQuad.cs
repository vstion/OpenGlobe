﻿#region License
//
// (C) Copyright 2010 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

using System;
using System.Drawing;
using MiniGlobe.Core;
using MiniGlobe.Core.Geometry;
using MiniGlobe.Renderer;

namespace MiniGlobe.Scene
{
    public sealed class DayNightViewportQuad
    {
        public DayNightViewportQuad(Context context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ///////////////////////////////////////////////////////////////////

            _context = context;
            _renderState = new RenderState();
            _renderState.FacetCulling.Enabled = false;
            _renderState.DepthTest.Enabled = false;

            string vs =
                @"#version 150

                  in vec4 position;
                  in vec2 textureCoordinates;

                  out vec2 fsTextureCoordinates;

                  uniform mat4 mg_viewportOrthographicProjectionMatrix;

                  void main()                     
                  {
                      gl_Position = mg_viewportOrthographicProjectionMatrix * position;
                      fsTextureCoordinates = textureCoordinates;
                  }";
            string fs =
                @"#version 150
                 
                  in vec2 fsTextureCoordinates;

                  out vec4 fragmentColor;

                  uniform sampler2D mg_texture0;    // Day
                  uniform sampler2D mg_texture1;    // Night
                  uniform sampler2D mg_texture2;    // Blend

                  void main()
                  {
                      vec4 dayColor = texture(mg_texture0, fsTextureCoordinates);
                      vec4 nightColor = texture(mg_texture1, fsTextureCoordinates);
                      float blend = texture(mg_texture2, fsTextureCoordinates).r;

                      fragmentColor = mix(nightColor, dayColor, blend);
                  }";
            _sp = Device.CreateShaderProgram(vs, fs);

            ///////////////////////////////////////////////////////////////////

            _positionBuffer = Device.CreateVertexBuffer(BufferHint.StaticDraw, 4 * SizeInBytes<Vector2S>.Value);
            _textureCoordinatesBuffer = Device.CreateVertexBuffer(BufferHint.StaticDraw, 4 * SizeInBytes<Vector2H>.Value);

            AttachedVertexBuffer attachedPositionBuffer = new AttachedVertexBuffer(
                _positionBuffer, VertexAttributeComponentType.Float, 2);
            AttachedVertexBuffer attachedTextureCoordinates = new AttachedVertexBuffer(
                _textureCoordinatesBuffer, VertexAttributeComponentType.HalfFloat, 2);

            _va = _context.CreateVertexArray();
            _va.VertexBuffers[_sp.VertexAttributes["position"].Location] = attachedPositionBuffer;
            _va.VertexBuffers[_sp.VertexAttributes["textureCoordinates"].Location] = attachedTextureCoordinates;
        }

        private void Update()
        {
            if (_viewport != _context.Viewport)
            {
                //
                // Bottom and top swapped:  MS -> OpenGL
                //
                float left = _context.Viewport.Left;
                float bottom = _context.Viewport.Top;
                float right = _context.Viewport.Right;
                float top = _context.Viewport.Bottom;

                Vector2S[] positions = new Vector2S[] 
                { 
                    new Vector2S(left, bottom), 
                    new Vector2S(right, bottom), 
                    new Vector2S(left, top), 
                    new Vector2S(right, top)
                };
                _positionBuffer.CopyFromSystemMemory(positions);

                Vector2H[] textureCoordinates = new Vector2H[] 
                { 
                    new Vector2H(0, 0), 
                    new Vector2H(1, 0), 
                    new Vector2H(0, 1), 
                    new Vector2H(1, 1)
                };
                _textureCoordinatesBuffer.CopyFromSystemMemory(textureCoordinates);

                _viewport = _context.Viewport;
            }
        }

        // TODO: Everywhere
        private static void ThrowInvalidOperationIfNull(Texture2D texture, string memberName)
        {
            if (texture == null)
            {
                throw new InvalidOperationException(memberName);
            }
        }

        public void Render(SceneState sceneState)
        {
            ThrowInvalidOperationIfNull(DayTexture, "DayTexture");
            ThrowInvalidOperationIfNull(NightTexture, "NightTexture");
            ThrowInvalidOperationIfNull(BlendTexture, "BlendTexture");

            Update();

            _context.TextureUnits[0].Texture2D = DayTexture;
            _context.TextureUnits[1].Texture2D = NightTexture;
            _context.TextureUnits[2].Texture2D = BlendTexture;
            _context.Bind(_renderState);
            _context.Bind(_sp);
            _context.Bind(_va);
            _context.Draw(PrimitiveType.TriangleStrip, sceneState);
        }

        public Context Context
        {
            get { return _context; }
        }

        public Texture2D DayTexture { get; set; }
        public Texture2D NightTexture { get; set; }
        public Texture2D BlendTexture { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            _sp.Dispose();
            _positionBuffer.Dispose();
            _textureCoordinatesBuffer.Dispose();
            _va.Dispose();
        }

        #endregion

        private readonly Context _context;
        private readonly RenderState _renderState;
        private readonly ShaderProgram _sp;

        private Rectangle _viewport;
        private VertexBuffer _positionBuffer;
        private VertexBuffer _textureCoordinatesBuffer;
        private VertexArray _va;
    }
}