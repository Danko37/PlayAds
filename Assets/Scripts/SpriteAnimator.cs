using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [SerializeField] private List<FrameAnimationClip> animationClips;

    [Tooltip("запускает первый клип при включении объекта")]
    [SerializeField] private bool playOnEnable;

    Coroutine currentCoroutine;

    public event Action onAnimationFinished;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>(); 
        }
    }

    private void OnEnable()
    {
        if(playOnEnable && animationClips.Count > 0)
        {
            Play(animationClips[0].Name);
        }
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
        var frameCount = clip.Frames.Length;
        var frameDuration = 1f / clip.FPS;

        do
        {
            for (var i = 0; i < frameCount; i++)
            {
                spriteRenderer.sprite = clip.Frames[i];
                yield return new WaitForSeconds(frameDuration);
                if(!clip.Loop && i == frameCount - 1)
                    
                {
                    onAnimationFinished?.Invoke();
                }
            }
            
            
        } while (clip.Loop);
    }
}
