using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using kasthack.vksharp;

namespace PullVK
{
    internal class Program
    {
        private static readonly RawApi Api = new RawApi();

        private static void Main()
        {
            Run();
            Console.ReadLine();
        }

        private static async void Run()
        {
            await LoadUsers(1, 10000001, 100, @"D:\vk\wall", 1000, true, LogUsers, UpdateTraffic);
        }

        private static void LogUsers(long a)
        {
            Console.WriteLine("Loaded {0} users", a.ToString(CultureInfo.InvariantCulture));
        }

        private static void UpdateTraffic(long a)
        {
            var postfix = new[] {'B', 'K', 'M', 'G', 'T'};
            var index = 0;
            while (a > 1024)
            {
                index++;
                a >>= 10;
            }
            Console.WriteLine("{0} {1}", a.ToString(CultureInfo.InvariantCulture), postfix[index]);

        }

        private static async Task SaveFile(string outfile, string resp, bool gzip)
        {
            using (var f = File.OpenWrite(outfile))
            {
                using (Stream s = gzip ? (Stream) new GZipStream(f, CompressionMode.Compress) : f)
                {
                    using (var sw = new StreamWriter(s))
                    {
                        await sw.WriteAsync(resp);
                        await sw.FlushAsync();
                    }
                }
            }
        }

        public static async Task LoadUsers(
            int start,
            int end,
            int threads,
            string downloadDir,
            int volumeSize = 1000,
            bool gzip = true,
            Action<long> showCount = null,
            Action<long> showTraffic = null,
            Func<bool> cancellationToken = null)
        {

            long trafficUsed = 0, usersLoaded = 0;
            var current = start;

            var semaphore = new SemaphoreSlim(threads);
            int activeThreads = 0;

            Func<int, Task> getChunk = async i =>
            {
                try
                {
                    var users = Enumerable.Range(i, Math.Min(volumeSize, end - i)).ToArray();
                    if (cancellationToken?.Invoke() ?? false)
                        return;
                    var outfile = GetChunkPath(downloadDir, users, gzip);
                    if (!File.Exists(outfile))
                    {
                        var resp = await Loaders.GetWallContent(users, Api);
                        Console.WriteLine(users.Max());
                        await SaveFile(outfile, resp, gzip);
                        trafficUsed += Encoding.UTF8.GetByteCount(resp);
                        usersLoaded += volumeSize;
                        showTraffic?.Invoke(trafficUsed);
                        showCount?.Invoke(usersLoaded);
                    }
                }
                catch
                {
                }
                finally
                {
                    --activeThreads;
                    semaphore.Release();
                }
            };

            while (current < end)
            {
                if (cancellationToken?.Invoke() ?? false) break;
                await semaphore.WaitAsync();
                Console.WriteLine("Threads: {0}", ++activeThreads);
                var tsk = getChunk(current);
                if (tsk.Status != TaskStatus.RanToCompletion)
                    await Task.Delay(400);
                current += volumeSize;
            }
        }

        private static string GetChunkPath(string downloadDir, int[] users, bool gzip)
        {
            var filename = $"{users.First()}_{users.Length}.json{(gzip ? ".gz" : "")}";
            downloadDir = Path.Combine(downloadDir, (users.First()/1000000).ToString());
            if (!Directory.Exists(downloadDir)) Directory.CreateDirectory(downloadDir);
            return Path.Combine(downloadDir, filename);
        }
    }
}
