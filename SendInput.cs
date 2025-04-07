using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static DynamicSample.NativeMethods;

namespace DynamicSample
{
    internal static class NativeMethods
    {
        [Flags]
        public enum KeyboardFlags : uint
        {
            NONE = 0,

            /// <summary>
            /// KEYEVENTF_EXTENDEDKEY = 0x0001 (If specified, the scan code was preceded by a prefix byte that has the value 0xE0 (224).)
            /// </summary>
            EXTENDED_KEY = 1,

            /// <summary>
            /// KEYEVENTF_KEYUP = 0x0002 (If specified, the key is being released. If not specified, the key is being pressed.)
            /// </summary>
            KEY_UP = 2,

            /// <summary>
            /// KEYEVENTF_UNICODE = 0x0004 (If specified, wScan identifies the key and wVk is ignored.)
            /// </summary>
            UNICODE = 4,

            /// <summary>
            /// KEYEVENTF_SCANCODE = 0x0008 (Windows 2000/XP: If specified, the system synthesizes a VK_PACKET keystroke. The wVk parameter must be zero. This flag can only be combined with the KEYEVENTF_KEYUP flag. For more information, see the Remarks section.)
            /// </summary>
            SCAN_CODE = 8,
        }

        [Flags]
        public enum MouseFlags : uint
        {
            /// <summary>
            /// Specifies that movement occurred.
            /// </summary>
            MOVE = 0x0001,

            /// <summary>
            /// Specifies that the left button was pressed.
            /// </summary>
            LEFT_DOWN = 0x0002,

            /// <summary>
            /// Specifies that the left button was released.
            /// </summary>
            LEFT_UP = 0x0004,

            /// <summary>
            /// Specifies that the right button was pressed.
            /// </summary>
            RIGHT_DOWN = 0x0008,

            /// <summary>
            /// Specifies that the right button was released.
            /// </summary>
            RIGHT_UP = 0x0010,

            /// <summary>
            /// Specifies that the middle button was pressed.
            /// </summary>
            MIDDLE_DOWN = 0x0020,

            /// <summary>
            /// Specifies that the middle button was released.
            /// </summary>
            MIDDLE_UP = 0x0040,

            /// <summary>
            /// Windows 2000/XP: Specifies that an X button was pressed.
            /// </summary>
            X_DOWN = 0x0080,

            /// <summary>
            /// Windows 2000/XP: Specifies that an X button was released.
            /// </summary>
            X_UP = 0x0100,

            /// <summary>
            /// Windows NT/2000/XP: Specifies that the wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData. 
            /// </summary>
            VERTICAL_WHEEL = 0x0800,

            /// <summary>
            /// Specifies that the wheel was moved horizontally, if the mouse has a wheel. The amount of movement is specified in mouseData. Windows 2000/XP:  Not supported.
            /// </summary>
            HORIZONTAL_WHEEL = 0x1000,

            /// <summary>
            /// Windows 2000/XP: Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
            /// </summary>
            VIRTUAL_DESK = 0x4000,

            /// <summary>
            /// Specifies that the dx and dy members contain normalized absolute coordinates. If the flag is not set, dxand dy contain relative data (the change in position since the last reported position). This flag can be set, or not set, regardless of what kind of mouse or other pointing device, if any, is connected to the system. For further information about relative mouse motion, see the following Remarks section.
            /// </summary>
            ABSOLUTE = 0x8000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeybdInput
        {
            public ushort virtualKey;
            public ushort scanCode;
            public KeyboardFlags flags;
            public uint timeStamp;
            public IntPtr extraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int deltaX;
            public int deltaY;
            public int mouseData;
            public MouseFlags flags;
            public uint time;
            public IntPtr extraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint message;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MouseInput mouse;
            [FieldOffset(0)]
            public KeybdInput keyboard;
            [FieldOffset(0)]
            public HardwareInput hardware;
        }
        public enum InputType
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HARDWARE = 2
        }
        public struct Input
        {
            public InputType Type;
            public InputUnion Union;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
    }

    internal static class MouseClickMethods
    {
        public static void ClickMouse(Point point)
        {
            Input[] inputs =
            {
                new Input
                {
                    Type = InputType.MOUSE,
                    Union = new InputUnion
                    {
                        mouse = new MouseInput
                        {
                            deltaX = point.X,
                            deltaY = point.Y,
                            flags = MouseFlags.MOVE | MouseFlags.ABSOLUTE | MouseFlags.VIRTUAL_DESK
                        }
                    }
                },

                new Input
                {
                    Type = InputType.MOUSE,
                    Union = new InputUnion
                    {
                        mouse = new MouseInput
                        {
                            flags = MouseFlags.LEFT_DOWN
                        }
                    }
                },

                new Input
                {
                    Type = InputType.MOUSE,
                    Union = new InputUnion
                    {
                        mouse = new MouseInput
                        {
                            flags = MouseFlags.LEFT_UP
                        }
                    }
                }
            };

            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) != (uint)inputs.Length)
                throw new Exception($@"{nameof(ClickMouse)} error = {Marshal.GetLastWin32Error()}");
        }
    }
}
