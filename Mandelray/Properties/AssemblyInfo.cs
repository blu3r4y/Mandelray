using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion("1.0.0")]

[assembly: AssemblyTitle("Mandelray")]
[assembly: AssemblyDescription("A Mandelbrot Set Viewer in C#")]

[assembly: AssemblyCompany("Mario Kahlhofer")]
[assembly: AssemblyProduct("Mandelray")]
[assembly: AssemblyCopyright("Copyright © 2018 Mario Kahlhofer - blu3r4y")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: Guid("8f9e7ae8-ae4c-495d-ac51-c5106fcbbd0e")]
[assembly: ComVisible(false)]