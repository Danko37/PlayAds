using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [SerializeField] private List<FrameAnimationClip> animationClips;

    Coroutine currentCoroutine;

    private void Start()
    {
        Play(animationClips[0].Name);
    }

    public void Play(string clipName)
    {
        if(currentCoroutine != null) StopCoroutine(currentCoroutine);
        
        var currentClip = animationClips.Find(x => x.Name == clipName);
        currentCoroutine = StartCoroutine(PlayAnimation(currentClip));
    }
    
    public void Stop()
    {
        if(currentCoroutine == null)  return;
        StopCoroutine(currentCoroutine);
    }

    IEnumerator PlayAnimation(FrameAnimationClip clip)
    {
        int frameCount = clip.Frames.Length;
        float frameDuration = 1f / clip.FPS;

        do
        {
            for (int i = 0; i < frameCount; i++)
            {
                spriteRenderer.sprite = clip.Frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        } while (clip.Loop);
    }
}
