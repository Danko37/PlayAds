using System;
using UnityEngine;

[Serializable]
public class FrameAnimationClip
{
    public string Name;
    public Sprite[] Frames;
    public float FPS;
    public bool Loop;
}