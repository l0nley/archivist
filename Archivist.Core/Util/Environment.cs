using Archivist.Core.Operations;
using System;

namespace Archivist.Core.Util
{
    public class Environment
    {
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
        public void ReportStatus(Guid id, OperationStatus status, string errorMessage = null)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Status {id.ToString("N").Substring(0, 6)} {status.ToString()} {errorMessage}");
        }

        public void ReportProgress(Guid id, int current, int total)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} Progress {id.ToString("N").Substring(0, 6)}  {current}/{total}");
        }

        public void WriteOut(string v)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine(v);
            Console.ResetColor();
        }

        public ConsoleKeyInfo GetKey()
        {
            return Console.ReadKey();
        }
    }
}
