# Getting Started Examples
## Solace C#/.NET API

These tutorials will get you up to speed and sending messages with Solace technology as quickly as possible. There are two ways you can get started:

- If your company has Solace message routers deployed, contact your middleware team to obtain the host name or IP address of a Solace message router to test against, a username and password to access it, and a VPN in which you can produce and consume messages.
- If you do not have access to a Solace message router, you will need to go through the “[Set up a VMR](http://docs.solace.com/Solace-VMR-Set-Up/Setting-Up-VMRs.htm)” tutorial to download and install the software.

## Contents

This repository contains code and matching tutorial walk throughs for five different basic Solace messaging patterns. For a nice introduction to the Solace API and associated tutorials, check out the [tutorials home page](https://solacesamples.github.io/solace-samples-dotnet/).

## Checking out and Building

To check out the project and build it, do the following:

  1. clone this GitHub repository
  1. `cd solace-samples-dotnet`
 
### Download the Solace C# API

The C# API library can be [downloaded here]({{ site.links-downloads }}){:target="_top"}. The build instructions below assume you have unpacked the zip file to a known location. 

### Build the Samples

Building these examples is simple. The following provides an example. For ideas on how to build with other IDEs you can consult the README of the C# API library.

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicPublisher.exe  TopicPublisher.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicSubscriber.exe  TopicSubscriber.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` at compile and runtime time and `libsolclient_64.dll` at runtime in the same directory where your source and executables are. 

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution. 

## Running the Samples

To try individual samples, build the project from source and then run samples like the following:

```
$ ./TopicSubscriber HOST

```

See the [tutorials](https://solacesamples.github.io/solace-samples-dotnet/) for more details.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Authors

See the list of [contributors](https://github.com/SolaceSamples/solace-samples-dotnet/contributors) who participated in this project.

## License

This project is licensed under the Apache License, Version 2.0. - See the [LICENSE](LICENSE) file for details.

## Resources

For more information try these resources:

- The Solace Developer Portal website at: http://dev.solace.com
- Get a better understanding of [Solace technology](http://dev.solace.com/tech/).
- Check out the [Solace blog](http://dev.solace.com/blog/) for other interesting discussions around Solace technology
- Ask the [Solace community.](http://dev.solace.com/community/)
