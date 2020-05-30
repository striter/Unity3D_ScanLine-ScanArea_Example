
using UnityEngine;

public class PE_DepthCircleScan : PostEffectBase
{
    public override bool m_DepthFrustumCornors => true;
    readonly int ID_Origin = Shader.PropertyToID("_Origin");
    readonly int ID_Color = Shader.PropertyToID("_Color");
    readonly int ID_Texture = Shader.PropertyToID("_Texture");
    readonly int ID_TexScale = Shader.PropertyToID("_TextureScale");
    readonly int ID_MinSqrDistance = Shader.PropertyToID("_MinSqrDistance");
    readonly int ID_MaxSqrDistance = Shader.PropertyToID("_MaxSqrDistance");

    public void SetElapse(float elapse, float width)
    {
        float minDistance = elapse - width;
        float maxDistance = elapse;
        m_Material.SetFloat(ID_MinSqrDistance, minDistance * minDistance);
        m_Material.SetFloat(ID_MaxSqrDistance, maxDistance * maxDistance);
    }

    public PE_DepthCircleScan SetEffect(Vector3 origin, Color scanColor)
    {
        m_Material.SetVector(ID_Origin, origin);
        m_Material.SetColor(ID_Color, scanColor);
        return this;
    }

    public PE_DepthCircleScan SetTexture(Texture scanTex = null, float _scanTexScale = 15f)
    {
        m_Material.SetTexture(ID_Texture, scanTex);
        m_Material.SetFloat(ID_TexScale, _scanTexScale);
        return this;
    }
}

public class PE_DepthCircleArea : PostEffectBase
{
    public override bool m_DepthFrustumCornors => true;

    readonly int ID_Origin = Shader.PropertyToID("_Origin");
    readonly int ID_FillColor = Shader.PropertyToID("_FillColor");
    readonly int ID_FillTexture = Shader.PropertyToID("_FillTexture");
    readonly int ID_FillTextureScale = Shader.PropertyToID("_TextureScale");
    readonly int ID_FillTextureFlow = Shader.PropertyToID("_TextureFlow");
    readonly int ID_EdgeColor = Shader.PropertyToID("_EdgeColor");
    readonly int ID_SqrEdgeMin = Shader.PropertyToID("_SqrEdgeMin");
    readonly int ID_SqrEdgeMax = Shader.PropertyToID("_SqrEdgeMax");

    public void SetRadius(float radius, float edge)
    {
        float edgeMax = radius;
        float edgeMin = radius - edge;
        m_Material.SetFloat(ID_SqrEdgeMax, edgeMax * edgeMax);
        m_Material.SetFloat(ID_SqrEdgeMin, edgeMin * edgeMin);
    }

    public PE_DepthCircleArea SetOrigin(Vector3 origin)
    {
        m_Material.SetVector(ID_Origin, origin);
        return this;
    }

    public PE_DepthCircleArea SetColor(Color fillColor, Color edgeColor)
    {
        m_Material.SetColor(ID_FillColor, fillColor);
        m_Material.SetColor(ID_EdgeColor, edgeColor);
        return this;
    }

    public PE_DepthCircleArea SetTexture(Texture fillTex, float texScale, Vector2 texFlow)
    {
        m_Material.SetTexture(ID_FillTexture, fillTex);
        m_Material.SetFloat(ID_FillTextureScale, texScale);
        m_Material.SetVector(ID_FillTextureFlow, texFlow);
        return this;
    }
}