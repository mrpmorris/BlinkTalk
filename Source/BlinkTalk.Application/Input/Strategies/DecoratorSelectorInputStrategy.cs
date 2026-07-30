using System;
using System.Collections.Generic;
using BlinkTalk.Application.Text;

namespace BlinkTalk.Application.Input.Strategies;

/// <summary>
/// The letter-decorator level, entered by selecting the decorator key at the start of the first row.
/// It scans the language's combining marks in a popup; indicating types the focused mark straight
/// into the word being composed and returns to row scanning, exactly as typing a letter does. Marks
/// are appended, not composed, so several can be applied by opening the popup again.
/// <para>
/// Giving up returns to scanning the keys of the row already chosen, so declining a decorator does
/// not cost the person that row.
/// </para>
/// </summary>
public sealed class DecoratorSelectorInputStrategy : IInputStrategy
{
	private IScanController Controller = null!;
	private FocusCycler? Cycler;
	private IReadOnlyList<string> Decorators = Array.Empty<string>();
	private int FocusedIndex;

	public void ChildStrategyActivated(IInputStrategy childStrategy) { }

	public void Initialize(IScanController controller)
	{
		Controller = controller;
		Decorators = controller.Keyboard.Decorators;
		if (Decorators.Count == 0)
		{
			// Nothing to offer, so hand straight back. The key is only on the keyboard for a language
			// that has marks, but this level must not strand the person either way.
			controller.Pop();
			return;
		}

		controller.SetChoosingDecorator(true);
		Cycler = controller.NewCycler(FocusChanged, firstCycleMultiplier: Consts.FirstCycleDelayMultiplier);
		Cycler.Start(Decorators.Count);
	}

	public void ReceiveIndication()
	{
		Cycler?.Stop();
		Controller.Sentence.InputText(Decorators[FocusedIndex]);
		Controller.Pop(2);
	}

	public void Terminated()
	{
		Cycler?.Stop();
		Controller.SetChoosingDecorator(false);
	}

	private void FocusChanged(int focusIndex)
	{
		FocusedIndex = focusIndex;
		Controller.SetHighlight(HighlightTarget.ForDecorator(focusIndex));
		if (Cycler!.FocusChangeCount > Decorators.Count + 1)
			Controller.Pop();
	}
}
