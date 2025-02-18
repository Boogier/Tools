using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CreateProcesses
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: CreateProcesses <number-of-processes>");
                return;
            }

            var pid = Process.GetCurrentProcess().Id;
            Console.WriteLine($"Process ID: {pid}");

            using (var m = new EventWaitHandle(false, EventResetMode.ManualReset, "Global\\SimpleApp0", out var created))
            {
                if (created)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var count = args.Length > 0 ? Convert.ToInt32(args[0]) : 10;
                    var tasks = StartApps(count).ToList();

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    MessageBox.Show($"Created {count} processes.\nTime taken: {sw.ElapsedMilliseconds} ms");

                    m.Set();
                }
                else
                {
                    using (var h = new EventWaitHandle(false, EventResetMode.ManualReset, $"Global\\SimpleApp{pid}", out var created1))
                    {
                        h.Set();
                        //new Form().Show();
                        m.WaitOne();
                    }
                }
            }
        }

        private static IEnumerable<Task> StartApps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Task.Run(async () =>
                {
                    var p = Process.Start(Assembly.GetExecutingAssembly().Location);
                    using (var h = new EventWaitHandle(false, EventResetMode.ManualReset, $"Global\\SimpleApp{p.Id}", out var created))
                    {
                        var tcs = new TaskCompletionSource<int>();
                        ThreadPool.RegisterWaitForSingleObject(h, (state, @out) => tcs.SetResult(0), null, -1, true);
                        await tcs.Task.ConfigureAwait(false);
                    }
                });
            }
        }
    }
}