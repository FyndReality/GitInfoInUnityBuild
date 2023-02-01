/* MIT License
Copyright (c) 2016 RedBlueGames
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
Code written by Doug Cox
https://gist.github.com/edwardrowe/fdec706fe53bfff0671e063f263ada63/ef2befd4e3f00ecff39d20053f0aec12dccf6d5c
*/

using System;
using UnityEngine;
using System.Text;
using System.Diagnostics;

namespace FyndReality.Util.Git
{
/// <summary>
/// GitException includes the error output from a Git.Run() command as well as the
/// ExitCode it returned.
/// </summary>
public class GitException : InvalidOperationException
{
    public GitException(int exitCode, string errors) : base(errors) =>
        this.ExitCode = exitCode;

    /// <summary>
    /// The exit code returned when running the Git command.
    /// </summary>
    public readonly int ExitCode;
}

public static class GitCommand
{
    /// <summary>
    /// Runs git.exe with the specified arguments and returns the output.
    /// On fail it will return null or if throwGitExceptionOnFail, a GitException
    /// </summary>
    public static string Run(string arguments, bool throwGitExceptionOnFail = false)
    {
        using (var process = new Process())
        {
            var exitCode = Run(process, @"git", arguments, Application.dataPath,
                out var output, out var errors);
            if (exitCode == 0)
                return output;

            var gitException = new GitException(exitCode, errors);
            if (throwGitExceptionOnFail)
                throw gitException;

            UnityEngine.Debug.LogError(
                "Unable to get info from repo, is git available in environment path and initialized for project?"
                + "\nCommand: git " + string.Join(" ", arguments) 
                + "\nError: " + gitException.Message);
            return null;
        }
    }

    /// <summary>
    /// Runs the specified process and waits for it to exit. Its output and errors are
    /// returned as well as the exit code from the process.
    /// See: https://stackoverflow.com/questions/4291912/process-start-how-to-get-the-output
    /// Note that if any deadlocks occur, read the above thread (cubrman's response).
    /// </summary>
    private static int Run(Process process, string application,
        string arguments, string workingDirectory, out string output,
        out string errors)
    {
        process.StartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            FileName = application,
            Arguments = arguments,
            WorkingDirectory = workingDirectory
        };

        // Use the following event to read both output and errors output.
        var outputBuilder = new StringBuilder();
        var errorsBuilder = new StringBuilder();
        process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
        process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

        // Start the process and wait for it to exit.
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        output = outputBuilder.ToString().TrimEnd();
        errors = errorsBuilder.ToString().TrimEnd();
        return process.ExitCode;
    }
}
}