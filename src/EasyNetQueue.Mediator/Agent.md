# EasyNetQ.Mediator Agent Notes

## Overview
- `EasyNetQ.Mediator` is a .NET 8 library that bridges EasyNetQ (RabbitMQ client) with MediatR.
- Incoming messages are translated into MediatR requests so application handlers can stay transport-agnostic.
- Outgoing interactions are split between queue-based senders and exchange-based publishers, exposing small configurables via factory objects.

## Solution Layout
- `EasyNetQ.Mediator/` – core library with abstractions, factories, sender/receiver implementations, and execution pipeline.
- `EasyNetQ.Mediator.Test/` – xUnit tests (unit plus RabbitMQ-backed integration tests). Requires a local RabbitMQ broker on `localhost`.
- `EasyNetQueue.Mediator.Tests/` – legacy/placeholder test project (AutoMapper, FluentAssertions). Not wired into the current solution file.
- `azure-pipelines.yml` – CI definition scaffold.

## Core Library Map
### Messages & Helpers
- `Message/BaseMessage` – marker record that all transport payloads inherit.
- `Helper` – default naming helpers for queues, exchanges, RPC queues.

### Factories & Options
- `QueueFactory<T>` / `IQueueFactory<T>` – wraps `IBus` and exposes `QueueOptions` for point-to-point send/receive.
- `ExchangeFactory<T>` / `ISubscriberFactory<T>` – wraps `IBus` for publish/subscribe scenarios; sets defaults for exchanges and per-subscriber queues.
- `QueueOptions`, `ExchangeOptions`, `SubscriberOptions` – strongly-typed option bags exposed to callers; defaults ensure deterministic naming unless overridden.

### Sending APIs
- `IMessageSender<T>` + `MessageSender<T>` – sends messages directly to queues (uses `Exchange.Default` and `PublishAsync` with a mandatory flag).
- `IMessagePublisher<T>` + `MessagePublisher<T>` – fan-out publisher; declares exchange before publishing.

### Consuming APIs
- `IMessageReceiver<T>` + `MessageReceiver<T>` – queue consumers; `ReceiveAsync` wires a handler delegate to `AdvancedBus.Consume`.
- `IMessageSubscriber<T>` + `MessageSubscriber<T>` – exchange subscribers; declares exchange/queue pair, binds, then consumes.

### Execution Pipeline
- `ReceiverRegistrationBuilder` / `ReceiverRegistration` – fluent builder used to map transport messages to MediatR request types and queue settings.
- `ReceiverExecutor<TMessage, TCommand>` – consumes queue messages, maps them to commands via `IMessageMapper`, and dispatches through `ISender`.
- `RabbitMediatorExecutorLauncher` – runtime orchestrator that spins up receivers for every registration inside a scoped DI container, resolving `IBus`, `ISender`, and `IMessageMapper`.
- `IMessageMapper` – abstraction expected to convert bus messages into MediatR commands (implementation provided by host application).

## External Dependencies
- `EasyNetQ` – RabbitMQ abstraction (bus, advanced features, topology).
- `MediatR` – command dispatch inside the application boundary.
- `Microsoft.Extensions.Logging.Abstractions` – optional logging for receivers.

## Testing Layers
- Unit tests (e.g., `ReceiverRegistrationTest`) verify configuration mechanics.
- Integration tests (`PublishSubscribeIntegrationTest`, `SenderReceiverIntegrationTest`) run against a live RabbitMQ instance; the fixture connects with default guest credentials.
- Some test classes are placeholders and currently lack concrete assertions (`DependencyInjectionTest`, `ReceiverExecutorTest`).

## Extension Points & Considerations
- Implement `IMessageMapper` and register it alongside `ISender` and `IBus` in DI before invoking `RabbitMediatorExecutorLauncher`.
- `ReceiverRegistration.Options` exposes queue-level toggles; additional EasyNetQ topology tweaks would require extending option classes or factory logic.
- Logging in `MessageReceiver<T>` defaults to `NullLogger` if DI does not provide an `ILogger<MessageReceiver<T>>`.
- The library assumes the caller manages lifecycle for `IBus` and the executor launcher (e.g., host background service).
