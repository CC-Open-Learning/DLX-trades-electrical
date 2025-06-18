using System;
using UnityEngine;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Defines mouse event actions for PointClickNavigation
    ///     in order to handle both mouse clicks and camera panning without
    ///     having these actions conflict 
    /// </summary>
    internal class MouseHandler
    {
        // Enum
        private enum MouseButtonState
        {
            Neutral,
            Down,
            Up
        }

        // Actions
        public Action ShortClick;
        public Action LongClick;
        public Action MouseHold;

        // Fields
        private MouseButtonState mouseBtnState;
        private float shortClickDuration = 0.17f;
        private float mouseBtnHoldTime = 0f;

        // Properties
        public float ShortClickDuration
        {
            get => shortClickDuration;
            set
            {
                if (value < 0f)
                {
                    Debug.LogWarning("Short Click Duration must be a positive value!");
                    return;
                }
                shortClickDuration = value;
            }
        }


        /// <summary>
        ///     Called by the <see cref="WaypointHandler"/> to check the state of
        ///     the mouse click action
        /// </summary>
        /// <remarks>
        ///     Invokes either the MouseHold, LongClick, or ShortClick events depending
        ///     on how long the mouse button has/had been pressed 
        /// </remarks>
        public void HandleMouseInteractions()
        {
            UpdateMouseButtonState();

            if (mouseBtnState == MouseButtonState.Down)
            {
                mouseBtnHoldTime += Time.deltaTime;
                MouseHold?.Invoke();
            }
            else if (mouseBtnState == MouseButtonState.Up)
            {
                if (mouseBtnHoldTime > shortClickDuration)
                {
                    LongClick?.Invoke();
                }
                else
                {
                    ShortClick?.Invoke();
                }

                mouseBtnHoldTime = 0f;
                mouseBtnState = MouseButtonState.Neutral;
            }
        }

        /// <summary>
        ///     Handles mouse down and mouse up for the primary mouse button
        /// </summary>
        /// <remarks>
        ///     Primary mouse always has value 0. This is usually left-click
        ///     but may correspond to a right-click depending on the user's 
        ///     operating system settings.
        /// </remarks>
        private void UpdateMouseButtonState()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mouseBtnState = MouseButtonState.Down;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                mouseBtnState = MouseButtonState.Up;
            }
        }
    }
}
