using LC3.Emulation.Core;
using System.Diagnostics;

namespace LC3.Emulation.TestSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proc = new ProcessorUnprotected();
            //proc.LoadImage("C:\\Users\\nyahstic\\Documents\\LittleComputer-3\\AssemblyTest.obj");
            proc.OnDebugInfo += (info) => Console.WriteLine(info);
            proc.Running = true;
            while (true)
            {
                if (!proc.Running) break;
                try
                {
                    //Console.Title = $"LC3 Emulator - PC: {proc.RegisterFile[(int)ProcessorUnprotected.Register.R_PC]:X4} ({proc.MemoryRead(proc.RegisterFile[(int)ProcessorUnprotected.Register.R_PC]):X4})";
                    proc.Step();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception: {ex}");
                    break;
                }
            }
            //proc.Run();
            Console.WriteLine("Execution finished. Press any key to exit. . .");
            Console.Read();
        }
    }
}
