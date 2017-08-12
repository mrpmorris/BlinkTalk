using System;
using UnityEngine;
using UnityEngine.Assertions;

public class IndicatorController : MonoBehaviour
{
    public Transform[] uiElements = new Transform[0];

    private int uiElementIndex = 0;
    private System.DateTime stateChangedTime;
    private float currentHoverTime;

    private enum State
    {
        WaitingForMeasurements,
        WaitingForIndication,
        IndicationStarted,
        IndicationEnded
    }

    private State _state;
    private State state
    {
        get { return _state; }
        set
        {
            _state = value;
            stateChangedTime = System.DateTime.UtcNow;
        }
    }

    private System.TimeSpan stateAge
    {
        get { return System.DateTime.UtcNow - stateChangedTime; }
    }

    private void Awake()
    {
        IndicationProcessor.Start();
    }

    private void Update()
    {
        if (state == State.WaitingForMeasurements)
        {
            currentHoverTime += Time.deltaTime;
            if (currentHoverTime > 1.25f)
            {
                uiElementIndex = (uiElementIndex + 1) % uiElements.Length;
                currentHoverTime = 0;
            }
            Transform target = uiElements[uiElementIndex];
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * 15f);
        }
    }

    private void OnDestroy()
    {
        IndicationProcessor.Stop();
    }

    public void Reset()
    {
        uiElementIndex = 0;
        currentHoverTime = 0;
    }
}
