using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class CameraEffectManager :MonoBehaviour
{
    List<CameraEffectBase> m_CameraEffects=new List<CameraEffectBase>();
    public Camera m_Camera { get; protected set; }
    public bool m_MainTextureCamera { get; private set; }
    public bool m_DepthToWorldRebuild { get; private set; } = false;
    public bool m_DoGraphicBlitz { get; private set; } = false;
    RenderTexture m_BlitzTempTexture1, m_BlitzTempTexture2;

    protected void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_Camera.depthTextureMode = DepthTextureMode.None;
        m_DepthToWorldRebuild = false;
        m_MainTextureCamera = false;
        m_DoGraphicBlitz = false;
        m_BlitzTempTexture1 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        m_BlitzTempTexture2 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
    }
    protected void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_DepthToWorldRebuild)
            CalculateDepthToWorldRays();

        if(!m_DoGraphicBlitz)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, m_BlitzTempTexture1);
        for (int i = 0; i < m_CameraEffects.Count; i++)
        {
            if (! m_CameraEffects[i].m_Enabled)
                continue;

            m_CameraEffects[i].OnRenderImage(m_BlitzTempTexture1,m_BlitzTempTexture2);
            Graphics.Blit(m_BlitzTempTexture2, m_BlitzTempTexture1);
        }
        Graphics.Blit(m_BlitzTempTexture1,destination);
    }
    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(m_BlitzTempTexture2);
        RenderTexture.ReleaseTemporary(m_BlitzTempTexture1);
        RemoveAllPostEffect();
    }

    #region Calculations
    public float Get01Depth(Vector3 target) => m_Camera.WorldToViewportPoint(target).z / (m_Camera.farClipPlane - m_Camera.nearClipPlane);
    public float Get01DepthLength(float length) => length / (m_Camera.farClipPlane - m_Camera.nearClipPlane);
    static readonly int m_GlobalCameraDepthTextureMode = Shader.PropertyToID("_CameraDepthTextureMode");
    static readonly int ID_FrustumCornersRayBL = Shader.PropertyToID("_FrustumCornersRayBL");
    static readonly int ID_FrustumCornersRayBR = Shader.PropertyToID("_FrustumCornersRayBR");
    static readonly int ID_FrustumCornersRayTL = Shader.PropertyToID("_FrustumCornersRayTL");
    static readonly int ID_FrustumCornersRayTR = Shader.PropertyToID("_FrustumCornersRayTR");
    protected void CalculateDepthToWorldRays()
    {
        float fov = m_Camera.fieldOfView;
        float near = m_Camera.nearClipPlane;
        float far = m_Camera.farClipPlane;
        float aspect = m_Camera.aspect;

        Transform cameraTrans = m_Camera.transform;
        float halfHeight = near * Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
        Vector3 toRight = cameraTrans.right * halfHeight * aspect;
        Vector3 toTop = cameraTrans.up * halfHeight;

        Vector3 topLeft = cameraTrans.forward * near + toTop - toRight;
        float scale = topLeft.magnitude / near;
        topLeft.Normalize();
        topLeft *= scale;

        Vector3 topRight = cameraTrans.forward * near + toTop + toRight;
        topRight.Normalize();
        topRight *= scale;

        Vector3 bottomLeft = cameraTrans.forward * near - toTop - toRight;
        bottomLeft.Normalize();
        bottomLeft *= scale;
        Vector3 bottomRight = cameraTrans.forward * near - toTop + toRight;
        bottomRight.Normalize();
        bottomRight *= scale;
        
        Shader.SetGlobalVector(ID_FrustumCornersRayBL, bottomLeft);
        Shader.SetGlobalVector(ID_FrustumCornersRayBR, bottomRight);
        Shader.SetGlobalVector(ID_FrustumCornersRayTL, topLeft);
        Shader.SetGlobalVector(ID_FrustumCornersRayTR, topRight);
    }
    #endregion

    #region Interact
    public T GetOrAddCameraEffect<T>() where T : CameraEffectBase, new()
    {
        T existingEffect = GetCameraEffect<T>();
        if (existingEffect != null)
            return existingEffect;

        T effectBase = new T();
        if (effectBase.m_Supported)
        {
            effectBase.InitEffect(this);
            m_CameraEffects.Add(effectBase);
            ResetCameraEffectParams();
            return effectBase;
        }
        return null;
    }

    public T GetCameraEffect<T>() where T : CameraEffectBase => m_CameraEffects.Find(p => p.GetType() == typeof(T)) as T;
    public void RemoveCameraEffect<T>() where T : CameraEffectBase, new()
    {
        T effect = GetCameraEffect<T>();
        if (effect == null)
            return;

        m_CameraEffects.Remove(effect);
        ResetCameraEffectParams();
    }
    public void RemoveAllPostEffect()
    {
        foreach(CameraEffectBase effect in m_CameraEffects)
        {
            effect.OnDestroy();
        }
        m_CameraEffects.Clear();
        ResetCameraEffectParams();
    }

    protected void ResetCameraEffectParams()
    {
        Shader.SetGlobalInt(m_GlobalCameraDepthTextureMode, m_MainTextureCamera ? 1 : 0);
        m_DoGraphicBlitz = false;
        m_DepthToWorldRebuild = false;
        foreach(CameraEffectBase effectBase in m_CameraEffects)
        {
            if (!effectBase.m_Enabled)
                return;

            m_DoGraphicBlitz |= effectBase.m_DoGraphicBlitz;
            m_DepthToWorldRebuild |= effectBase.m_DepthFrustumCornors;
        }
    }
    #endregion
}

#region CameraEffectBase
public class CameraEffectBase
{
    public virtual bool m_DepthFrustumCornors => false;
    public virtual bool m_DoGraphicBlitz => false;
    protected CameraEffectManager m_Manager { get; private set; }
    public bool m_Supported { get; private set; }
    public bool m_Enabled { get; protected set; }
    public CameraEffectBase()
    {
        m_Supported = Init();
    }
    protected virtual bool Init()
    {
        return true;
    }
    public virtual void InitEffect(CameraEffectManager _manager)
    {
        m_Manager = _manager;
        m_Enabled = true;
    }
    public virtual void SetEnable(bool enable) => m_Enabled = enable;
    public virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
    }

    public virtual void OnDestroy()
    {
    }
}

public class PostEffectBase : CameraEffectBase
{
    const string S_ParentPath = "Hidden/PostEffect/";
    public Material m_Material { get; private set; }
    public override bool m_DoGraphicBlitz => true;
    protected override bool Init()
    {
        m_Material = CreateMaterial(this.GetType());
        return m_Material != null;
    }

    public static Material CreateMaterial(Type type)
    {
        try
        {
            Shader shader = Shader.Find(S_ParentPath + type.ToString());
            if (shader == null)
                throw new Exception("Shader:" + S_ParentPath + type.ToString() + " Not Found");
            if (!shader.isSupported)
                throw new Exception("Shader:" + S_ParentPath + type.ToString() + " Is Not Supported");

            return new Material(shader) { hideFlags = HideFlags.DontSave };
        }
        catch (Exception e)
        {
            Debug.LogError("Post Effect Error:" + e.Message);
            return null;
        }
    }

    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, m_Material);
    }
    public override void OnDestroy()
    {
        GameObject.Destroy(m_Material);
    }
}
#endregion