﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace vrp
{
    public class VRenderTexture2D
    {
        string m_name;
        RenderTextureFormat m_format;
        bool m_liner;
        int m_msaa;
        public bool TestNeedModify(int w, int h, int d)
        {
            if (data != null && data.IsCreated())
            {
                if (data.width != w || data.height != h || data.depth != d)
                {
                    data.Release();
                    New(w, h, d);
                    return true;
                }
                return false;
            }
            else
            {
                New(w, h, d);
                return true;
            }
        }

        void New(int w, int h, int d)
        {
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(w, h, m_format, d);
            renderTextureDescriptor.msaaSamples = m_msaa;
            renderTextureDescriptor.sRGB = !m_liner;
            data = new RenderTexture(renderTextureDescriptor);
            data.anisoLevel = 2;
            data.name = m_name;
            
            data.Create();
        }

        public VRenderTexture2D(string name, RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32, bool liner = false, bool msaa = false)
        {
            m_name = name;
            m_format = textureFormat;
            m_liner = liner;
            m_msaa = msaa ? 4 : 1;
        }

        public void Dispose()
        {
            if (data != null && data.IsCreated())
            {
                data.Release();
            }
        }

        public RenderTexture data { get; private set; }
    }

    public class VRenderTextureArray
    {
        string m_name;
        RenderTextureFormat m_format;
        bool m_liner;
        int m_msaa;
        bool m_shadowmap;
        public bool TestNeedModify(int w, int h, int n)
        {
            if (data != null && data.IsCreated())
            {
                if (data.width != w || data.height != h || data.volumeDepth < n || data.volumeDepth >= n * 2)
                {
                    data.Release();
                    New(w, h, n);
                    return true;
                }
                return false;
            }
            else
            {
                New(w, h, n);
                return true;
            }
        }

        void New(int w, int h, int n)
        {
            if (n == 0)
            {
                data = null;
                return;
            }
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(w, h, m_format);
            renderTextureDescriptor.msaaSamples = m_msaa;
            renderTextureDescriptor.sRGB = !m_liner;
            renderTextureDescriptor.dimension = TextureDimension.Tex2DArray;
            renderTextureDescriptor.volumeDepth = n;
            renderTextureDescriptor.depthBufferBits = 24;
            renderTextureDescriptor.shadowSamplingMode = m_shadowmap ? ShadowSamplingMode.CompareDepths : ShadowSamplingMode.None;
            data = new RenderTexture(renderTextureDescriptor);
            data.anisoLevel = 2;
            data.name = m_name;
            data.Create();
        }

        public VRenderTextureArray(string name, RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32, bool liner = false, bool msaa = false, bool shadowmap = false)
        {
            m_name = name;
            m_format = textureFormat;
            m_liner = liner;
            m_msaa = msaa ? 4 : 1;
            m_shadowmap = shadowmap;
        }

        public bool IsValid()
        {
            return data != null && data.IsCreated();
        }

        public void Dispose()
        {
            if (data != null && data.IsCreated())
            {
                data.Release();
            }
        }

        public RenderTexture data { get; private set; }
    }

    public class VComputeBuffer
    {
        int m_stride;
        public VComputeBuffer(int stride)
        {
            m_stride = stride;
        }
        public bool TestNeedModify(int len)
        {
            if (data != null)
            {
                if (data.count < len || data.count >= len * 2)
                {
                    data.Release();
                    data = null;
                    if (len != 0)
                        data = new ComputeBuffer(len, m_stride);
                    return true;
                }
                return false;
            }
            else
            {
                if (len != 0)
                {
                    data = new ComputeBuffer(len, m_stride);
                    return true;
                }
                return false;
            }
        }

        public bool IsValid()
        {
            return data != null && data.IsValid();
        }

        public void Dispose()
        {
            if (data != null)
            {
                data.Release();
                data = null;
            }
        }
        public ComputeBuffer data { get; private set; }
    }

    public class VMaterials
    {
        public Material blitSmall { get; private set; }
        public Material blitArray { get; private set; }


        public VMaterials()
        {
            blitSmall = new Material(Shader.Find("Hidden/VRP/BlitSmall"));
            blitArray = new Material(Shader.Find("Hidden/VRP/BlitArray"));
        }



    }

}