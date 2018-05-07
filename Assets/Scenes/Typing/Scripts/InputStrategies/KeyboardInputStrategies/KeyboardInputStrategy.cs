using System;
using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class KeyboardInputStrategy : MonoBehaviour, IKeyboardInputStrategy
	{
		private bool Active;
		private KeyboardInputStrategyState _state;
		private KeyboardInputStrategyState State { get { return _state; } set { SetState(value); } }
		private ITypingController Controller;
		private IRowSelectionStrategy RowSelectionStrategy;
		private IKeySelectionStrategy KeySelectionStrategy;

		public void Initialize(ITypingController controller)
		{
			Controller = controller;
			RowSelectionStrategy.Initialize(Controller);
			KeySelectionStrategy.Initialize(Controller, this);
		}

		public void Activate()
		{
			Active = true;
			State = KeyboardInputStrategyState.SelectingRow;
		}

		public void MayRemainActive(bool value)
		{
			if (!value) 
				Active = false;

			RowSelectionStrategy.MayRemainActive(false);
			KeySelectionStrategy.MayRemainActive(false);
		}

		public void ChildInputStrategyExpired()
		{
			switch(State)
			{
				case KeyboardInputStrategyState.SelectingKey:
					State = KeyboardInputStrategyState.SelectingRow;
					break;

				case KeyboardInputStrategyState.SelectingRow:
					break;

				default:
					throw new NotImplementedException(State + "");
			}
		}

		private void SetState(KeyboardInputStrategyState value)
		{
			_state = value;
			RowSelectionStrategy.MayRemainActive(value == KeyboardInputStrategyState.SelectingRow);
			KeySelectionStrategy.MayRemainActive(value == KeyboardInputStrategyState.SelectingKey);

			switch (State)
			{
				case KeyboardInputStrategyState.SelectingRow:
					RowSelectionStrategy.Activate();
					break;
				case KeyboardInputStrategyState.SelectingKey:
					KeySelectionStrategy.Activate(RowSelectionStrategy.SelectedRow);
					break;
				default:
					throw new NotImplementedException(State + "");
			}
		}

		private void Awake()
		{
			RowSelectionStrategy = gameObject.AddServiceComponent<IRowSelectionStrategy, RowSelectionStrategy>();
			KeySelectionStrategy = gameObject.AddServiceComponent<IKeySelectionStrategy, KeySelectionStrategy>();
		}

		private void Update()
		{
			if (Controller.HasIndicated)
				Indicate();
		}

		private void Indicate()
		{
			if (!Active)
				return;

			switch (State)
			{
				case KeyboardInputStrategyState.SelectingRow:
					SetState(KeyboardInputStrategyState.SelectingKey);
					break;
				case KeyboardInputStrategyState.SelectingKey:
					Debug.Log(KeySelectionStrategy.SelectedKeyText);
					SetState(KeyboardInputStrategyState.SelectingRow);
					break;
				default: throw new NotImplementedException(State + "");
			}
		}
	}
}
