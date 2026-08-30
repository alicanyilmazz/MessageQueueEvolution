# MessageQueueEvolution

> **The queue is the project. Concurrency is the subject.**

`MessageQueueEvolution` is a step-by-step journey through **threading, asynchronous programming, concurrency, synchronization, resilience, and message processing in C#**.

The main goal of this repository is **not simply to build another queue implementation**.

Instead, a message queue is used as a practical environment to understand **why concurrency concepts and messaging abstractions exist in the first place**.

Rather than learning concepts such as `Thread`, `Task`, `async/await`, `lock`, `Monitor`, `ConcurrentDictionary`, `Channel<T>`, `CancellationToken`, retry policies, backpressure, idempotency, and distributed messaging independently, this repository introduces them **when a real problem in the architecture requires them**.

---

# The Learning Approach

The repository intentionally does **not** start with the best possible architecture.

It starts with a simple implementation.

Then we break it.

Then we understand why it breaks.

Then we introduce the concept that solves the problem.

```text
Build a simple version
        │
        ▼
Discover a problem
        │
        ▼
Understand the concurrency / architecture issue
        │
        ▼
Introduce the appropriate concept
        │
        ▼
Refactor the implementation
        │
        ▼
Discover the next problem
        │
        ▼
Repeat
```

The purpose is to understand the transition from:

```csharp
Queue<T>
```

to a much more capable message processing architecture.

---

# Why Build It This Way?

It is easy to use technologies such as:

- RabbitMQ
- MassTransit
- MediatR
- Kafka
- Azure Service Bus
- Hangfire
- Polly

without fully understanding why concepts such as these exist:

```text
Retry
Backoff
Cancellation
Timeout
Dead Letter Queue
Idempotency
Message Envelope
CorrelationId
Backpressure
Outbox
Inbox
Circuit Breaker
Consumer
Producer
Dispatcher
```

This repository approaches the subject from the opposite direction.

Instead of starting with the abstraction, we start with the **problem that creates the abstraction**.

For example:

```text
Multiple threads access Queue<T>
        │
        ▼
Race Condition
        │
        ▼
lock
```

Then:

```text
Worker continuously checks an empty queue
        │
        ▼
Busy Waiting
        │
        ▼
Monitor.Wait / Monitor.Pulse
```

Then:

```text
Manual synchronization becomes complicated
        │
        ▼
Need a higher-level Producer / Consumer abstraction
        │
        ▼
Channel<T>
```

Then:

```text
Multiple threads manage pending messages
        │
        ▼
Shared Mutable State
        │
        ▼
ConcurrentDictionary<TKey, TValue>
```

Then:

```text
A running operation must be stopped
        │
        ▼
Cooperative Cancellation
        │
        ▼
CancellationToken
```

Then:

```text
Producer is faster than Consumer
        │
        ▼
Queue grows indefinitely
        │
        ▼
Bounded Channel
        │
        ▼
Backpressure
```

Every concept is introduced because the previous version creates a real reason to use it.

---

# Evolution Roadmap

The repository evolves through multiple versions.

| Version | Stage | Status | Main Focus |
|---|---|---|---|
| V1 | `01-PrimitiveQueue` | ✅ Completed | Thread, Queue, Monitor, Producer / Consumer |
| V2 | `02-GenericMessageBus` | 🚧 Current | Generic Messages, Command / Query, Dispatcher, Channel, Retry, Cancellation |
| V3 | `03-ResilientMessageBus` | ⏳ Planned | Advanced Retry, Backoff, Timeout, Circuit Breaker |
| V4 | `04-ConcurrentMessageBus` | ⏳ Planned | Multiple Workers, Parallel Consumers, Ordering, SemaphoreSlim |
| V5 | `05-AdvancedQueueing` | ⏳ Planned | Priority, Scheduling, Delayed Messages, Backpressure |
| V6 | `06-PersistentMessageBus` | ⏳ Planned | Persistence, Restart Recovery, Durable Messages |
| V7 | `07-ReliableMessageBus` | ⏳ Planned | Dead Letter Queue, Idempotency, Deduplication |
| V8 | `08-ObservableMessageBus` | ⏳ Planned | CorrelationId, Metrics, Logging, Tracing, OpenTelemetry |
| V9 | `09-DistributedMessageBus` | ⏳ Planned | RabbitMQ, Multiple Services, Distributed Consumers |
| V10 | `10-ProductionMessagingPlatform` | ⏳ Planned | Outbox, Inbox, Eventual Consistency, Production Architecture |

> ### Current Stage
>
> 🚧 **V2 — GenericMessageBus**

---

# V1 — PrimitiveQueue

The first version intentionally starts with low-level .NET primitives.

The implementation is based on concepts such as:

```text
Queue<T>
Thread
lock
Monitor.Wait
Monitor.Pulse
Producer
Consumer
Worker Thread
Polling
Retry
```

The basic architecture looks like this:

```text
Producer
   │
   │ Enqueue
   ▼
┌───────────────┐
│   Queue<T>    │
└───────┬───────┘
        │
        │ Dequeue
        ▼
   Worker Thread
        │
        ▼
     Execute
```

When there is no work available, the worker waits:

```csharp
Monitor.Wait(...)
```

When a producer adds a message:

```csharp
queue.Enqueue(message);

Monitor.Pulse(...);
```

the worker wakes up and continues processing.

This version helps demonstrate the basic **Producer / Consumer** model and low-level thread synchronization.

---

## Domain-Specific Retry Queues

V1 also contains queue implementations created for specific operations.

The idea is roughly:

```text
Operation
    │
    ▼
Send Request
    │
    ├──────── Success ────────► Remove
    │
    └──────── Failure
                │
                ▼
            TryCount++
                │
                ▼
          NextTryTime
                │
                ▼
           Retry Later
```

These queues introduce concepts such as:

```text
Retry
Delayed Retry
Polling
Backoff
TryCount
NextTryTime
Failure Handling
```

They work, but they expose several architectural problems.

---

# Problems Discovered in V1

## Domain Coupling

The infrastructure starts knowing too much about the business operation.

Instead of having a generic queue engine, separate queue implementations begin to appear.

The question becomes:

> Why should queue infrastructure know what a specific business operation means?

Ideally:

```text
Infrastructure
      │
      ▼
Process Message
```

not:

```text
Infrastructure
      │
      ├── Understand Operation A
      ├── Understand Operation B
      └── Understand Operation C
```

---

## Duplicate Infrastructure Logic

Different queue implementations start repeating the same mechanisms:

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

Only the business operation changes.

This is one of the main reasons V2 exists.

---

## Message Type Switches

A worker may eventually need code similar to:

```csharp
switch (message.MessageType)
{
    case MessageType.A:
        // process A
        break;

    case MessageType.B:
        // process B
        break;
}
```

Every new message requires modifying the queue infrastructure.

This creates unnecessary coupling.

A better architecture should allow new messages and handlers to be introduced **without changing the worker itself**.

---

# V2 — GenericMessageBus

V2 begins transforming the primitive queue into a reusable message processing system.

The infrastructure should no longer care about the business meaning of a message.

Instead of creating infrastructure classes for every operation, we introduce generic concepts such as:

```text
IMessage
ICommand
IQuery<TResult>
MessageEnvelope
MessageDispatcher
MessageHandle
WorkQueue
MessageBus
RetryPolicy
```

The architecture begins to look like this:

```text
                         MessageBus
                             │
             ┌───────────────┼───────────────┐
             │               │               │
             ▼               ▼               ▼
         commands         queries        background
             │               │               │
             ▼               ▼               ▼
         WorkQueue        WorkQueue        WorkQueue
             │               │               │
             └───────────────┼───────────────┘
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

---

# Commands and Queries

Commands represent an intention to perform an operation.

Example:

```csharp
public record CreateOrderCommand(
    int CustomerId,
    decimal Amount
) : ICommand;
```

Queries represent a request for information.

```csharp
public record GetOrderQuery(
    int OrderId
) : IQuery<OrderResponse>;
```

The queue does not need to know what these messages actually do.

It only needs to know how to deliver them to the correct handler.

---

# Message Dispatcher

Instead of using a large `switch` statement, handlers are registered independently.

Conceptually:

```text
CreateOrderCommand
        │
        ▼
    Dispatcher
        │
        ▼
CreateOrderHandler
```

and:

```text
GetOrderQuery
       │
       ▼
   Dispatcher
       │
       ▼
GetOrderHandler
```

This separates:

```text
Message Processing Infrastructure
```

from:

```text
Business Logic
```

---

# Message Envelope

A business message should contain business information.

Infrastructure information belongs somewhere else.

For that reason messages are wrapped in an envelope.

```text
MessageEnvelope
│
├── MessageId
├── Message
├── CreatedAt
├── Attempt
├── RetryPolicy
├── Timeout
├── CancellationToken
└── Completion
```

For example:

```text
CreateOrderCommand
        │
        ▼
┌─────────────────────────┐
│     MessageEnvelope     │
├─────────────────────────┤
│ Id                      │
│ Attempt                 │
│ RetryPolicy             │
│ Timeout                 │
│ Cancellation            │
│ Completion              │
│                         │
│ Message                 │
│   └─ CreateOrderCommand │
└─────────────────────────┘
```

This keeps business messages clean while allowing the messaging infrastructure to track processing metadata.

---

# Retry

Failures are expected when communicating with external systems.

A message may therefore define a retry policy.

For example:

```text
Attempt 1
    │
    X
    │
  1 second
    │
Attempt 2
    │
    X
    │
  2 seconds
    │
Attempt 3
    │
    X
    │
  4 seconds
    │
Attempt 4
```

This introduces concepts such as:

```text
Fixed Retry
Linear Backoff
Exponential Backoff
Maximum Attempts
Retryable Exceptions
```

Later versions will expand this into more advanced resilience patterns.

---

# Delayed Retry Without Blocking Workers

A worker should not sleep while waiting for a message retry.

Bad:

```text
Message A fails
        │
        ▼
Worker sleeps for 5 minutes
        │
        ▼
Worker cannot process anything else
```

Better:

```text
Message A fails
        │
        ▼
Retry scheduled
        │
        └─────────────── 5 minutes ───────────────┐
                                                  │
Worker continues                                  │
        │                                         │
        ├── Message B                             │
        ├── Message C                             │
        └── Message D                             │
                                                  │
                                                  ▼
                                         Message A returns
```

This is an important step from blocking thread-based designs toward asynchronous message processing.

---

# Cancellation

Messages can be canceled using `CancellationToken`.

A message may be:

```text
Waiting
Processing
Waiting for Retry
```

and cancellation should be able to propagate through the execution pipeline.

For example:

```csharp
await httpClient.SendAsync(
    request,
    cancellationToken);
```

This introduces the idea of **cooperative cancellation**.

---

# Timeout

Cancellation and timeout are not the same thing.

Cancellation means:

> The caller no longer wants the operation.

Timeout means:

> The operation took longer than it was allowed to take.

Example:

```csharp
new MessageOptions
{
    Timeout = TimeSpan.FromSeconds(5)
};
```

A timeout may later trigger retry logic.

```text
Request
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

# ConcurrentDictionary

As the architecture evolves, multiple execution paths may need to access the collection of pending messages.

For example:

```text
Producer
   │
   └── Add pending message

Worker
   │
   └── Remove completed message

Another Thread
   │
   └── Find message to cancel
```

Using a normal `Dictionary<TKey,TValue>` from multiple threads would introduce thread-safety problems.

This creates a real reason to introduce:

```csharp
ConcurrentDictionary<TKey, TValue>
```

Again, the concept is introduced because the architecture creates the need for it.

---

# Channel<T>

V1 manually combines:

```text
Queue<T>
Thread
lock
Monitor.Wait
Monitor.Pulse
```

V2 begins moving toward:

```csharp
Channel<T>
```

`Channel<T>` provides a higher-level asynchronous Producer / Consumer abstraction.

Conceptually:

```text
Producer
   │
   ▼
Channel<T>
   │
   ▼
Consumer
```

It also makes later concepts such as **bounded capacity and backpressure** easier to implement.

---

# Why Console Applications?

The early versions intentionally use Console Applications as lightweight hosts.

This is a deliberate choice.

The purpose of the early stages is to focus on:

```text
Threading
Concurrency
Synchronization
Message Processing
Retry
Cancellation
Queue Behavior
```

without introducing unrelated concerns such as:

```text
HTTP
Controllers
Swagger
UI
Authentication
Database APIs
Web Framework Configuration
```

The queue implementation itself can live inside reusable class libraries while a Console Application demonstrates its behavior.

As the repository evolves toward distributed messaging, later versions will introduce technologies such as:

```text
ASP.NET Core
Worker Services
Databases
RabbitMQ
OpenTelemetry
Distributed Services
```

The Console Application is therefore only the **host used to observe the architecture**, not the architecture itself.

---

# What Will We Learn?

Throughout this evolution, the repository will gradually explore:

```text
Thread
ThreadPool
Task
async / await

Blocking vs Non-Blocking

Concurrency vs Parallelism

Producer / Consumer

Race Conditions
Critical Sections

lock

Monitor.Wait
Monitor.Pulse

ConcurrentQueue<T>
ConcurrentDictionary<TKey,TValue>

TaskCompletionSource<T>

CancellationToken
CancellationTokenSource
Linked Cancellation Tokens

SemaphoreSlim

Channel<T>

Bounded Channels
Unbounded Channels

Backpressure

Multiple Consumers
Ordering

Retry
Delayed Retry
Exponential Backoff

Timeout

Circuit Breaker

Priority Queues

Scheduled Messages

Persistence

Recovery

Dead Letter Queue

Idempotency

Deduplication

CorrelationId

Metrics
Tracing
OpenTelemetry

Distributed Messaging

RabbitMQ

Outbox Pattern
Inbox Pattern

Eventual Consistency
```

These concepts will not be added only because they are popular.

Each one should answer a problem that appeared in the previous version.

---

# Future Evolution

The long-term direction of the project is:

```text
V1
PrimitiveQueue
     │
     ▼
V2
GenericMessageBus
     │
     ▼
V3
ResilientMessageBus
     │
     ▼
V4
ConcurrentMessageBus
     │
     ▼
V5
AdvancedQueueing
     │
     ▼
V6
PersistentMessageBus
     │
     ▼
V7
ReliableMessageBus
     │
     ▼
V8
ObservableMessageBus
     │
     ▼
V9
DistributedMessageBus
     │
     ▼
V10
ProductionMessagingPlatform
```

The final goal is not to compete with existing production-ready messaging frameworks.

The goal is to understand **why those frameworks look the way they do**.

---

# What This Repository Is Not

This repository is an educational architecture experiment.

It is not intended to replace:

- RabbitMQ
- MassTransit
- Kafka
- Azure Service Bus
- MediatR
- Hangfire
- Polly

Instead, these technologies become easier to understand after experiencing the problems they were designed to solve.

For example:

```text
Why does RabbitMQ need acknowledgements?

Why does MassTransit have consumers?

Why does MediatR use handlers?

Why does Polly provide retry and circuit breaker policies?

Why do message brokers have Dead Letter Queues?

Why do distributed systems need idempotency?

Why does the Outbox Pattern exist?
```

Instead of memorizing the answers, this repository attempts to **reach those problems naturally by evolving the architecture**.

---

# The Core Idea

The repository follows one simple principle:

> **Do not start with the abstraction. Start with the problem that creates the abstraction.**

And that leads to the central idea behind the project:

> **The queue is the project. Concurrency is the subject.**

We are not learning concurrency just to build a queue.

We are building a queue so that we can understand concurrency.

---

# Status

🚧 **Work in Progress**

Current stage:

```text
V2 — GenericMessageBus
```

Previous implementations are intentionally kept so that each architectural step can be compared with the version that came before it.

The destination matters.

But in this repository, **the evolution is the actual lesson**.
