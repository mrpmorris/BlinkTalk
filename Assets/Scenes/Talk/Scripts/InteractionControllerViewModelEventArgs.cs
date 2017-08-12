using Assets.Scripts.ViewModels;
using System;

namespace Assets.Scripts
{
    public class InteractionControllerViewModelEventArgs : EventArgs
    {
        public readonly InteractionControllerViewModel viewModel;

        public InteractionControllerViewModelEventArgs(InteractionControllerViewModel viewModel)
        {
            if (viewModel == null)
                throw new NullReferenceException("viewModel");
            this.viewModel = viewModel;
        }
    }
}
