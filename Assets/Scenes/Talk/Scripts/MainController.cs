using Assets.Scripts.Presenters;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Assets.Scripts.ViewModels
{
    public class MainController : MonoBehaviour
    {
        public MainViewModelPresenter mainViewModelPresenter;
        public Button selectionButton1;
        public Button selectionButton2;
        public Button selectionButton3;
        public Button actionButton;
        public IndicatorController indicatorController;

        private Stack<IInteractionController> controllerStack = new Stack<IInteractionController>();

        // Use this for initialization
        void Start()
        {
            Debug.Assert(mainViewModelPresenter, "mainViewModelPresenter");
            Debug.Assert(selectionButton1, "selectionButton1");
            Debug.Assert(selectionButton2, "selectionButton2");
            Debug.Assert(selectionButton3, "selectionButton3");
            Debug.Assert(actionButton, "actionButton");
            Debug.Assert(indicatorController, "indicatorController");

            TextToSpeech.Speak("Welcome to Blink Talk");

            selectionButton1.onClick.AddListener(SelectionButton1Click);
            selectionButton2.onClick.AddListener(SelectionButton2Click);
            selectionButton3.onClick.AddListener(SelectionButton3Click);
            actionButton.onClick.AddListener(ActionButtonClick);

            CreateMainInteractionController();
        }

        public IInteractionController currentInteractionController
        {
            get { return controllerStack.Peek(); }
        }

        private void CreateMainInteractionController()
        {
            var mainInteractionController = new WordBuilderInteractionController();
            controllerStack.Push(mainInteractionController);
            HookupInteractionControllerEvents(mainInteractionController);
            mainViewModelPresenter.UpdateUI(mainInteractionController.viewModel);
        }

        private void HookupInteractionControllerEvents(IInteractionController interactionController)
        {
            interactionController.childControllerAdded += ChildControllerAdded;
            interactionController.completed += InteractionCompleted;
            interactionController.viewModelChanged += InteractionViewModelChanged;
        }

        private void UnhookControllerEvents(IInteractionController interactionController)
        {
            interactionController.childControllerAdded -= ChildControllerAdded;
            interactionController.completed -= InteractionCompleted;
            interactionController.viewModelChanged -= InteractionViewModelChanged;
        }

        private void InteractionViewModelChanged(object sender, InteractionControllerViewModelEventArgs e)
        {
            mainViewModelPresenter.UpdateUI(e.viewModel);
            indicatorController.Reset();
        }

        private void InteractionCompleted(object sender, EventArgs e)
        {
            IInteractionController completedController = controllerStack.Pop();
            UnhookControllerEvents(completedController);
            mainViewModelPresenter.UpdateUI(currentInteractionController.viewModel);
        }

        private void ChildControllerAdded(object sender, InteractionControllerEventArgs e)
        {
            controllerStack.Push(e.interactionController);
            HookupInteractionControllerEvents(e.interactionController);
            mainViewModelPresenter.UpdateUI(currentInteractionController.viewModel);
            indicatorController.Reset();
        }

        private void SelectionButton1Click()
        {
            currentInteractionController.ClickSelection1();
        }

        private void SelectionButton2Click()
        {
            currentInteractionController.ClickSelection2();
        }

        private void SelectionButton3Click()
        {
            currentInteractionController.ClickSelection3();
        }

        private void ActionButtonClick()
        {
            currentInteractionController.ClickAction();
        }

    }
}
