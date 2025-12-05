using Fusion;
using UnityEngine;

namespace TheColosseumChallenge.Networking
{
    /// <summary>
    /// Network input data structure for player input synchronization.
    /// Contains all player input that needs to be synchronized across the network.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        #region Movement Input
        
        /// <summary>
        /// Movement direction input (WASD/Joystick).
        /// </summary>
        public Vector2 MoveDirection;
        
        /// <summary>
        /// Look/aim input delta (Mouse/Right stick).
        /// </summary>
        public Vector2 LookDelta;
        
        #endregion

        #region Button Flags
        
        /// <summary>
        /// Bitfield for button states to minimize network data.
        /// </summary>
        public NetworkButtons Buttons;
        
        #endregion

        #region Button Indices
        
        /// <summary>
        /// Button index constants for the Buttons bitfield.
        /// </summary>
        public const int BUTTON_JUMP = 0;
        public const int BUTTON_SPRINT = 1;
        public const int BUTTON_CROUCH = 2;
        public const int BUTTON_FIRE = 3;
        public const int BUTTON_AIM = 4;
        public const int BUTTON_RELOAD = 5;
        public const int BUTTON_INTERACT = 6;
        
        #endregion

        #region Helper Properties
        
        /// <summary>
        /// Whether the jump button is pressed.
        /// </summary>
        public bool IsJumpPressed => Buttons.IsSet(BUTTON_JUMP);
        
        /// <summary>
        /// Whether the sprint button is pressed.
        /// </summary>
        public bool IsSprintPressed => Buttons.IsSet(BUTTON_SPRINT);
        
        /// <summary>
        /// Whether the crouch button is pressed.
        /// </summary>
        public bool IsCrouchPressed => Buttons.IsSet(BUTTON_CROUCH);
        
        /// <summary>
        /// Whether the fire button is pressed.
        /// </summary>
        public bool IsFirePressed => Buttons.IsSet(BUTTON_FIRE);
        
        /// <summary>
        /// Whether the aim button is pressed.
        /// </summary>
        public bool IsAimPressed => Buttons.IsSet(BUTTON_AIM);
        
        /// <summary>
        /// Whether the reload button is pressed.
        /// </summary>
        public bool IsReloadPressed => Buttons.IsSet(BUTTON_RELOAD);
        
        /// <summary>
        /// Whether the interact button is pressed.
        /// </summary>
        public bool IsInteractPressed => Buttons.IsSet(BUTTON_INTERACT);
        
        #endregion
    }
}