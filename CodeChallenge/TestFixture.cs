using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeChallenge
{
    class TestFixture
    {
        private Random random = new Random();
        const int wordCount = 0x100000;
        private byte[] wordData = new byte[wordCount * 8];
        private byte[] resultCache = new byte[wordCount * 8];
        private long[] resultData = new long[wordCount];
        private readonly object writeSync = new object();
        private string applicationPath;

        public TestFixture(string applicationPath)
        {
            this.applicationPath = applicationPath;
        }

        private void WriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (writeSync)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private void WriteDebug(string message) => WriteLine($" => {message}", ConsoleColor.DarkGreen);
        private void ReadDebug(string message) => WriteLine($" <= {message}", ConsoleColor.Green);

        private string RandomWord()
        {
            var builder = new StringBuilder("      \r\n");

            for (int i = 0; i < 6; i++)
            {
                builder[i] = (char)(random.Next(65, 91) | (random.NextDouble() > 0.5 ? 32 : 0));
            }
            return builder.ToString();
        }

        public int Run(int passes)
        {
            var baselinePath = DiscoverBaselineApplicationPath();
            Console.WriteLine("Running baseline");
            var baseline = Run(baselinePath, passes);

            Console.WriteLine("Running target");
            var target = Run(this.applicationPath, passes);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Final result: {baseline / target:P}");
            Console.ResetColor();
            return 0;
        }

        private string DiscoverBaselineApplicationPath()
        {
            string applicationName = (Environment.OSVersion.Platform == PlatformID.Win32NT) ? "CodeChallenge.Baseline.exe" : "CodeChallenge.Baseline";
            var applicationPath = Directory.EnumerateFiles(".", applicationName, SearchOption.AllDirectories).FirstOrDefault();

            if (applicationPath == null)
            {
#if DEBUG
                var configuration = "debug";
#else
                var configuration = "release";
#endif

                // When running from an IDE, try harder to find the application
                applicationPath = Directory.EnumerateFiles(Path.GetFullPath("../../../.."), applicationName, SearchOption.AllDirectories).Where(o => o.Contains(configuration, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (applicationPath == null)
                {
                    throw new FileNotFoundException("Couldn't find baseline application");
                }
            }

            Console.WriteLine($"Baseline application: {applicationPath}");
            return applicationPath;
        }

        private double Run(string applicationPath, int passes)
        {
            var startInfo = new ProcessStartInfo(applicationPath);
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardInputEncoding = Encoding.ASCII;
            startInfo.StandardOutputEncoding = Encoding.ASCII;
            startInfo.StandardErrorEncoding = Encoding.ASCII;
            TimeSpan[] benchmarks = new TimeSpan[passes];

            using (var process = Process.Start(startInfo))
            {
                process.ErrorDataReceived += OnErrorDataReceived;
                process.BeginErrorReadLine();

                var writer = process.StandardInput;
                writer.NewLine = "\r\n";
                var reader = process.StandardOutput;

                LoadRandomWords();

                for (int pass = 0; pass < passes; pass++)
                {
                    benchmarks[pass] = RunPass(writer, reader, pass);
                }

                process.Kill();
            }

            WriteLine("Run results", ConsoleColor.White);
            var fastest = benchmarks.OrderBy(o => o).First();
            var slowest = benchmarks.OrderBy(o => o).Last();

            foreach (var result in benchmarks)
            {
                var color = result switch
                {
                    _ when result == slowest => ConsoleColor.Red,
                    _ when result == fastest => ConsoleColor.Green,
                    _ => ConsoleColor.White,
                };

                WriteLine(result.ToString(), color);
            }

            return benchmarks.OrderBy(o => o).Take(passes - 1).Sum(o => o.TotalSeconds);
        }

        private void LoadRandomWords()
        {
            WriteLine("Preparing dataset... ");
            for (int i = 0; i < wordCount; i++)
            {
                var randWord = RandomWord();
                var word = new Span<byte>(wordData, i * 8, 8);
                Encoding.ASCII.GetBytes(randWord).CopyTo(word);
                resultData[i] = BitConverter.ToInt64(Encoding.ASCII.GetBytes(InverseCase(randWord)));
            }
        }

        private TimeSpan RunPass(StreamWriter writer, StreamReader reader, int pass)
        {
            WriteLine($"Running pass {pass}... ");
            writer.WriteLine("#BEGIN");
            var readyResponse = reader.ReadLineAsync();

            using var binaryReader = new BinaryReader(reader.BaseStream, Encoding.ASCII, true);
            using var binaryWriter = new BinaryWriter(writer.BaseStream, Encoding.ASCII, true);

            if (!readyResponse.Wait(TimeSpan.FromSeconds(5)))
            {
                WriteLine("Protocol timeout failure: Did not return '#READY' within 5 seconds of '#BEGIN'");
                throw new TimeoutException("Timed out waiting for #READY");
            }

            if (readyResponse.Result != "#READY")
            {
                WriteLine("Protocol failure: Did not return '#READY'");
                throw new InvalidOperationException("Did not recieve '#READY'");
            }

            WriteLine("Ready recieved.");

            void ReadResults()
            {
                for (int i = 0; i < wordCount; i++)
                {
                    binaryReader.Read(new Span<byte>(resultCache, i << 3, 8));
                }
            }

            var readTask = Task.Run(ReadResults);

            var timer = Stopwatch.StartNew();

            for (int i = 0; i < wordData.Length; i += 0x10000)
            {
                binaryWriter.Write(new Span<byte>(wordData, i, 0x10000));
            }

            //for (int i = 0; i < wordCount; i++)
            //{
            //    binaryWriter.Write(wordData[i]);
            //    binaryWriter.Flush();
            //}

            readTask.Wait();
            timer.Stop();

            WriteLine("Verifying results... ");

            for (int i = 0; i < wordCount; i++)
            {
                var result = BitConverter.ToInt64(new Span<byte>(resultCache, i << 3, 8));

                if (resultData[i] != result)
                {
                    var expected = Encoding.ASCII.GetString(BitConverter.GetBytes(resultData[i])).Trim();
                    var actual = Encoding.ASCII.GetString(BitConverter.GetBytes(result)).Trim();
                    WriteLine($"Result mismatch at {i}. Expected {expected} recieved {actual}.", ConsoleColor.Red);
                }
            }

            writer.WriteLine("#END " + pass);
            return timer.Elapsed;
        }

        public int RunDebug()
        {
            var startInfo = new ProcessStartInfo(applicationPath);
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardInputEncoding = Encoding.ASCII;
            startInfo.StandardOutputEncoding = Encoding.ASCII;
            startInfo.StandardErrorEncoding = Encoding.ASCII;

            using (var process = Process.Start(startInfo))
            {
                process.BeginErrorReadLine();
                process.ErrorDataReceived += OnErrorDataReceived;
                var writer = process.StandardInput;
                writer.NewLine = "\r\n";
                var reader = process.StandardOutput;

                for (int pass = 0; pass < 2; pass++)
                {
                    var result = DebugPass(writer, reader, pass);

                    if (result != 0)
                    {
                        return result;
                    }
                }

                process.Kill();
            }

            return 0;
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            WriteLine("[DEBUG] " + e.Data, ConsoleColor.Magenta);
        }

        private int DebugPass(StreamWriter writer, StreamReader reader, int pass)
        {
            WriteDebug("#BEGIN");
            writer.WriteLine("#BEGIN");
            var readyResponse = reader.ReadLineAsync();
            if (!readyResponse.Wait(TimeSpan.FromSeconds(5)))
            {
                WriteLine("Protocol timeout failure: Did not return '#READY' within 5 seconds of '#BEGIN'", ConsoleColor.Red);
                return -1;
            }

            ReadDebug(readyResponse.Result);
            if (readyResponse.Result != "#READY")
            {
                WriteLine("Protocol failure: Did not return '#READY'", ConsoleColor.Red);
                return -2;
            }

            string[] words = Enumerable.Range(0, 10).Select(o => RandomWord().Trim()).ToArray();

            foreach (string word in words)
            {
                WriteDebug(word);
                writer.WriteLine(word);
            }

            foreach (string word in words)
            {
                var expected = InverseCase(word);
                var result = reader.ReadLine();
                ReadDebug(result);
                if (result != expected)
                {
                    WriteLine($"Response failed: Expected {expected} recieved {result}", ConsoleColor.Red);
                    return -3;
                }
            }

            WriteDebug("#END " + pass);
            writer.WriteLine("#END " + pass);

            return 0;
        }

        private static string InverseCase(string line)
        {
            return new string(line.ToCharArray().Select(InverseCase).ToArray());
        }

        private static char InverseCase(char c)
        {
            if (char.IsUpper(c))
            {
                return char.ToLower(c);
            }
            else if (char.IsLower(c))
            {
                return char.ToUpper(c);
            }
            else
            {
                return c;
            }
        }
    }
}
