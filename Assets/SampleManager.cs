using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleManager : MonoBehaviour {
    public Color Scan_Color;
    public float Scan_Width = 1f;
    public float Scan_Duration = 1.5f;
    public float Scan_Range = 20f;
    public Texture Scan_Texure;
    public float Scan_Scale = 1f;

    public Color Area_FillColor = Color.grey;
    public Color Area_EdgeColor = Color.blue;
    public float Area_Radius = 10f;
    public float Area_Width = 1f;
    public float Area_Duration = 2f;
    public Texture Area_Texture = null;
    public float Area_TexureScale = 1;
    public Vector2 Area_TextureFlow = Vector2.one;

    RaycastHit hit;

    CameraEffectManager m_Effect;
    Coroutine cor_scan;
    Coroutine cor_area;
    bool m_AreaScaning = false;
    void Awake()
    {
        Camera.main.depthTextureMode = DepthTextureMode.None;
        m_Effect = Camera.main.GetComponent<CameraEffectManager>();
        m_AreaScaning = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << 0))
        {
            StartDepthScanCircle(hit.point, Scan_Color, Scan_Width, Scan_Range, Scan_Duration).SetTexture(Scan_Texure, Scan_Scale);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 1 << 0))
        {
            m_AreaScaning = !m_AreaScaning;
            SetDepthAreaCircle(m_AreaScaning, hit.point, Area_Radius, Area_Width, Area_Duration).SetColor(Area_FillColor,Area_EdgeColor).SetTexture(Area_Texture, Area_TexureScale, Area_TextureFlow);
        }
    }

    public PE_DepthCircleScan StartDepthScanCircle(Vector3 origin, Color scanColor, float width = 1f, float radius = 20, float duration = 1.5f)
    {
        if(cor_scan!=null)
        {
            this.StopCoroutine(cor_scan);
            cor_scan = null;
        }

        PE_DepthCircleScan scan = m_Effect.GetOrAddCameraEffect<PE_DepthCircleScan>().SetEffect(origin, scanColor);
        cor_scan=this.StartCoroutine(ChangeValueTo((float value) => {
            scan.SetElapse(radius * value, width);
        }, 0, 1, duration, () => {
            m_Effect.RemoveCameraEffect<PE_DepthCircleScan>();
        }));
        return scan;
    }

    public PE_DepthCircleArea SetDepthAreaCircle(bool begin, Vector3 origin, float radius = 10f, float edgeWidth = .5f, float duration = 1.5f)
    {
        if (cor_area != null)
        {
            this.StopCoroutine(cor_area);
            cor_area = null;
        }

        PE_DepthCircleArea area = m_Effect.GetOrAddCameraEffect<PE_DepthCircleArea>().SetOrigin(origin);
        cor_area=this.StartCoroutine(ChangeValueTo((float value) => { area.SetRadius(radius * value, edgeWidth); },
            begin ? 0 : 1, begin ? 1 : 0, duration,
            () => {
                if (!begin)
                    m_Effect.RemoveCameraEffect<PE_DepthCircleArea>();
            }));
        return area;
    }

    public static IEnumerator ChangeValueTo(System.Action<float> OnValueChanged, float startValue, float endValue, float duration, Action OnFinished = null, bool scaled = true)
    {
        float timeValue = duration;
        for (; ; )
        {
            timeValue -= scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            OnValueChanged(Mathf.Lerp(endValue, startValue, timeValue / duration));
            if (timeValue < 0)
            {
                OnValueChanged(endValue);
                OnFinished?.Invoke();
                yield break;
            }
            yield return null;
        }
    }



}
