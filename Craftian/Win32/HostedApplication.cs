using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Craftian.Win32
{
    public class HostedApplication
    {
        public Process Hosted { get; private set; }

        private const int WsBorder = 8388608;
        private const int WsDlgframe = 4194304;
        private const int WsCaption = WsBorder | WsDlgframe;
        private const int WsSysmenu = 524288;
        private const int WsThickframe = 262144;
        private const int WsMinimize = 536870912;
        private const int WsMaximizebox = 65536;
        private const int GwlStyle = (int)-16L;
        private const int GwlExstyle = (int)-20L;
        private const int WsExDlgmodalframe = (int)0x1L;
        private const int SwpNomove = 0x2;
        private const int SwpNosize = 0x1;
        private const int SwpFramechanged = 0x20;
        private const uint MfByposition = 0x400;
        private const uint MfRemove = 0x1000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        public static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);

        private static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return (int)GetWindowLong32(hWnd, nIndex);
            }

            return (int)GetWindowLongPtr64(hWnd, nIndex);
        }

        private static int SetWindowLong(IntPtr hWnd, int nIndex, int newLong)
        {
            if (IntPtr.Size == 4)
            {
                return (int)SetWindowLong32(hWnd, nIndex, newLong);
            }

            return (int)SetWindowLongPtr64(hWnd, nIndex, newLong);
        }

        private void MakeExternalWindowBorderless(IntPtr mainWindowHandle)
        {
            var style = GetWindowLong(mainWindowHandle, GwlStyle);

            style = style & ~WsCaption;
            style = style & ~WsSysmenu;
            style = style & ~WsThickframe;
            style = style & ~WsMinimize;
            style = style & ~WsMaximizebox;

            SetWindowLong(mainWindowHandle, GwlStyle, style);
            style = GetWindowLong(mainWindowHandle, GwlExstyle);
            SetWindowLong(mainWindowHandle, GwlExstyle, style | WsExDlgmodalframe);
            SetWindowPos(mainWindowHandle, new IntPtr(0), 0, 0, 0, 0, SwpNomove | SwpNosize | SwpFramechanged);
        }

        public HostedApplication(ProcessStartInfo info)
        {
            Hosted = Process.Start(info);

            if (Hosted == null)
                return;

            Hosted.WaitForInputIdle();
            MakeExternalWindowBorderless(Hosted.MainWindowHandle);
            SetParent(Hosted.MainWindowHandle, Process.GetCurrentProcess().MainWindowHandle);
        }
    }
}
