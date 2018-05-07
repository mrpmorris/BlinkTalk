using System;
using UnityEngine;

namespace BlinkTalk.Typing.InputStrategies.KeyboardInputStrategies
{
	public class KeyboardInputStrategy : MonoBehaviour, IInputStrategy
	{
		public bool Live { get { return _live; } set { SetLive(value); } }

		private KeyboardInputStrategyState State {  get { return _state; } set { SetState(value);  } }
		private TypingController Controller;
		private RowSelectionStrategy RowSelectionStrategy;
		private KeySelectionStrategy KeySelectionStrategy;
		private bool _live;
		private KeyboardInputStrategyState _state;

		public void Initialize(TypingController controller)
		{
			Controller = controller;
			RowSelectionStrategy.Initialize(controller);
			KeySelectionStrategy.Initialize(controller);
		}

		private void SetLive(bool value)
		{
			_live = value;
			State = KeyboardInputStrategyState.SelectingRow;
		}

		private void SetState(KeyboardInputStrategyState value)
		{
			_state = value;
			RowSelectionStrategy.Live = value == KeyboardInputStrategyState.SelectingRow;
			KeySelectionStrategy.Live = value == KeyboardInputStrategyState.SelectingKey;
			switch(State)
			{
				case KeyboardInputStrategyState.SelectingRow:
					RowSelectionStrategy.Reset();
					break;
				case KeyboardInputStrategyState.SelectingKey:
					KeySelectionStrategy.Reset(RowSelectionStrategy.SelectedRow);
					break;
				default:
					throw new NotImplementedException(State + "");
			}
		}

		private void Awake()
		{
			RowSelectionStrategy = gameObject.AddComponent<RowSelectionStrategy>();
			KeySelectionStrategy = gameObject.AddComponent<KeySelectionStrategy>();
		}

		private void Update()
		{
			if (Controller.HasIndicated)
				Indicate();
		}

		private void Indicate()
		{
			if (!Live)
				return;

			switch(State)
			{
				case KeyboardInputStrategyState.SelectingRow:
					SetState(KeyboardInputStrategyState.SelectingKey);
					break;
				case KeyboardInputStrategyState.SelectingKey:
					Debug.Log(KeySelectionStrategy.GetKey());
					SetState(KeyboardInputStrategyState.SelectingRow);
					break;
				default: throw new NotImplementedException(State + "");
			}
		}
	}
}
