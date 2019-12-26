using Microsoft.Extensions.CommandLineUtils;
using System;

namespace CodeChallenge
{
    class Program
    {
        static int Main(string[] args)
        {
            var application = new CommandLineApplication(throwOnUnexpectedArg: true);
            application.HelpOption("-?|--help");
            application.Command("debug", DebugCommand);
            application.Command("benchmark", BenchmarkCommand);
            return application.Execute(args);
        }

        private static void DebugCommand(CommandLineApplication application)
        {
            application.HelpOption("-?|--help");
            var path = application.Option("-p|--path", "Path to implementation.", CommandOptionType.SingleValue);

            application.OnExecute(() =>
            {
                var fixture = new TestFixture(path.Value());
                return fixture.RunDebug();
            });
        }

        private static void BenchmarkCommand(CommandLineApplication application)
        {
            application.HelpOption("-?|--help");
            var path = application.Option("-p|--path", "Path to implementation.", CommandOptionType.SingleValue);
            var iterations = application.Option("-i|--iterations", "Number of test passes to run.", CommandOptionType.SingleValue);

            application.OnExecute(() =>
            {
                var fixture = new TestFixture(path.Value());
                return fixture.Run(iterations.HasValue() ? int.Parse(iterations.Value()) : 10);
            });
        }
    }
}
