using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Lync.Model.Conversation.AudioVideo;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class VideoWindowHost : HwndHost
    {
        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, int style, int x, int y, int width, int height, IntPtr hwndParent, IntPtr hMenu, IntPtr hInst, [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Auto)]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        internal const int
          HOST_ID = 0x00000002,
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          WS_CLIPCHILDREN = 0x2000000,
          CLIPSIBLINGS = 0x4000000;

        private VideoWindow _videoWindow;
        private int _width, _height;

        public VideoWindowHost(VideoWindow videoWindow, double width, double height)
        {
            this._videoWindow = videoWindow;
            this._width = (int)width;
            this._height = (int)height;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            IntPtr hwndHost = IntPtr.Zero;

            hwndHost = CreateWindowEx(0, "static", "",
                                WS_CHILD | WS_VISIBLE,
                                0, 0,
                                _width, _height,
                                hwndParent.Handle,
                                (IntPtr)HOST_ID,
                                IntPtr.Zero,
                                0);

            _videoWindow.WindowStyle = (WS_CHILD | WS_CLIPCHILDREN | CLIPSIBLINGS);
            _videoWindow.Owner = hwndHost.ToInt32();
            _videoWindow.SetWindowPosition(0, 0, _width, _height);
            _videoWindow.Visible = -1;
            return new HandleRef(this, hwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }
    }
}
