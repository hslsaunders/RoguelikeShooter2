using System;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public enum ArmUseState
    {
        None,
        HoldingRoot,
        HoldingSupport,
        SuperfluouslyHolding,
        DoingAction,
        Unusable 
    }
}