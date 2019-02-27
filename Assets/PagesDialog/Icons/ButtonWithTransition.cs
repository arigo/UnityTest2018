using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ButtonWithTransition : Button
{
    public enum TransitionState   /* same as SelectionState, really */
    {
        Normal = 0,
        Highlighted = 1,
        Pressed = 2,
        Disabled = 3
    }
    public delegate void StateTransition(TransitionState state);
    public StateTransition onStateTransition { get; set; }


    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        Debug.Log(state);
        base.DoStateTransition(state, instant);
        if (onStateTransition != null)
            onStateTransition((TransitionState)state);
    }
}
