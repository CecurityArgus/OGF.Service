using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ARGUS.Controls
{
    internal static class Console
    {
        // Because it is not possible to write on a console in a Windows Application

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        /*
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);
        */
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        /// <summary>
        /// Console Application?
        /// </summary>
        public static bool HasConsole
        {
            get { return GetConsoleWindow() != IntPtr.Zero; }
        }


        /*
        /// <summary>
        /// Creates a new console instance if the process is not attached to a console already.
        /// </summary>
        public static void CreateConsole()
        {
            if (!HasConsole)
            {
                AllocConsole();
                InvalidateOutAndError();
            }            
        }

        /// <summary>
        /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
        /// </summary>
        public static void HideCreatedConsole()
        {
            if (HasConsole)
            {
                SetOutAndErrorNull();
                FreeConsole();
            }            
        }*/

        private static void InvalidateOutAndError()
        {
            Type type = typeof(System.Console);

            System.Reflection.FieldInfo outField = type.GetField("_out",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.FieldInfo errorField = type.GetField("_error",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.MethodInfo initializeStdOutError = type.GetMethod("InitializeStdOutError",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Debug.Assert(outField != null);
            Debug.Assert(errorField != null);

            Debug.Assert(initializeStdOutError != null);

            outField.SetValue(null, null);
            errorField.SetValue(null, null);

            initializeStdOutError.Invoke(null, new object[] { true });
        }

        private static void SetOutAndErrorNull()
        {
            System.Console.SetOut(TextWriter.Null);
            System.Console.SetError(TextWriter.Null);
        }
        

        /// <summary>
        /// Hide the console if not started from command line
        /// </summary>
        /// <returns></returns>
        public static string HideConsole()
        {
            if (HasConsole) // else Console.Title cannot be set
            {
                if (!FromCmd)
                {
                    // Make Title Unique
                    System.Console.Title = Guid.NewGuid().ToString("n") + '_' + System.Console.Title;

                    //Hide WiIndow
                    IntPtr hWnd = FindWindow(null, System.Console.Title);
                    if (hWnd != IntPtr.Zero)
                        ShowWindow(hWnd, 0); // 0 = SW_HIDE
                }
            }
            return null;
        }

        /// <summary>
        /// Show back the hidden console HideConsole()
        /// </summary>
        /// <param name="id">id returned from HideConsole()</param>
        /// <returns></returns>
        public static void ShowConsole(string id)
        {
            if (HasConsole) // else Console.Title cannot be set
            {
                if (id != null)
                {
                    //Sometimes System.Windows.Forms.Application.ExecutablePath works for the caption depending on the system you are running under.
                    IntPtr hWnd = FindWindow(null, id);
                    if (hWnd != IntPtr.Zero)
                    {
                        // Update Title
                        var pos = System.Console.Title.IndexOf("_");
                        if (pos > -1)
                            System.Console.Title = System.Console.Title.Substring(pos + 1);

                        //Hide the window
                        ShowWindow(hWnd, 1); //1 = SW_SHOWNORMAL
                    }
                }
            }
        }

        /*
        private static void AttachToCmd()
        {
            //Get a pointer to the forground window.  The idea here is that
            //IF the user is starting our application from an existing console
            //shell, that shell will be the uppermost window.  We'll get it
            //and attach to it
            var  ptr = GetForegroundWindow();

            int u;
            GetWindowThreadProcessId(ptr, out u);

            Process process = Process.GetProcessById(u);
            if (process.ProcessName == "cmd")    //Is the uppermost window a cmd process?
            {
                AttachConsole(process.Id);
                InvalidateOutAndError();
                return;
            }
        }

        private static void CreateConsole()
        {
            AllocConsole();
            InvalidateOutAndError();
        }

        private static void FreeConsole()
        {
            FreeConsole();
        }*/               
        
        public static bool FromCmd 
        {
            get
            {
                // Get Process of Foreground window
                int u;
                GetWindowThreadProcessId(GetForegroundWindow(), out u);

                // Is Command window
                var process = Process.GetProcessById(u);
                return process.ProcessName.ToUpperInvariant() == "CMD";
            }
        }

        public static CreatedConsole CreateConsole()
        {
            return new CreatedConsole();
        }

        public class CreatedConsole: IDisposable
        {

            public CreatedConsole()
            {
                if (!HasConsole)
                {
                    AllocConsole();
                    InvalidateOutAndError();
                }
            }

            public void Close()
            {
                if (HasConsole)
                {
                    SetOutAndErrorNull();                    
                    FreeConsole();
                }
            }

            public void Dispose()
            {
                Close();
            }            
        }
    }
}

