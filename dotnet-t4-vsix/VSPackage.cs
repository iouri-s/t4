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
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Task = System.Threading.Tasks.Task;

namespace Mono.TextTemplating
{
    [Guid(PACKAGE_GUID)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(PACKAGE_NAME, PACKAGE_DESCRIPTION, PACKAGE_VERSION)]
    [ProvideCodeGenerator(typeof(TextTemplatingFileGeneratorCore), TextTemplatingFileGeneratorCore.GENERATOR_NAME, TextTemplatingFileGeneratorCore.GENERATOR_DESCRIPTION, true)]
    public sealed class VSPackage : AsyncPackage
    {
        public const string PACKAGE_GUID = "68C949A0-7E31-2020-82A1-DBAEFCD2AE62";
        public const string PACKAGE_NAME = "Text Templating File Generator .NET Core APP 3.1";
        public const string PACKAGE_DESCRIPTION = TextTemplatingFileGeneratorCore.GENERATOR_DESCRIPTION;
        public const string PACKAGE_VERSION = "1.0.1";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}
