using Assets.Scripts.ViewModels;
using System;

namespace Assets.Scripts
{
    public interface IInteractionController
    {
        InteractionControllerViewModel viewModel { get; }
        void ClickSelection1();
        void ClickSelection2();
        void ClickSelection3();
        void ClickAction();

        event EventHandler completed;
        event EventHandler<InteractionControllerEventArgs> childControllerAdded;
        event EventHandler<InteractionControllerViewModelEventArgs> viewModelChanged;
    }
}
