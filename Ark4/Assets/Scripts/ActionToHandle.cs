using UnityEngine;

public class QueuedPetActions : CustomYieldInstruction
{
    public bool Handled = false;

    public override bool keepWaiting
    {
        get
        {
            return !Handled;
        }
    }
}