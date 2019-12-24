using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CodeChallenge.Optimized
{
    class Program
    {
        static readonly long BEGIN = FormatLong("#BEGIN");
        static readonly long READY = FormatLong("#READY");

        static long FormatLong(string value)
        {
            return BitConverter.ToInt64(Encoding.ASCII.GetBytes(value + "\r\n"));
        }

        static void Main(string[] args)
        {
            using var input = Console.OpenStandardInput();
            using var output = Console.OpenStandardOutput();
            using var streamReader = new BinaryReader(input, Encoding.ASCII);
            using var streamWriter = new BinaryWriter(output, Encoding.ASCII);
            var caseFlip = new Vector<long>(0x0000_2020_2020_2020);
            var buffer = new Span<byte>(new byte[Vector<byte>.Count]);
            int pass = 0;

            // Protocol:
            // => "#BEGIN\r\n"
            while (streamReader.ReadInt64() == BEGIN)
            {
                var endCommand = new Vector<long>(FormatLong($"#END {pass++}\r\n"));
                // <= "#READY\r\n"
                streamWriter.Write(READY);
                streamWriter.Flush();

                bool hasEnded = false;

                // => "ABCdef\r\n"
                while (!hasEnded)
                {
                    int readCount = streamReader.Read(buffer);

                    var words = Vector.AsVectorInt64(new Vector<byte>(buffer));
                    var commands = Vector.Equals(words, endCommand);

                    if (!Vector.EqualsAll(commands, Vector<long>.Zero))
                    {
                        for (int i = 0; i < readCount; i++)
                        {
                            if (commands[i] != 0)
                            {
                                readCount = i * 8;
                                hasEnded = true;
                                break;
                            }
                        }
                    }

                    var invertedWords = Vector.Xor(words, caseFlip);
                    invertedWords.CopyTo(buffer);
                    streamWriter.Write(buffer.Slice(0, readCount));
                }

                streamWriter.Flush();
            }
        }
    }
}
