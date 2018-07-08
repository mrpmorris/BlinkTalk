using System;
using System.Collections;
using UnityEngine;

namespace BlinkTalk.Typing
{
    public class FocusCycler
    {
        private int FocusIndex;
        private readonly MonoBehaviour Owner;
        private readonly int NumberOfItems;
        private readonly Action<int> FocusChanged;
        private Coroutine CycleFocusCoRoutine;

        public FocusCycler(MonoBehaviour owner, int numberOfItems, Action<int> focusChanged)
        {
            Owner = owner;
            NumberOfItems = numberOfItems;
            FocusChanged = focusChanged;
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
            float speedFactor = 1.5f;
            while (true)
            {
                FocusChanged(FocusIndex);
                yield return new WaitForSeconds(Consts.CycleDelay * speedFactor);
                speedFactor = 1;
                FocusIndex = (FocusIndex + 1) % NumberOfItems;
            }
        }
    }
}