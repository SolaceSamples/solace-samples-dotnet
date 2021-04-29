# Getting Started Examples
## Solace C#/.NET API

The "Getting Started" tutorials will get you up to speed and sending messages with Solace technology as quickly as possible. There are three ways you can get started:

- Follow [these instructions](https://cloud.solace.com/learn/group_getting_started/ggs_signup.html) to quickly spin up a cloud-based Solace messaging service for your applications.
- Follow [these instructions](https://docs.solace.com/Solace-SW-Broker-Set-Up/Setting-Up-SW-Brokers.htm) to start the Solace VMR in leading Clouds, Container Platforms or Hypervisors. The tutorials outline where to download and how to install the Solace VMR.
- If your company has Solace message routers deployed, contact your middleware team to obtain the host name or IP address of a Solace message router to test against, a username and password to access it, and a VPN in which you can produce and consume messages.

## Contents

This repository contains code and matching tutorial walk throughs for five different basic Solace messaging patterns. For a nice introduction to the Solace API and associated tutorials, check out the [tutorials home page](https://dev.solace.com/samples/solace-samples-dotnet/).

## Obtaining the Solace API

This tutorial depends on you having the Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) downloaded and installed for your project, and the instructions in this tutorial assume you successfully done it. If your environment differs then adjust the build instructions appropriately.

Here are a few easy ways to get this API.

### Get the API: Using nuget.org

Use the NuGet console or the NuGet Visual Studio Extension to download the [SolaceSystems.Solclient.Messaging](http://nuget.org/packages/SolaceSystems.Solclient.Messaging/) package for your solution and to install it for your project.

The package contains the required libraries and brief API documentation. It will automatically copy correct libraries from the package to the target directory at build time, but of course if you compile your program from the command line you would need to refer to the API assemblies and libraries locations explicitly.

Notice that in this case both x64 and x86 API assemblies and libraries have the same names.

### Get the API: Using the Solace Developer Portal

The SolClient for .NET can be [downloaded here](https://solace.com/downloads/). That distribution is a zip file containing the required libraries, detailed API documentation, and examples.

You would need either to update your Visual Studio project to point to the extracted assemblies and libraries, or to refer to their locations explicitly.

Notice that in this case x64 and x86 API assemblies and libraries have different names, e.g. the x86 API assembly is SolaceSystems.Solclient.Messaging.dll and the x64 API assembly is SolaceSystems.Solclient.Messaging_64.dll.

## Checking out and Building

To check out the project and build it, do the following:

  1. clone this GitHub repository
  1. `cd solace-samples-dotnet`

### Build the Samples

Building these examples is simple. The following provides an example. For ideas on how to build with other IDEs you can consult the README of the C# API library.

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicPublisher.exe  TopicPublisher.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicSubscriber.exe  TopicSubscriber.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `bin\Win64` (or `bin\Win32`) directory of that distribution.

## Running the Samples

To try individual samples, build the project from source and then run samples like the following:

```
$ ./TopicSubscriber HOST

```

See the [tutorials](https://dev.solace.com/samples/solace-samples-dotnet/) for more details.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Authors

See the list of [contributors](https://github.com/SolaceSamples/solace-samples-dotnet/contributors) who participated in this project.

## License

This project is licensed under the Apache License, Version 2.0. - See the [LICENSE](LICENSE) file for details.

## Resources

For more information try these resources:

- [Tutorials](https://tutorials.solace.dev/)
- The Solace Developer Portal website at: http://dev.solace.com
- Get a better understanding of [Solace technology](https://solace.com/products/tech/).
- Check out the [Solace blog](http://dev.solace.com/blog/) for other interesting discussions around Solace technology
- Ask the [Solace community.](https://solace.community)
