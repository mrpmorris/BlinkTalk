using System;
using System.Collections;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class FocusCycler
    {
        private int FocusIndex;
        private float FirstCycleDelayMultiplier = 1.5f;
        private readonly MonoBehaviour Owner;
        private readonly int NumberOfItems;
        private readonly Action<int> FocusChanged;
        private readonly Func<int, bool> MayFocus;
        private Coroutine CycleFocusCoRoutine;

        public FocusCycler(MonoBehaviour owner, int numberOfItems, Action<int> focusChanged, float firstCycleDelayMultiplier = 1, Func<int, bool> mayFocus = null)
        {
            if (firstCycleDelayMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(firstCycleDelayMultiplier));
            Owner = owner;
            NumberOfItems = numberOfItems;
            FocusChanged = focusChanged;
            FirstCycleDelayMultiplier = firstCycleDelayMultiplier;
            MayFocus = mayFocus ?? new Func<int, bool>(x => true);
        }

        public void Start()
        {
            if (CycleFocusCoRoutine != null)
                throw new InvalidOperationException("FocusCycler already started");

            FocusIndex = 0;
            CycleFocusCoRoutine = Owner.StartCoroutine(CycleFocus());
        }

        public void Stop()
        {
            if (CycleFocusCoRoutine != null)
            {
                Owner.StopCoroutine(CycleFocusCoRoutine);
                CycleFocusCoRoutine = null;
            }
        }

        private IEnumerator CycleFocus()
        {
            float delayMultiplier = FirstCycleDelayMultiplier;
            while (true)
            {
                if (MayFocus(FocusIndex))
                {
                    FocusChanged(FocusIndex);
                    yield return new WaitForSeconds(Consts.CycleDelay * delayMultiplier);
                    delayMultiplier = 1;
                }
                FocusIndex = (FocusIndex + 1) % NumberOfItems;
            }
        }
    }
}