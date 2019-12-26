using System;
using System.Linq;

namespace CodeChallenge.Target
{
    class Program
    {
        static void Main(string[] args)
        {
            // Protocol:
            // => "#BEGIN\r\n"
            // <= "#READY\r\n"
            // => "ABCdef\r\n"
            // <= "abcDEF\r\n"
            // (...)
            // => "#END 0\r\n"

            // Output windows newlines
            Console.Out.NewLine = "\r\n";
            string line;

            while ((line = ReadLineTee()) == "#BEGIN")
            {
                Console.WriteLine("#READY");

                while (!(line = ReadLineTee()).StartsWith("#END"))
                {
                    Console.WriteLine(InverseCase(line));
                }
            }
        }

        public static string ReadLineTee()
        {
            var line = Console.In.ReadLine();
            //Console.Error.WriteLine(line);
            return line;
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
