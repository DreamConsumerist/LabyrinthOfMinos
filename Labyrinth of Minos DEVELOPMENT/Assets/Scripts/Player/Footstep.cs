using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Footstep : MonoBehaviour
{
    
    static readonly int ColorProp = Shader.PropertyToID("_BaseColor"); // URP Lit
    static readonly int LegacyColorProp = Shader.PropertyToID("_Color"); // Standard/Legacy

    Renderer _renderer;
    MaterialPropertyBlock _mpb;
    float _currentAlpha = 1f;
    bool _fading = false;

    // Spawner injects this to return to pool.
    Action<Footstep> _onFadeComplete;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    public void Initialize(Action<Footstep> onFadeComplete)
    {
        _onFadeComplete = onFadeComplete;
    }

    public void ResetVisual(float startingAlpha = 1f)
    {
        _fading = false;
        _currentAlpha = startingAlpha;
        ApplyAlpha(_currentAlpha);
        gameObject.SetActive(true);
    }

    public void SetTransform(Vector3 pos, Quaternion rot, Vector3 scale, Transform parent)
    {
        // Set world transform first
        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = scale;

        
        if (parent != null)
            transform.SetParent(parent, worldPositionStays: true);
        else
            transform.SetParent(null, true);
    }

    public void FadeOut(float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (_fading) return;
        StartCoroutine(FadeRoutine(duration));
    }

    IEnumerator FadeRoutine(float duration)
    {
        _fading = true;
        float t = 0f;
        float start = _currentAlpha;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            _currentAlpha = Mathf.Lerp(start, 0f, t / duration);
            ApplyAlpha(_currentAlpha);
            yield return null;
        }

        _currentAlpha = 0f;
        ApplyAlpha(_currentAlpha);
        gameObject.SetActive(false);
        _fading = false;
        _onFadeComplete?.Invoke(this);
    }

    void ApplyAlpha(float a)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_mpb);

        // Try URP _BaseColor first; if unused, fall back to _Color
        if (_renderer.sharedMaterial != null &&
            _renderer.sharedMaterial.HasProperty(ColorProp))
        {
            var baseCol = _renderer.sharedMaterial.GetColor(ColorProp);
            baseCol.a = a;
            _mpb.SetColor(ColorProp, baseCol);
        }
        else
        if (_renderer.sharedMaterial != null &&
            _renderer.sharedMaterial.HasProperty(LegacyColorProp))
        {
            var baseCol = _renderer.sharedMaterial.GetColor(LegacyColorProp);
            baseCol.a = a;
            _mpb.SetColor(LegacyColorProp, baseCol);
        }

        _renderer.SetPropertyBlock(_mpb);
    }
}
