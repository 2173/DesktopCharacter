using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DesktopCharacter.Launcher
{
    public class AppLauncher
    {
        [DllImport("dllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mLoadLibrary(IntPtr lpLibData, int libDataSize);
        [DllImport("dllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool mFreeLibrary(IntPtr hModule);
        [DllImport("dllLoader.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mGetProcAddress(IntPtr hModule, string lpProcName);

        [STAThreadAttribute]
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            App.Main();
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
