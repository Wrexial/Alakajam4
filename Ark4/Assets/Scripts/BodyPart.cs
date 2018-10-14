using MEC;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BodyPart : MonoBehaviour
{
    public SlotType Type;
    public Transform IngameElement;
    public Transform UIElement;

    private Material[] _materials;

    private CoroutineHandle? _showingCoroutine;
    private readonly int DISSOLVE_PROPERTY = Shader.PropertyToID("_SliceAmount");
    private readonly int DISSOLVE_OFFSET_PROPERTY = Shader.PropertyToID("_SliceGuide");

    private const float ANIMATION_TIME = 0.75f;
    
    public void SetDefaultPosition()
    {
        IngameElement.gameObject.SetActive(true);
        //transform.localPosition = _position;
        //var rot = transform.rotation;
        //rot.eulerAngles = _rotation;
        //transform.localRotation = rot;
        //transform.localScale = _scale;
    }

    public void Disable(bool ingameElement, bool anim = true)
    {
        if (ingameElement)
        {
            if (anim)
            {
                TimingHandlers.CleanlyKillCoroutine(ref _showingCoroutine);
                _showingCoroutine = Timing.RunCoroutine(Show(false));
            }
            else
            {
                IngameElement.gameObject.SetActive(false);
            }
        }
        else
        {
            UIElement.gameObject.SetActive(false);
        }
    }

    public void Enable(bool ingameElement, bool anim = true)
    {
        if (ingameElement)
        {
            if (anim)
            {
                TimingHandlers.CleanlyKillCoroutine(ref _showingCoroutine);
                _showingCoroutine = Timing.RunCoroutine(Show(true));
            }
            else
            {
                IngameElement.gameObject.SetActive(true);
            }
        }
        else
        {
            UIElement.gameObject.SetActive(true);
        }
    }

    private IEnumerator<float> Show(bool shouldEnable)
    {
        if (_materials == null)
        {
            //Lazy load this shit....
            var renderers = IngameElement.GetComponentsInChildren<MeshRenderer>();
            _materials = new Material[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                _materials[i] = renderers[i].material;
            }
        }

        IngameElement.gameObject.SetActive(true);

        var timer = 0f;
        var delta = 0f;

        for (int i = 0; i < _materials.Length; i++)
        {
            _materials[i].SetTextureOffset(DISSOLVE_OFFSET_PROPERTY, GetRandomOffset());
        }

        while (delta != 1f)
        {
            timer += Time.deltaTime;
            delta = Mathf.Clamp01(timer / ANIMATION_TIME);

            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i].SetFloat(DISSOLVE_PROPERTY, shouldEnable ? 1 - delta : delta);  //The value 0 is on, 1 is off
            }
            yield return Timing.WaitForOneFrame;
        }

        IngameElement.gameObject.SetActive(shouldEnable);
    }

    private Vector2 GetRandomOffset()
    {
        var randomOffset = Vector2.zero;
        randomOffset.x = Random.Range(0f, 1f);
        randomOffset.y = Random.Range(0f, 1f);
        return randomOffset;
    }
}