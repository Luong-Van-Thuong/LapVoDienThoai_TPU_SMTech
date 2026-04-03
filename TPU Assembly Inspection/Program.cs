using TPU_Assembly_Inspection_Paddle;
using System.Runtime.InteropServices;
namespace TPU_Assembly_Inspection
{
    internal static class Program
    {

        [STAThread]

        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MAINFORM());
        }
    }
}