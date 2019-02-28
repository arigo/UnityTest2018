using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ButtonWithTransition : Button
{
    public enum TransitionState   /* same as SelectionState, really */
    {
        Normal = SelectionState.Normal,
        Highlighted = SelectionState.Highlighted,
        Pressed = SelectionState.Pressed,
        Disabled = SelectionState.Disabled,
    }
    public delegate void StateTransition(TransitionState state);
    public StateTransition onStateTransition { get; set; }


    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (onStateTransition != null)
            onStateTransition((TransitionState)state);
    }
}
