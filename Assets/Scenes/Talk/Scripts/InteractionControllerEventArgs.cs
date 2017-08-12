using System;

namespace Assets.Scripts
{
    public class InteractionControllerEventArgs : EventArgs
    {
        public readonly IInteractionController interactionController;

        public InteractionControllerEventArgs(IInteractionController interactionController)
        {
            if (interactionController == null)
                throw new NullReferenceException("interactionController");
            this.interactionController = interactionController;
        }
    }
}
