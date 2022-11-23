# Getting Started Examples
## Solace C#/.NET API

The "Getting Started" tutorials will get you up to speed and sending messages with Solace technology as quickly as possible. There are three ways you can get started:

- Follow [these instructions](https://cloud.solace.com/learn/group_getting_started/ggs_signup.html) to quickly spin up a cloud-based Solace messaging service for your applications.
- Follow [these instructions](https://docs.solace.com/Solace-SW-Broker-Set-Up/Setting-Up-SW-Brokers.htm) to start the Solace VMR in leading Clouds, Container Platforms or Hypervisors. The tutorials outline where to download and how to install the Solace VMR.
- If your company has Solace message routers deployed, contact your middleware team to obtain the host name or IP address of a Solace message router to test against, a username and password to access it, and a VPN in which you can produce and consume messages.

## Contents

This repository contains code and matching tutorial walk throughs for five different basic Solace messaging patterns. For a nice introduction to the Solace API and associated tutorials, check out the [tutorials home page](https://dev.solace.com/samples/solace-samples-dotnet/).

## Obtaining the Solace API

The Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) is available as a [Nuget package](https://www.nuget.org/packages/SolaceSystems.Solclient.Messaging) and is included as a package reference in each sample project in the repository. If you are building the projects using Visual Studio, the library will be installed automatically when you load the Solution. If using Visual Studio code, run `dotnet restore` from the `src` directory to download the API.

## Building and Running the Project

To check out the project and build it, do the following:

**Visual Studio**
  1. Clone this GitHub repository
  1. Open the Solution `src/SolaceExamples.sln`
  1. Designate a startup project (or projects)
  1. Start debugging by pressing F5
  
**VS Code**
  1. Clone this GitHub repository
  1. `cd solace-samples-dotnet/src`
  1. `dotnet restore`
  1. `dotnet build`
  1. `dotnet run --project <ProjectName> -- [args]`

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
