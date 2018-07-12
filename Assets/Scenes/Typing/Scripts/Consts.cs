using System;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public static class Consts
    {
        public const float CycleDelay = 1.5f;
        public readonly static Func<float> FocusLerpFactor = () => Time.time < CycleDelay ? 1 : Time.deltaTime * 20f;
    }
}
