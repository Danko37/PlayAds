using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteAnimation
{
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
            if (currentClip == null) return;
        
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
                }
            } while (clip.Loop);

            currentCoroutine = null;

            // Гасим рендерер ДО события: подписчик выключит объект в этом же кадре,
            // и за время до выключения не мелькнёт ни последний, ни первый кадр.
            spriteRenderer.sprite = null;

            onAnimationFinished?.Invoke();
        }
    }
}
