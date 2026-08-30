# MessageQueueEvolution

A step-by-step journey from a simple in-memory worker queue to a more advanced, generic message processing architecture in C#.

The main purpose of this repository is **not to present a perfect queue implementation from the first commit**.

Instead, the repository intentionally starts with simple, tightly coupled and domain-specific implementations, then gradually improves them by identifying problems and introducing better abstractions.

The goal is to understand **why** concepts such as message dispatching, retry policies, cancellation, timeouts, message envelopes, custom queues and message buses exist.

## Evolution Roadmap

| Version | Stage | Status |
|---|---|---|
| V1 | `01-PrimitiveQueue` | ✅ Completed |
| V2 | `02-GenericMessageBus` | 🚧 Current |
| V3 | `03-ResilientMessageBus` | ⏳ Planned |
| V4 | `04-ConcurrentMessageBus` | ⏳ Planned |
| V5 | `05-AdvancedQueueing` | ⏳ Planned |
| V6 | `06-PersistentMessageBus` | ⏳ Planned |
| V7 | `07-ReliableMessageBus` | ⏳ Planned |
| V8 | `08-ObservableMessageBus` | ⏳ Planned |
| V9 | `09-DistributedMessageBus` | ⏳ Planned |
| V10 | `10-ProductionMessagingPlatform` | ⏳ Planned |

> **Current Stage: V2 — GenericMessageBus**

---

# Why This Repository Exists

It is easy to look at technologies such as:

- RabbitMQ
- MassTransit
- MediatR
- Azure Service Bus
- Kafka
- Hangfire

and use them without fully understanding the problems they solve.

This repository approaches the subject from the opposite direction.

We start with something simple:

```text
Queue
  ↓
Worker Thread
  ↓
Dequeue
  ↓
Execute
```

Then we gradually encounter real problems:

```text
What if processing fails?

What if I need to retry?

What if retries should wait?

What if I want to cancel a message?

What if an operation takes too long?

What if different message types need different handlers?

What if I need multiple queues?

What if the queue becomes full?

What if I need a result from a message?

What if I need Commands and Queries?

What if the application shuts down?
```

Each version of the project attempts to solve some of these problems.

---

# Evolution

The repository is designed as an evolutionary architecture exercise.

```text
Simple Queue
     │
     ▼
Worker Thread
     │
     ▼
Producer / Consumer
     │
     ▼
Domain Specific Queues
     │
     ▼
Retry Queues
     │
     ▼
Retry Policies
     │
     ▼
Generic Messages
     │
     ▼
Message Envelope
     │
     ▼
Message Dispatcher
     │
     ▼
Command / Query
     │
     ▼
Custom Work Queues
     │
     ▼
Cancellation / Timeout
     │
     ▼
Message Bus
     │
     ▼
More advanced messaging concepts...
```

The important part is the transition between these steps.

---

# Project Structure

Currently the solution contains two main implementations.

```text
MessageQueueEvolution
│
├── Base
│
│   ├── Core
│   ├── Manager
│   ├── QueueMessages
│   ├── Queues
│   ├── Service
│   ├── ThreadMessages
│   └── Program.cs
│
└── General
    │
    ├── Commands
    ├── Dispatcher
    ├── Envelope
    ├── Messages
    ├── Options
    ├── Queue
    ├── Retry
    ├── MessageBus.cs
    ├── MessageHandle.cs
    └── Program.cs
```

---

# 1. Base Implementation

The `Base` project represents the earlier stages of the evolution.

It contains a traditional producer-consumer implementation using:

```csharp
Queue<T>
Thread
Monitor.Wait
Monitor.Pulse
lock
```

The basic idea is:

```text
Producer
   │
   │ Enqueue
   ▼
┌───────────────┐
│ Message Queue │
└───────┬───────┘
        │
        │ Dequeue
        ▼
   Worker Thread
        │
        ▼
     Execute
```

When the queue is empty, the worker does not continuously consume CPU.

Instead, it waits:

```csharp
Monitor.Wait(...)
```

When a producer adds a new message:

```csharp
Queue.Enqueue(message);

Monitor.Pulse(...);
```

the worker is awakened.

This demonstrates the fundamental **Producer / Consumer** pattern.

---

# Domain-Specific Retry Queues

The Base project also contains examples such as:

```text
RefundQueue
ReversalQueue
SettlementQueue
```

These represent an important intermediate stage.

Instead of processing every message immediately, some operations need to be retried later.

A typical flow looks like:

```text
Operation
   │
   ▼
Send Request
   │
   ├── Success ─────────────► Remove
   │
   └── Failure
          │
          ▼
     Schedule Retry
          │
          ▼
      NextTryTime
          │
          ▼
        Retry
```

Messages may contain metadata such as:

```text
TryCount
NextTryTime
```

A timer periodically checks which messages are ready to be processed again.

This introduces concepts such as:

- Retry
- Delayed Retry
- Backoff
- Polling
- Store-and-forward
- Retry scheduling

---

# The Problem With Domain-Specific Queues

At first, separate queue classes may seem reasonable:

```text
RefundQueue
ReversalQueue
SettlementQueue
```

But eventually duplicated logic starts to appear.

For example:

```text
Queue storage
Locking
Retry count
Next retry time
Polling
Error handling
Removing completed messages
Maximum retry count
```

Only the actual business operation changes.

This leads to an important design question:

> Why should the queue infrastructure know anything about refunds, reversals or settlements?

The infrastructure should manage messages.

The application should define what those messages mean.

This leads to the next stage.

---

# 2. General Implementation

The `General` project moves toward a domain-independent message processing architecture.

Instead of building:

```text
RefundQueue
ReversalQueue
SettlementQueue
```

we can create generic queues:

```text
commands
queries
background
notifications
integrations
```

The queue itself no longer needs to understand the business domain.

---

# Messages

At the center of the architecture is a message.

```text
IMessage
   │
   ├── ICommand
   │
   └── IQuery<TResult>
```

A command represents an intention to perform an operation.

Example:

```csharp
public record CreateOrderCommand(
    int CustomerId,
    decimal Amount
) : ICommand;
```

A query represents a request for data.

```csharp
public record GetOrderQuery(
    int OrderId
) : IQuery<OrderResponse>;
```

The queue does not need to know what `CreateOrderCommand` actually does.

It only knows that it is a message that must eventually be dispatched.

---

# Message Dispatcher

Earlier versions may require code similar to:

```csharp
switch (message.MessageType)
{
    case MessageType.A:
        ...
        break;

    case MessageType.B:
        ...
        break;
}
```

This creates coupling between the queue infrastructure and message types.

The generic implementation introduces a dispatcher.

```text
Message
   │
   ▼
Dispatcher
   │
   ├── CreateOrderCommand
   │        ↓
   │   CreateOrderHandler
   │
   └── GetOrderQuery
            ↓
       GetOrderHandler
```

Handlers can be registered independently from the queue.

The queue therefore becomes responsible for **processing**, not business logic.

---

# Message Envelope

A business message alone is not enough for a queue system.

The infrastructure also needs metadata.

For this reason messages are wrapped inside a `MessageEnvelope`.

Conceptually:

```text
MessageEnvelope
│
├── Id
├── Message
├── CreatedAt
├── Attempt
├── RetryPolicy
├── Timeout
├── CancellationToken
└── Completion
```

This separation is important.

For example:

```text
CreateOrderCommand
```

should describe the business operation.

It should not need properties such as:

```text
RetryCount
Timeout
QueueName
CancellationSource
```

Those belong to the messaging infrastructure.

---

# Retry Policies

Retry behavior is extracted into a separate policy.

Different messages may require completely different retry strategies.

## No Retry

```text
Attempt
   │
   X
Failure
   │
   ▼
Stop
```

## Fixed Retry

```text
Attempt 1
   │
   X
   │
  5 sec
   │
Attempt 2
   │
   X
   │
  5 sec
   │
Attempt 3
```

## Exponential Backoff

```text
Attempt 1
   │
   X
   │
  1 sec
   │
Attempt 2
   │
   X
   │
  2 sec
   │
Attempt 3
   │
   X
   │
  4 sec
   │
Attempt 4
```

This is similar to retry strategies commonly used when communicating with external services.

---

# Delayed Retry

An important rule is that a worker should ideally not remain blocked while waiting for a retry.

Bad approach:

```text
Message A fails

Worker
  │
  ▼
Sleep 5 minutes
  │
  ▼
Retry Message A
```

During those five minutes, the worker cannot process other work.

A better model is:

```text
Message A
   │
   X
Failure
   │
   ▼
Schedule Retry
   │
   └─────────────── 5 minutes ──────────────┐

Worker continues                              │
   │                                          │
   ├── Message B                              │
   ├── Message C                              │
   └── Message D                              │
                                              │
                                              ▼
                                       Message A returns
```

This allows retry delays without unnecessarily blocking the worker.

---

# Cancellation

Each queued message can be identified by an ID.

```text
Message
   │
   └── Id
```

This allows operations such as:

```csharp
bus.Cancel(
    "commands",
    messageId);
```

Cancellation may occur while a message is:

```text
Waiting in Queue

Processing

Waiting for Retry
```

Handlers receive a `CancellationToken`, allowing cancellation to propagate into async operations.

Example:

```csharp
await httpClient.SendAsync(
    request,
    cancellationToken);
```

---

# Timeout

Cancellation and timeout are related but different concepts.

Cancellation means:

> The caller no longer wants this operation.

Timeout means:

> The operation exceeded the amount of time it was allowed to run.

Example:

```csharp
new MessageOptions
{
    Timeout = TimeSpan.FromSeconds(5)
};
```

A timeout may then participate in the retry policy.

```text
API Request
   │
   │ > 5 seconds
   ▼
Timeout
   │
   ▼
Retry Policy
   │
   ├── Retry
   └── Stop
```

---

# MessageHandle

When a message is submitted, the caller receives a handle.

Conceptually:

```text
MessageHandle
│
├── Id
└── Completion
```

For queries:

```text
MessageHandle<TResult>
│
├── Id
└── Task<TResult>
```

This gives the caller two important abilities:

```text
Wait for completion

or

Cancel using the message ID
```

---

# Custom Queues

A message bus can contain multiple independent work queues.

Example:

```csharp
bus.CreateQueue("commands");

bus.CreateQueue("queries");

bus.CreateQueue("background");
```

Later the same infrastructure could be used for:

```text
email
reports
payments
notifications
integrations
slow-api
file-processing
```

The important design principle is:

> The queue infrastructure should not depend on the domain.

Instead:

> The domain should use the queue infrastructure.

---

# Current Architecture

The current direction of the project can be summarized as:

```text
                         MessageBus
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
           ▼                 ▼                 ▼
       commands           queries         background
           │                 │                 │
           ▼                 ▼                 ▼
       WorkQueue         WorkQueue         WorkQueue
           │                 │                 │
           └─────────────────┼─────────────────┘
                             │
                             ▼
                        Dispatcher
                             │
                  ┌──────────┼──────────┐
                  │          │          │
                  ▼          ▼          ▼
               Command     Query      Action
                  │          │          │
                  ▼          ▼          ▼
               Handler    Handler    Delegate
```

Every queued message may additionally have:

```text
Retry
Timeout
Cancellation
Completion
Message ID
Attempt Count
```

---

# Why Keep The Old Implementations?

The earlier implementations are intentionally kept in this repository.

They are not deleted when a better abstraction is introduced.

That is because the purpose of this repository is to show the **reasoning behind architectural evolution**.

If only the final implementation existed, we could see:

```text
MessageBus
Dispatcher
Envelope
RetryPolicy
WorkQueue
```

but it would be harder to understand why they were needed.

By keeping previous versions, we can compare:

```text
Problem
   ↓
Naive Solution
   ↓
New Problem
   ↓
Refactoring
   ↓
New Abstraction
```

This repository therefore focuses on the journey, not only the destination.

---

# Concepts Explored

The project currently explores or is expected to explore concepts such as:

- Producer / Consumer
- Worker Threads
- Thread Synchronization
- `Monitor.Wait`
- `Monitor.Pulse`
- In-Memory Queues
- `Channel<T>`
- Async Processing
- Message Dispatching
- Commands
- Queries
- Message Envelopes
- Retry Policies
- Fixed Retry
- Exponential Backoff
- Delayed Retry
- Cancellation
- Timeout
- Backpressure
- Graceful Shutdown
- Custom Queues
- Message Handles
- Message Bus Architecture

---

# Roadmap

This repository will continue evolving.

Possible future steps include:

### Multiple Workers

Allow multiple consumers to process messages concurrently.

```text
Queue
 │
 ├── Worker 1
 ├── Worker 2
 ├── Worker 3
 └── Worker 4
```

---

### Priority Queues

Support different message priorities.

```text
Critical
High
Normal
Low
```

---

### Dead Letter Queue

Messages that exceed their retry limit should not silently disappear.

```text
Message
   │
   X
Max Retry Reached
   │
   ▼
Dead Letter Queue
```

---

### Scheduled Messages

Allow messages to be executed in the future.

```text
Send Now
   │
   ▼
Execute at 18:00
```

---

### Persistence

Current queues are primarily in-memory.

A future version may persist queued messages so they can survive application restarts.

Possible implementations:

```text
SQL Server
Redis
File Storage
```

---

### Idempotency

Retrying a message introduces another important question:

> What happens if the same operation is executed twice?

Idempotency support can prevent duplicate side effects.

---

### Circuit Breaker

If an external service is unavailable, continuously retrying may make the situation worse.

A future version may introduce:

```text
Closed
  ↓
Open
  ↓
Half-Open
```

circuit breaker behavior.

---

### Middleware / Pipeline

Messages could pass through middleware components before reaching their handler.

```text
Message
   │
   ▼
Logging
   │
   ▼
Validation
   │
   ▼
Metrics
   │
   ▼
Retry
   │
   ▼
Handler
```

This would make the architecture similar to pipelines used by frameworks such as MediatR and MassTransit.

---

### Observability

Future versions may introduce:

```text
CorrelationId
MessageId
Execution Time
Retry Count
Queue Length
Success Count
Failure Count
Structured Logging
Metrics
Tracing
```

---

### External Message Broker

Eventually the abstractions developed here could be adapted to a real message broker.

For example:

```text
Application
     │
     ▼
 IMessageBus
     │
     ├── InMemoryMessageBus
     │
     └── RabbitMqMessageBus
```

This would allow comparison between an in-memory message processing system and a distributed message broker.

---

### Outbox / Inbox

Once messaging becomes distributed, consistency problems appear.

Future experiments may include:

```text
Transactional Outbox

Inbox / Deduplication

Eventual Consistency
```

---

# What This Project Is Not

This repository is primarily an **educational architecture exercise**.

It is not intended to replace production-ready systems such as:

- RabbitMQ
- MassTransit
- Azure Service Bus
- Kafka
- MediatR
- Hangfire

Instead, the purpose is to understand the architectural problems that eventually lead to tools and frameworks like these.

---

# Main Learning Goal

The main question behind this repository is not:

> How do I create a Queue in C#?

It is:

> How does a simple Queue gradually evolve into a message processing system?

Starting from:

```csharp
Queue<T>
```

and gradually reaching concepts such as:

```text
Message Bus
Command / Query
Dispatcher
Retry
Backoff
Cancellation
Timeout
Dead Letter Queue
Idempotency
Persistence
Observability
Distributed Messaging
```

is the actual purpose of **MessageQueueEvolution**.

---

# Status

🚧 **Work in progress**

The architecture is intentionally evolving over time.

Existing implementations may be refactored, replaced or kept for comparison as new concepts are introduced.
