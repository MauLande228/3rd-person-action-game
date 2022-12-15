using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SniperFlee
{
    [System.Serializable]
    public class Input
    {
        public KeyCode _primary;
        public KeyCode _alternate;

        public bool Pressed()
        {
            return UnityEngine.Input.GetKey(_primary) || UnityEngine.Input.GetKey(_alternate);
        }

        public bool PressedDown()
        {
            return UnityEngine.Input.GetKeyDown(_primary) || UnityEngine.Input.GetKeyDown(_alternate);
        }

        public bool PressedUp()
        {
            return UnityEngine.Input.GetKeyUp(_primary) || UnityEngine.Input.GetKeyUp(_alternate);
        }
    }

    public class PlayerInput : MonoBehaviour
    {
        public Input Forward;
        public Input Backward;
        public Input Right;
        public Input Left;
        public Input Sprint;
        public Input Crouch;
        public Input Prone;

        public int MoveAxisForward
        {
            get
            {
                if(Forward.Pressed() && Backward.Pressed()) { return 0; }
                else if(Forward.Pressed()) { return 1; }
                else if(Backward.Pressed()) { return -1; }
                else { return 0; }
            }
        }

        public int MoveAxisRight
        {
            get
            {
                if(Right.Pressed() && Left.Pressed()) { return 0; }
                else if (Right.Pressed()) { return 1; }
                else if (Left.Pressed()) { return -1; }
                else { return 0; }
            }
        }

        #region MOUSE PARAMS
        public const string MouseX = "Mouse X";
        public const string MouseY = "Mouse Y";
        public const string MouseScroll = "Mouse ScrollWheel";

        public static float MouseXInput { get=> UnityEngine.Input.GetAxis(MouseX); }
        public static float MouseYInput { get=> UnityEngine.Input.GetAxis(MouseY); }
        public static float MouseScrollInput { get=> UnityEngine.Input.GetAxis(MouseScroll); }
        #endregion

    }
}
