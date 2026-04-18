using TPU_Assembly_Inspection_Paddle;
using System.Runtime.InteropServices;
namespace TPU_Assembly_Inspection
{
    internal static class Program
    {
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "TPU_Assembly_Inspection_SingleInstance";

            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Ứng dụng đã được mở rồi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MAINFORM());
        }
    }
}