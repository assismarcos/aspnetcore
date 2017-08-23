// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    [Collection("Azure")]
    public class TemplateFunctionalTests
    {
        readonly AzureFixture _fixture;

        private readonly ITestOutputHelper _outputHelper;

        public TemplateFunctionalTests(AzureFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData("web", "Hello World!")]
        [InlineData("razor", "Learn how to build ASP.NET apps that can run anywhere.")]
        [InlineData("mvc", "Learn how to build ASP.NET apps that can run anywhere.")]
        public async Task DotnetNewWebRunsInWebApp(string template, string expected)
        {
            var testId = nameof(DotnetNewWebRunsInWebApp) + template;

            using (var logger = GetLogger(testId))
            {
                Assert.NotNull(_fixture.Azure);

                var site = await _fixture.Deploy("Templates\\BasicAppServices.json", baseName: testId);
                var testDirectory = GetTestDirectory(testId);

                var dotnet = DotNet(logger, testDirectory);

                var result = await dotnet.ExecuteAsync("new " + template);
                result.AssertSuccess();

                await site.BuildPublishProfileAsync(testDirectory.FullName);

                result = await dotnet.ExecuteAsync("publish /p:PublishProfile=Profile");
                result.AssertSuccess();

                using (var httpClient = site.CreateClient())
                {
                    var getResult = await httpClient.GetAsync("/");
                    getResult.EnsureSuccessStatusCode();
                    Assert.Contains(expected, await getResult.Content.ReadAsStringAsync());
                }
            }
        }

        private TestLogger GetLogger([CallerMemberName] string callerName = null)
        {
            _fixture.TestLog.StartTestLog(_outputHelper, nameof(TemplateFunctionalTests), out var factory, callerName);
            return new TestLogger(factory, factory.CreateLogger(callerName));
        }

        private TestCommand DotNet(TestLogger logger, DirectoryInfo workingDirectory)
        {
            return new TestCommand("dotnet")
            {
                Logger = logger,
                WorkingDirectory = workingDirectory.FullName
            };
        }

        private DirectoryInfo GetTestDirectory([CallerMemberName] string callerName = null)
        {
            if (Directory.Exists(callerName))
            {
                Directory.Delete(callerName, recursive:true);
            }
            return Directory.CreateDirectory(callerName);
        }
    }
}