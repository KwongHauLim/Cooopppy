﻿using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cooopppy
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private IKeyboardMouseEvents m_GlobalHook;
        internal static bool hasClipboardChanged = false;
        private NotificationForm form;
        private bool isParse = false;
        private bool running = false;
        private int parseIndex = 0;
        private bool parseAssign = false;

        public bool Enabled { get; set; } = true;
        public bool LockCapture { get; set; } = false;
        public List<string> Records { get; set; } = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();
            Hook.GlobalEvents().OnCombination(new Dictionary<Combination, Action>
            {
                {
                    Combination.FromString("Control+C"), ()=>
                    {
                        Console.WriteLine("Ctrl+C");
                        if (isParse)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Records.Clear();
                                uiList.Children.Clear();
                                isParse = false;
                            });
                        }

                        parseIndex = -1;
                    }
                },
                {
                    Combination.FromString("Control+V"), ()=>
                    {
                        isParse = true;
                        if (++parseIndex >= Records.Count)
                            parseIndex = 0;
                        parseAssign = true;
                        hasClipboardChanged = false;
                        Clipboard.SetDataObject(Records[parseIndex]);
                    }
                }
            });

            form = new NotificationForm();

            running = true;
            Task.Run(() =>
            {
                while (running)
                {
                    try
                    {
                        if (!Enabled || LockCapture)
                            hasClipboardChanged = false;
                        else if (hasClipboardChanged && Clipboard.ContainsText())
                        {
                            if (parseAssign)
                            {
                                // Assign by program, skip is ok
                                parseAssign = false;
                                hasClipboardChanged = false;
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    var s = Clipboard.GetText();
                                    Records.Add(s);

                                    s = s.Replace(Environment.NewLine, "\\n");
                                    if (s.Length >= 13)
                                        s = s.Substring(0, 13) + "...";
                                    var btn = new Button();
                                    btn.Content = s;
                                    uiList.Children.Add(btn);

                                    parseAssign = true;
                                    hasClipboardChanged = false;
                                    Clipboard.SetDataObject(Records[0]);
                                });
                            }
                        }
                    }
                    catch { }
                }
            });
        }

        private void OnClosed(object sender, EventArgs e)
        {
            running = false;
        }

        /// <summary>
        /// Hidden form to recieve the WM_CLIPBOARDUPDATE message.
        /// </summary>
        private class NotificationForm : System.Windows.Forms.Form
        {
            public NotificationForm()
            {
                NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
                NativeMethods.AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    hasClipboardChanged = true;
                }
                base.WndProc(ref m);
            }
        }
    }

    internal static class NativeMethods
    {
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    }
}
