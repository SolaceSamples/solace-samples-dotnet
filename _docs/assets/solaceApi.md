
## Obtaining the Solace API

This tutorial depends on you having the Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) downloaded and installed for your project, and the instructions in this tutorial assume you successfully done it. If your environment differs then adjust the build instructions appropriately.

Here are a few easy ways to get this API.

### Get the API: Using nuget.org

Use the NuGet console or the NuGet Visual Studio Extension to download the [SolaceSystems.Solclient.Messaging](http://nuget.org/packages/SolaceSystems.Solclient.Messaging/) package for your solution and to install it for your project.

The package contains the required libraries and brief API documentation. It will automatically copy correct libraries from the package to the target directory at build time, but of course if you compile your program from the command line you would need to refer to the API assemblies and libraries locations explicitly.

Notice that in this case both x64 and x86 API assemblies and libraries have the same names.

### Get the API: Using the Solace Developer Portal

The SolClient for .NET can be [downloaded here]({{ site.links-downloads }}){:target="_top"}. That distribution is a zip file containing the required libraries, detailed API documentation, and examples.

You would need either to update your Visual Studio project to point to the extracted assemblies and libraries, or to refer to their locations explicitly.

Notice that in this case x64 and x86 API assemblies and libraries have different names, e.g. the x86 API assembly is SolaceSystems.Solclient.Messaging.dll and the x64 API assembly is SolaceSystems.Solclient.Messaging_64.dll.
