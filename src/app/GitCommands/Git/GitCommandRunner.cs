using System.Text;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI;

namespace GitCommands
{
    public sealed class GitCommandRunner : IGitCommandRunner
    {
        private readonly IExecutable _gitExecutable;
        private readonly Func<Encoding> _defaultEncoding;

        public GitCommandRunner(IExecutable gitExecutable, Func<Encoding> defaultEncoding)
        {
            _gitExecutable = gitExecutable;
            _defaultEncoding = defaultEncoding;
        }

        public IProcess RunDetached(
            CancellationToken cancellationToken,
            ArgumentString arguments = default,
            bool createWindow = false,
            bool redirectInput = false,
            bool redirectOutput = false,
            Encoding? outputEncoding = null,
            bool throwOnErrorExit = true)
        {
            if (outputEncoding is null && redirectOutput)
            {
                outputEncoding = _defaultEncoding();
            }

            return _gitExecutable.Start(arguments, createWindow, redirectInput, redirectOutput, outputEncoding, useShellExecute: false, throwOnErrorExit, cancellationToken);
        }

        public void RunDetached(
            ArgumentString arguments = default,
            bool createWindow = false,
            bool redirectInput = false,
            bool redirectOutput = false,
            Encoding? outputEncoding = null)
        {
            ThreadHelper.FileAndForget(async () =>
                {
                    using IProcess process = RunDetached(CancellationToken.None, arguments, createWindow, redirectInput, redirectOutput, outputEncoding);
                    await process.WaitForExitAsync();
                });
        }
    }
}
