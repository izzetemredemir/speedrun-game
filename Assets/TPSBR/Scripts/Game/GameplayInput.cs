using Fusion;

namespace TPSBR
{
	using UnityEngine;

	public enum EGameplayInputAction
	{
		Jump          = 1,
		Aim           = 2,
		Attack        = 3,
		ToggleSpeed   = 4,
		Reload        = 6,
		Interact      = 7,
		ToggleSide    = 8,
		ToggleJetpack = 9,
		Thrust        = 10,
	}

	public struct GameplayInput : INetworkInput
	{
		// PUBLIC MEMBERS

		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Actions;
		public byte           Weapon;

		public bool Jump          { get { return Actions.IsSet(EGameplayInputAction.Jump);          } set { Actions.Set(EGameplayInputAction.Jump,          value); } }
		public bool Aim           { get { return Actions.IsSet(EGameplayInputAction.Aim);           } set { Actions.Set(EGameplayInputAction.Aim,           value); } }
		public bool Attack        { get { return Actions.IsSet(EGameplayInputAction.Attack);        } set { Actions.Set(EGameplayInputAction.Attack,        value); } }
		public bool ToggleSpeed   { get { return Actions.IsSet(EGameplayInputAction.ToggleSpeed);   } set { Actions.Set(EGameplayInputAction.ToggleSpeed,   value); } }
		public bool Reload        { get { return Actions.IsSet(EGameplayInputAction.Reload);        } set { Actions.Set(EGameplayInputAction.Reload,        value); } }
		public bool Interact      { get { return Actions.IsSet(EGameplayInputAction.Interact);      } set { Actions.Set(EGameplayInputAction.Interact,      value); } }
		public bool ToggleSide    { get { return Actions.IsSet(EGameplayInputAction.ToggleSide);    } set { Actions.Set(EGameplayInputAction.ToggleSide,    value); } }
		public bool ToggleJetpack { get { return Actions.IsSet(EGameplayInputAction.ToggleJetpack); } set { Actions.Set(EGameplayInputAction.ToggleJetpack, value); } }
		public bool Thrust        { get { return Actions.IsSet(EGameplayInputAction.Thrust);        } set { Actions.Set(EGameplayInputAction.Thrust,        value); } }
	}

	public static class GameplayInputActionExtensions
	{
		// PUBLIC METHODS

		public static bool IsActive(this EGameplayInputAction action, GameplayInput input)
		{
			return input.Actions.IsSet(action) == true;
		}

		public static bool WasActivated(this EGameplayInputAction action, GameplayInput currentInput, GameplayInput previousInput)
		{
			return currentInput.Actions.IsSet(action) == true && previousInput.Actions.IsSet(action) == false;
		}

		public static bool WasDeactivated(this EGameplayInputAction action, GameplayInput currentInput, GameplayInput previousInput)
		{
			return currentInput.Actions.IsSet(action) == false && previousInput.Actions.IsSet(action) == true;
		}
	}
}
