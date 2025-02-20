﻿using System;
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
            var pid = Process.GetCurrentProcess().Id;

            using (var m = new EventWaitHandle(false, EventResetMode.ManualReset, "Global\\SimpleApp0", out var created))
            {
                if (created)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var count = args.Length > 0 && int.TryParse(args[0], out var n) ? n : 5;
                    var tasks = StartApps(count, args).ToList();

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    MessageBox.Show($"Created {count} processes.\nTime taken: {sw.ElapsedMilliseconds} ms\n\nUsage: CreateProcesses [number-of-processes] [form]\nnumber-of-processes - number of processes to create\nform - create processes with form");

                    m.Set();
                }
                else
                {
                    using (var h = new EventWaitHandle(false, EventResetMode.ManualReset, $"Global\\SimpleApp{pid}", out var created1))
                    {
                        h.Set();

                        if (args.Any(a => a.Equals("form", StringComparison.OrdinalIgnoreCase)))
                        {
                            var form = new Form();
                            new Thread(() =>
                            {
                                m.WaitOne();
                                form.Close();
                            }).Start();
                            Application.Run(form);
                        }
                        else
                        {
                            m.WaitOne();
                        }
                    }
                }
            }
        }

        private static IEnumerable<Task> StartApps(int count, string[] args)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Task.Run(async () =>
                {
                    var p = Process.Start(Assembly.GetExecutingAssembly().Location, string.Join(" ", args));
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