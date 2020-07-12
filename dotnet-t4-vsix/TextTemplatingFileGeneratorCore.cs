// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace Mono.TextTemplating
{
    [Guid(GENERATOR_GUID)]
    public sealed class TextTemplatingFileGeneratorCore : BaseTemplatedCodeGenerator
    {
        public const string GENERATOR_GUID = "85B769DE-38F5-2020-91AE-D0DFA431FE30";
        public const string GENERATOR_NAME = "dotnet-t4";
        public const string GENERATOR_DESCRIPTION = "Generate files from T4 templates using the .NET Core 3.1 runtime.";

		const string ERROR_OUTPUT = "ErrorGeneratingOutput";
		string extension = ".cs";

		public override string GetDefaultExtension ()
		{
			return extension ?? ".cs";
		}

		protected override byte[] GenerateCode (string inputFileName, string inputFileContent)
		{
			ThreadHelper.ThrowIfNotOnUIThread ();

			try {
				DetectExtensionDirective (inputFileContent);
				var outputFile = CreateTempTextFile (GetDefaultExtension ());

				if (!RunT4Execute (inputFileName, outputFile, out var errors)) {
					GenerateErrors (errors);
					return null;
				}

				var output = File.ReadAllBytes (outputFile);
				File.Delete (outputFile);

				if (output == null) {
					return Encoding.UTF8.GetBytes(ERROR_OUTPUT);
				}

				return output;
			}
			catch (Exception ex) {
				GenerateError (false, $"Something went wrong processing the template '{inputFileName}': {ex}");
				return Encoding.UTF8.GetBytes (ERROR_OUTPUT);
			}
		}

		#region Private Methods

		void DetectExtensionDirective (string inputFileContent)
		{
			Match m = Regex.Match (inputFileContent,
			   @"<#@\s*output(?:\s+encoding=""[.a-z0-9- ]*"")?(?:\s+extension=""([.a-z0-9- ]*)"")?(?:\s+encoding=""[.a-z0-9- ]*"")?\s*#>",
			   RegexOptions.IgnoreCase);

			if (m.Success && m.Groups[1].Success) {
				extension = m.Groups[1].Value;

				if (extension != "" && !extension.StartsWith (".")) {
					extension = "." + extension;
				}
			}
		}

		string CreateTempTextFile (string extension)
		{
			Exception ex = null;
			try {
				var tempDir = Path.GetTempPath ();
				Directory.CreateDirectory (tempDir);

				var path = Path.Combine (tempDir, $"tmp{Guid.NewGuid ():N}{extension}");
				return path;
			}
			catch (Exception e) {
				ex = e;
			}
			throw new Exception ("Failed to create temp file", ex);
		}

		bool RunT4Execute (string inputFileName, string outputFile, out TemplateError[] errors)
		{
			var executePath = Path.GetFullPath (Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location),
				@"T4Exe\t4.exe"));

			var directory = Path.GetDirectoryName (Path.GetFullPath (inputFileName));

			var info = new ProcessStartInfo {
				FileName = executePath,
				Arguments = EscapeArguments (new[] { "-o", outputFile, "-I", directory, inputFileName }),
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true,
				WindowStyle = ProcessWindowStyle.Hidden,
			};

			var p = System.Diagnostics.Process.Start (info);
			p.WaitForExit (60000);

			if (!p.HasExited) {
				p.Kill ();
				throw new TimeoutException ("The t4 process did not respond within 60 seconds. Aborting operation.");
			}

			if (p.ExitCode == 0) {
				errors = ProcessTemplateErrors (p).ToArray ();
				return true;
			} else if (p.ExitCode == 1) {
				errors = ProcessTemplateErrors (p).ToArray ();
				return false;
			} else {
				string error = p.StandardError.ReadToEnd ();
				errors = new[] { new TemplateError (false, $"Something went wrong executing the template in .NET Core: {error}") };
				return false;
			}
		}

		string EscapeArguments (IEnumerable<string> args)
		{
			StringBuilder arguments = new StringBuilder ();

			foreach (string arg in args) {
				if (arguments.Length > 0) {
					arguments.Append (" ");
				}

				arguments.Append ($"\"{arg.Replace ("\\", "\\\\").Replace ("\"", "\\\"")}\"");
			}

			return arguments.ToString ();
		}

		IEnumerable<TemplateError> ProcessTemplateErrors (System.Diagnostics.Process process)
		{
			var stdError = process.StandardError;

			while (!stdError.EndOfStream) {
				bool warning = stdError.ReadLine () == "1";
				int line = int.Parse (stdError.ReadLine ());
				int column = int.Parse (stdError.ReadLine ());
				int messageLength = int.Parse (stdError.ReadLine ());
				char[] messageBuffer = new char[messageLength];
				int readLength = stdError.ReadBlock (messageBuffer, 0, messageLength);
				string message = new string (messageBuffer, 0, readLength);
				stdError.ReadLine ();

				yield return new TemplateError (warning, message, line, column);
			}
		}

		void GenerateErrors (IEnumerable<TemplateError> errors)
		{
			foreach (TemplateError error in errors) {
				GenerateError (error);
			}
		}

		void GenerateError (TemplateError error)
		{
			GenerateError (error.Warning, error.Message, error.Line, error.Column);
		}

		void GenerateError (bool warning, string message, int line = 1, int column = 1)
		{
			GeneratorErrorCallback (warning, 0, message, line + 1, column + 1);
		} 
		#endregion
	}
}
