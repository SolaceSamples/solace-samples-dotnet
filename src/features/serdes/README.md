This folder contains sample .NET applications that demonstrate how to use the Solace Schema Registry SERDES provider for Solace PubSub+ messaging when connecting to a Schema Registry (by default running on `localhost`).

The samples are organized into two groups: **generic** (string serialization without a Schema Registry) and **JsonSchema** (JSON Schema serialization with Schema Registry validation).

## Contents

### Generic Samples

Located in `generic/`.

| Project | Description |
| --- | --- |
| `HelloWorldSolaceDotnetStringSerde` | Publishes two "Hello World" string messages to a topic and subscribes to receive and deserialize them. Demonstrates the basic `StringSerializer` and `StringDeserializer` without a Schema Registry. |

### JSON Schema Samples

Located in `JsonSchema/`. All JSON Schema samples require a running Solace Schema Registry.

| Project | Description |
| --- | --- |
| `HelloWorldSolaceDotNetJsonSchemaSerde` | Publishes a `ClockInOut` event as a validated `JsonNode` and subscribes to receive and deserialize it. Demonstrates the basic serialize/deserialize round-trip with Schema Registry validation. |
| `JsonSchemaSerializeProducer` | Continuously publishes `User` objects serialized and validated against the `user.json` schema. Runs until Enter is pressed. |
| `JsonSchemaDeserializeConsumerToPoco` | Subscribes to `solace/samples/json` and deserializes incoming messages directly to `User` POCO instances using the `customDotnetType` property in the schema. |
| `JsonSchemaSerdesRequestor` | Sends `CreateUser` request messages serialized with JSON Schema and waits for a `CreateUserResponse` reply. Pairs with `JsonSchemaSerdesReplier`. |
| `JsonSchemaSerdesReplier` | Receives `CreateUser` requests, deserializes them to `CreateUser` POCOs, generates a unique user ID, and replies with a serialized `CreateUserResponse`. Pairs with `JsonSchemaSerdesRequestor`. |

### Shared Resources

The `Resources` project at `Resources/` is a shared class library containing:

- `JsonSchema/User.cs`, `CreateUser.cs`, `CreateUserResponse.cs`, `ClockInOut.cs`: Plain .NET model classes used by the JSON Schema samples.
- `JsonSchema/Schemas/user.json`, `create-user.json`, `create-user-response.json`, `clock-in-out.json`: The JSON Schema definitions to be uploaded to the Schema Registry.

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) (the projects also multi-target `net462` for Windows-only builds)
- A running Solace PubSub+ broker
- A running [Solace Schema Registry](https://docs.solace.com/Schema-Registry/schema-registry-overview.htm) (required for JSON Schema samples only)

## Solace Schema Registry

For information about how to deploy and configure the Solace Schema Registry, please refer to the documentation:
https://docs.solace.com/Schema-Registry/schema-registry-overview.htm

## Upload a Schema

Before running any JSON Schema sample, the schema files from `Resources/JsonSchema/Schemas/` must be uploaded to the Schema Registry. To upload each schema:

1. Log into the Schema Registry with an account that has write access and click the "Create Artifact" button.
2. Leave the Group Id field empty.

    ### JSON Schema
    - **Artifact Id** (one per schema, each uploaded separately):
        - For `user.json`, use `solace/samples/json`
        - For `create-user.json`, use `solace/samples/create-user/json`
        - For `create-user-response.json`, use `solace/samples/create-user-response/json`
        - For `clock-in-out.json`, use `solace/samples/clock-in-out/json`
    - **Type**: Select `JSON Schema`.

> [!NOTE]
> Each schema must be uploaded separately with its own unique Artifact Id to avoid conflicts.

3. Click "Next" to proceed.
4. Skip the Artifact Metadata section and click "Next".
5. On the Version Content Page, leave the version set to auto (or enter a specific value).
6. Upload the matching schema file from `Resources/JsonSchema/Schemas/`:
    - For Artifact Id `solace/samples/json`, upload `user.json`
    - For Artifact Id `solace/samples/create-user/json`, upload `create-user.json`
    - For Artifact Id `solace/samples/create-user-response/json`, upload `create-user-response.json`
    - For Artifact Id `solace/samples/clock-in-out/json`, upload `clock-in-out.json`
7. Click "Next", skip Version Metadata, then click "Create".

## Building the Samples

Build an individual sample from its project directory:

```shell
dotnet build JsonSchema/JsonSchemaSerializeProducer/JsonSchemaSerializeProducer.csproj
```

Or build the Resources library first if building manually:

```shell
dotnet build Resources/Resources.csproj
```

## Running the Samples

### Generic: Hello World String Serde

Publishes two string messages and receives them on the same topic.

```shell
cd generic/HelloWorldSolaceDotnetStringSerde
dotnet run -- <host:port> <message-vpn> <client-username> [password]
# Example:
dotnet run -- localhost:55555 default default
```

### JSON Schema: Hello World

Publishes a single `ClockInOut` `JsonNode` and receives it on the same topic.

```shell
cd JsonSchema/HelloWorldSolaceDotNetJsonSchemaSerde
dotnet run -- <host:port> <username>@<vpnname> <password>
# Example:
dotnet run -- localhost:55555 default@default default
```

### JSON Schema: Producer

Continuously publishes `User` messages to `solace/samples/json` until Enter is pressed.

```shell
cd JsonSchema/JsonSchemaSerializeProducer
dotnet run -- <host:port> <username>@<vpnname> <password>
# Example:
dotnet run -- localhost:55555 default@default default
```

### JSON Schema: Consumer

Subscribes to `solace/samples/json` and deserializes received messages to `User` POCOs.

```shell
cd JsonSchema/JsonSchemaDeserializeConsumerToPoco
dotnet run -- <host:port> <username>@<vpnname> <password>
# Example:
dotnet run -- localhost:55555 default@default default
```

### JSON Schema: Request/Reply

Run the replier first, then the requestor. The requestor sends `CreateUser` requests and prints the `CreateUserResponse` containing the generated user ID.

```shell
# Terminal 1 - start the replier
cd JsonSchema/JsonSchemaSerdesReplier
dotnet run -- <host:port> <username>@<vpnname> <password>

# Terminal 2 - start the requestor
cd JsonSchema/JsonSchemaSerdesRequestor
dotnet run -- <host:port> <username>@<vpnname> <password>
```

## Environment Variables

The Schema Registry connection can be customized by setting environment variables before launching a JSON Schema sample. If unset, the defaults below are used.

```shell
export REGISTRY_URL="http://localhost:8081/apis/registry/v3"
export REGISTRY_USERNAME="sr-readonly"
export REGISTRY_PASSWORD="roPassword"
```
