﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace vrp
{
    public class GIResources
    {
        VRPAsset m_asset;

        public Vector3 last_position;

        ComputeShader m_cs_UpdateSHVolume;
        int kernel_Update;
        int kernel_Clear;
        int kernel_Fill;
        
        private VRenderTextureCube[] old_volume;
        private VRenderTextureCube[] new_volume;

        int cached_volume_size;
        float cached_volume_length;

        Scene_SH cached_scene_sh;

        bool enable_bake_gi;
        bool turn;

        private class ShaderPropertyID
        {
            public int GI_Volume_Params;
            public int param0, param1;
            public int[] old_volume;
            public int[] new_volume;

            public int[] giVolume;
            public ShaderPropertyID()
            {
                param0 = Shader.PropertyToID("_Param0");//xyz:(moved_distance/volume_length)   w:texture size
                param1 = Shader.PropertyToID("_Param1");//---
                old_volume = new int[7]; new_volume = new int[7]; giVolume = new int[7];
                for (int i = 0; i < 7; i++)
                {
                    GI_Volume_Params = Shader.PropertyToID("_GI_Volume_Params");
                    old_volume[i] = Shader.PropertyToID("_old" + i.ToString());
                    new_volume[i] = Shader.PropertyToID("_new" + i.ToString());
                    giVolume[i] = Shader.PropertyToID("_GIVolume_" + i.ToString());
                }

            }
        };

        private static readonly ShaderPropertyID shaderPropertyID = new ShaderPropertyID();

        public void Update(ref ScriptableRenderContext renderContext, Camera camera, ref CommandBuffer setup_properties)
        {
            //if (camera.cameraType != CameraType.Game) return;

            TestEnable(setup_properties);

            int size_of_volume = m_asset.realtimeGIDensity;
            float length_of_volume = m_asset.distributionDistanceFromCamera;
            Vector3 camera_pos = camera.transform.position;

            var cb = CommandBufferPool.Get("GI Volume update");

            for (int i = 0; i < 7; i++)
            {
                old_volume[i].TestNeedModify(size_of_volume, size_of_volume, size_of_volume);
                new_volume[i].TestNeedModify(size_of_volume, size_of_volume, size_of_volume);
            }

            if (enable_bake_gi) PrepareBakedGI(cb, size_of_volume, length_of_volume, camera_pos);



            

            //Vector4 parm0 = (camera.transform.position - last_position) / length_of_volume; parm0.w = size_of_volume;

            ////config changed
            //if (cached_volume_length != length_of_volume || cached_volume_size != size_of_volume)
            //{
            //    if (cached_volume_size != size_of_volume)
            //    {
            //        cached_volume_size = size_of_volume;
            //        old_volume0.TestNeedModify(size_of_volume, size_of_volume, size_of_volume);
            //        old_volume1.TestNeedModify(size_of_volume, size_of_volume, size_of_volume);
            //        new_volume0.TestNeedModify(size_of_volume, size_of_volume, size_of_volume);
            //        new_volume1.TestNeedModify(size_of_volume, size_of_volume, size_of_volume);

            //        Debug.Log(camera.name + " clr");
            //        ClearVolume(cb, turn, size_of_volume);
            //    }
            //    cached_volume_length = length_of_volume;
            //}
            ////move too far, just clear volume
            //else if (Mathf.Abs(parm0.x) > 1 || Mathf.Abs(parm0.y) > 1 || Mathf.Abs(parm0.z) > 0)
            //{
            //    Debug.Log(camera.name + " clr");
            //    ClearVolume(cb, turn, size_of_volume);
            //}
            ////else need blit offset
            //else
            //{
            //    Debug.Log(camera.name + " off");
            //    OffsetVolume(cb, turn, size_of_volume, parm0);
            //}


            for (int i = 0; i < 7; i++)
                setup_properties.SetGlobalTexture(shaderPropertyID.giVolume[i], /*turn ?*/ new_volume[i].data /*: old_volume0.data*/);

            setup_properties.SetGlobalVector(shaderPropertyID.GI_Volume_Params, new Vector4(length_of_volume, length_of_volume));

            renderContext.ExecuteCommandBuffer(cb);
            CommandBufferPool.Release(cb);
            turn = !turn;
            last_position = camera.transform.position;
        }

        void ClearVolume(CommandBuffer cb, bool turn, int size)
        {
            if (turn)
            {
                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Clear, shaderPropertyID.new_volume[i], new_volume[i].data);

                cb.DispatchCompute(m_cs_UpdateSHVolume, 0, size / 8, size / 8, size / 8);
            }
            else
            {
                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Clear, shaderPropertyID.new_volume[i], old_volume[i].data);

                cb.DispatchCompute(m_cs_UpdateSHVolume, kernel_Clear, size / 8, size / 8, size / 8);
            }
        }

        void OffsetVolume(CommandBuffer cb, bool turn, int size, Vector4 parm0)
        {
            if (turn)
            {
                cb.SetComputeVectorParam(m_cs_UpdateSHVolume, shaderPropertyID.param0, parm0);

                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Update, shaderPropertyID.old_volume[i], old_volume[i].data);

                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Update, shaderPropertyID.new_volume[i], new_volume[i].data);

                cb.DispatchCompute(m_cs_UpdateSHVolume, kernel_Update, size / 8, size / 8, size / 8);
            }
            else
            {
                cb.SetComputeVectorParam(m_cs_UpdateSHVolume, shaderPropertyID.param0, parm0);

                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Update, shaderPropertyID.old_volume[i], new_volume[i].data);

                for (int i = 0; i < 7; i++)
                    cb.SetComputeTextureParam(m_cs_UpdateSHVolume, kernel_Update, shaderPropertyID.new_volume[i], old_volume[i].data);

                cb.DispatchCompute(m_cs_UpdateSHVolume, kernel_Update, size / 8, size / 8, size / 8);
            }
        }


        void PrepareBakedGI(CommandBuffer cb, int size_of_volume, float length_of_volume, Vector3 camera_pos)
        {
            for (int i = 0; i < 7; i++)
                m_cs_UpdateSHVolume.SetTexture(kernel_Fill, shaderPropertyID.new_volume[i], new_volume[i].data);

            cb.SetComputeBufferParam(m_cs_UpdateSHVolume, kernel_Fill, "posBuffer", cached_scene_sh.posBuffer);
            cb.SetComputeBufferParam(m_cs_UpdateSHVolume, kernel_Fill, "shBuffer", cached_scene_sh.shBuffer);
            cb.SetComputeVectorParam(m_cs_UpdateSHVolume, shaderPropertyID.param0, new Vector4(cached_scene_sh.posBuffer.count, size_of_volume, length_of_volume, 0));
            cb.SetComputeVectorParam(m_cs_UpdateSHVolume, shaderPropertyID.param1, camera_pos);
            cb.DispatchCompute(m_cs_UpdateSHVolume, kernel_Fill, size_of_volume / 8, size_of_volume / 8, size_of_volume / 8);
        }






        void TestEnable(CommandBuffer setup_properties)
        {
            if (m_asset.enableRealtimeGI == false)
                setup_properties.DisableShaderKeyword("_Enable_R_GI");
            else
                setup_properties.EnableShaderKeyword("_Enable_R_GI");


            if (m_asset.enableBakedGI == false)
            {
                enable_bake_gi = false;
                setup_properties.DisableShaderKeyword("_Enable_B_GI");
            }
            else
            {
                var go = GameObject.FindWithTag("GI");
                if (go == null)
                {
                    enable_bake_gi = false;
                    setup_properties.DisableShaderKeyword("_Enable_B_GI");
                    cached_scene_sh = null;
                }
                else cached_scene_sh = go.GetComponent<Scene_SH>();
                if (cached_scene_sh == null)
                {
                    enable_bake_gi = false;
                    setup_properties.DisableShaderKeyword("_Enable_B_GI");
                }
                else
                {
                    enable_bake_gi = true;
                    setup_properties.EnableShaderKeyword("_Enable_B_GI");
                    if (cached_scene_sh.posBuffer == null) cached_scene_sh.PrepareBuffer();
                }
            }
        }




        public GIResources(VRPAsset asset)
        {
            m_cs_UpdateSHVolume = asset.resources.cs_UpdateSHVolume;
            m_asset = asset;

            kernel_Update = m_cs_UpdateSHVolume.FindKernel("Update");
            kernel_Clear = m_cs_UpdateSHVolume.FindKernel("Clear");
            kernel_Fill = m_cs_UpdateSHVolume.FindKernel("Fill");
            
            old_volume = new VRenderTextureCube[7];
            new_volume = new VRenderTextureCube[7];
            for (int i = 0; i < 7; i++)
            {
                old_volume[i] = new VRenderTextureCube("old_gi_volume_" + i.ToString(), RenderTextureFormat.ARGBFloat);
                new_volume[i] = new VRenderTextureCube("new_gi_volume_" + i.ToString(), RenderTextureFormat.ARGBFloat);
            }

            cached_volume_size = -1;
            cached_volume_length = -1;

            turn = true;
        }
        public void Dispose()
        {
            for (int i = 0; i < 7; i++)
            {
                old_volume[i].Dispose();
                new_volume[i].Dispose();
            }
        }
    }
}
