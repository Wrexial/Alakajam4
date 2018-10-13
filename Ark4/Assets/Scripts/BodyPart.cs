using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BodyPart : MonoBehaviour
{
    private const float ANIMATION_TIME = 0.4f;
    public SlotType Type;
    private Material _material;
    private CoroutineHandle? _showingCoroutine;
    private readonly int DISSOLVE_PROPERTY = Shader.PropertyToID("_SliceAmount");
    private readonly int DISSOLVE_OFFSET_PROPERTY = Shader.PropertyToID("_SliceGuide");

    public void Disable(bool anim = false)
    {
        if (anim)
        {
            _showingCoroutine = Timing.RunCoroutine(Show(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void Enable(bool anim = true)
    {
        if (anim)
        {
            _showingCoroutine = Timing.RunCoroutine(Show(true));
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    private IEnumerator<float> Show(bool shouldEnable)
    {
        if (_material == null)
        {
            //Lazy load this shit....
            _material = GetComponent<MeshRenderer>().material;
        }

        gameObject.SetActive(true);

        var timer = 0f;
        var delta = 0f;

        _material.SetTextureOffset(DISSOLVE_OFFSET_PROPERTY, GetRandomOffset());
        while (delta != 1f)
        {
            timer += Time.deltaTime;
            delta = Mathf.Clamp01(timer / ANIMATION_TIME);
            _material.SetFloat(DISSOLVE_PROPERTY, shouldEnable ? 1 - delta : delta);  //The value 0 is on, 1 is off
            yield return Timing.WaitForOneFrame;
        }

        gameObject.SetActive(shouldEnable);
    }

    private Vector2 GetRandomOffset()
    {
        var randomOffset = Vector2.zero;
        randomOffset.x = Random.Range(0f, 1f);
        randomOffset.y = Random.Range(0f, 1f);
        return randomOffset;
    }
}