# MessageQueueEvolution

# Evolution Roadmap

| Version | Stage | Status | Main Focus |
|---|---|---|---|
| V1 | `01-PrimitiveQueue` | ✅ Completed | Thread, Queue, lock, Monitor, Producer / Consumer |
| V2 | `02-GenericMessageBus` | 🚧 Current | Task, async/await, Channel, ConcurrentDictionary, Command / Query, Retry, Cancellation |
| V3 | `03-ResilientMessageBus` | ⏳ Planned | Retry Policies, Backoff, Timeout, Circuit Breaker |
| V4 | `04-ConcurrentMessageBus` | ⏳ Planned | Multiple Workers, SemaphoreSlim, Race Conditions, Ordering |
| V5 | `05-AdvancedQueueing` | ⏳ Planned | Priority, Scheduling, Delayed Messages, Backpressure |
| V6 | `06-PersistentMessageBus` | ⏳ Planned | Persistence, Recovery, Durable Messages |
| V7 | `07-ReliableMessageBus` | ⏳ Planned | DLQ, Idempotency, Deduplication, Delivery Semantics |
| V8 | `08-ObservableMessageBus` | ⏳ Planned | CorrelationId, Metrics, Logging, Tracing, OpenTelemetry |
| V9 | `09-DistributedMessageBus` | ⏳ Planned | RabbitMQ, Producers, Consumers, Acknowledgements |
| V10 | `10-ProductionMessagingPlatform` | ⏳ Planned | Outbox, Inbox, Eventual Consistency, Production Architecture |


> **The queue is the project. Concurrency is the subject.**

`MessageQueueEvolution` is a step-by-step journey through **threading, asynchronous programming, concurrency, synchronization, resilience, and message processing in C#/.NET**.

The main goal of this repository is **not simply to build another queue implementation**.

Instead, a message queue is used as a practical environment to understand:

- how threads work,
- how multiple threads coordinate,
- how race conditions appear,
- how shared state becomes dangerous,
- how synchronization primitives solve different problems,
- how asynchronous programming differs from multithreading,
- how producer/consumer systems work,
- and eventually why modern messaging systems such as RabbitMQ, MassTransit and Azure Service Bus provide the abstractions they do.

Rather than studying these concepts independently, this repository introduces them **when a real problem in the evolving queue architecture requires them**.

---

# Main Philosophy

The repository follows one simple rule:

> **Do not start with the abstraction. Start with the problem that creates the abstraction.**

The learning cycle is:

```text
Build a simple version
        │
        ▼
Discover a problem
        │
        ▼
Understand the threading / concurrency problem
        │
        ▼
Introduce the appropriate .NET concept
        │
        ▼
Refactor the architecture
        │
        ▼
Discover the next problem
        │
        ▼
Repeat
```

The goal is not to immediately write the cleanest or most advanced architecture.

Earlier implementations are intentionally kept in the repository so that we can understand:

```text
What was the original problem?

Why did the previous solution become insufficient?

What concurrency problem appeared?

Why was a new abstraction introduced?

What trade-off did the new solution create?
```

---

# Why Use a Queue to Learn Concurrency?

A queue looks simple:

```csharp
Queue<T>
```

But as soon as multiple execution paths start interacting with it, many fundamental computer science and .NET concepts naturally appear.

For example:

```text
Two threads access Queue<T>
        │
        ▼
Race Condition
        │
        ▼
Critical Section
        │
        ▼
lock / Monitor
```

Then:

```text
Worker continuously checks whether Queue is empty
        │
        ▼
Busy Waiting
        │
        ▼
Monitor.Wait / Monitor.Pulse
```

Then:

```text
Multiple threads modify shared state
        │
        ▼
Thread Safety Problem
        │
        ▼
Concurrent Collections
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

Then:

```text
A long running operation must be stopped
        │
        ▼
Cooperative Cancellation
        │
        ▼
CancellationToken
```

Then:

```text
External API fails
        │
        ▼
Retry
        │
        ▼
Backoff
        │
        ▼
Circuit Breaker
```

The queue gradually becomes a laboratory for understanding concurrency.

---

# What Will We Learn?

The project will gradually explore the following topics.

---

## 1. Threads

Starting with the lowest-level execution model:

```text
Thread
ThreadStart
ParameterizedThreadStart
Thread.CurrentThread
Thread.Name
Thread.IsAlive
Thread.Join
Thread.Sleep
Thread.Interrupt
Background Threads
Foreground Threads
```

Questions we want to understand:

```text
What is a thread?

What happens when a Thread starts?

What is the difference between foreground and background threads?

What does Thread.Join do?

Why is Thread.Sleep blocking?

Why should we avoid creating unlimited Threads?
```

---

# 2. ThreadPool

Creating a dedicated OS thread for every operation is expensive.

That naturally leads to:

```text
ThreadPool
Worker Threads
I/O Completion Threads
ThreadPool.QueueUserWorkItem
```

And eventually:

```text
Task
Task.Run
async / await
```

We will understand why modern .NET applications usually work with the ThreadPool instead of manually creating a new Thread for every operation.

---

# 3. Race Conditions

When multiple threads access shared state:

```text
Thread A
    │
    └── read value

Thread B
    │
    └── change value

Thread A
    │
    └── write old value
```

we can get unpredictable results.

Topics:

```text
Race Condition
Shared Mutable State
Critical Section
Atomicity
Thread Safety
```

---

# 4. lock

One of the first synchronization mechanisms we encounter will be:

```csharp
lock (_sync)
{
    // critical section
}
```

We will understand:

```text
What does lock actually do?

Why does lock need a shared object?

What is a critical section?

What happens if a lock is held for too long?

Why should external API calls generally not happen inside a lock?
```

---

# 5. Monitor

`lock` is built on top of `Monitor`.

We will explore:

```text
Monitor.Enter
Monitor.Exit
Monitor.Wait
Monitor.Pulse
Monitor.PulseAll
Monitor.TryEnter
```

The primitive queue starts with:

```text
Queue<T>
+
Thread
+
lock
+
Monitor.Wait
+
Monitor.Pulse
```

This gives us a low-level Producer / Consumer implementation.

---

# 6. Busy Waiting

A naive worker might do this:

```csharp
while (true)
{
    if (queue.Count > 0)
    {
        // process
    }
}
```

This creates:

```text
Busy Waiting
        │
        ▼
Unnecessary CPU Consumption
```

We will solve this first with:

```text
Monitor.Wait
Monitor.Pulse
```

and later replace manual coordination with higher-level abstractions.

---

# 7. Mutex

We will explore:

```text
Mutex
Named Mutex
Cross-Process Synchronization
```

and understand the important distinction:

```text
lock / Monitor
    │
    └── Synchronization inside a process

Mutex
    │
    └── Can synchronize across processes
```

Example problem:

```text
Application Instance A
        │
        ▼
Shared Resource

Application Instance B
        │
        ▼
Same Shared Resource
```

A named `Mutex` can coordinate them.

We will also discuss why a Mutex is usually heavier than `lock`.

---

# 8. Semaphore

We will explore:

```text
Semaphore
SemaphoreSlim
```

A lock allows:

```text
1 thread at a time
```

A semaphore can allow:

```text
N operations at a time
```

For example:

```text
External API

Maximum concurrent requests = 3

        ┌── Worker 1
API ◄───┼── Worker 2
        └── Worker 3

Worker 4 → waits
Worker 5 → waits
```

This becomes very useful when we introduce multiple queue consumers.

---

# 9. SemaphoreSlim

For modern asynchronous code we will primarily use:

```csharp
await semaphore.WaitAsync();
```

and:

```csharp
semaphore.Release();
```

This allows us to implement:

```text
Concurrency Limits
Throttling
Limited Parallelism
Resource Protection
```

without unnecessarily blocking threads.

---

# 10. Interlocked

Sometimes using a full lock is unnecessary.

We will explore atomic operations such as:

```text
Interlocked.Increment
Interlocked.Decrement
Interlocked.Exchange
Interlocked.CompareExchange
Interlocked.Add
```

For example:

```csharp
Interlocked.CompareExchange(
    ref _isProcessing,
    1,
    0);
```

can be used to prevent multiple workers from entering a section simultaneously.

This will help us understand:

```text
Atomic Operations
Lock-Free Operations
Compare-And-Swap
```

---

# 11. volatile and Memory Visibility

Concurrency is not only about two threads writing at the same time.

Another important problem is:

> When one thread changes a value, when does another thread see that change?

We will explore concepts such as:

```text
volatile
Memory Visibility
CPU Cache
Compiler Reordering
Memory Ordering
Memory Barriers
```

These topics will be introduced only where they help explain actual concurrency behavior.

---

# 12. Concurrent Collections

Instead of protecting every collection manually with locks, .NET provides concurrent collections.

We will explore:

```text
ConcurrentDictionary<TKey, TValue>
ConcurrentQueue<T>
ConcurrentStack<T>
ConcurrentBag<T>
BlockingCollection<T>
```

---

# 13. ConcurrentDictionary

For example, our Message Bus needs to track pending messages:

```text
MessageId
    │
    ▼
MessageEnvelope
```

At the same time:

```text
Producer
    └── Adds message

Worker
    └── Removes completed message

Cancellation Request
    └── Finds message

Retry Logic
    └── Reads message
```

Using:

```csharp
Dictionary<Guid, MessageEnvelope>
```

from multiple threads would be dangerous.

This creates the need for:

```csharp
ConcurrentDictionary<Guid, MessageEnvelope>
```

We will learn operations such as:

```text
TryAdd
TryRemove
TryGetValue
GetOrAdd
AddOrUpdate
```

and also understand an important point:

> A thread-safe collection does not automatically make the entire business operation thread-safe.

---

# 14. ConcurrentQueue

We will compare:

```text
Queue<T> + lock
```

with:

```text
ConcurrentQueue<T>
```

and understand what problems `ConcurrentQueue` solves and what problems it does **not** solve.

For example:

```text
ConcurrentQueue
```

can make enqueue/dequeue thread-safe, but it does not automatically provide:

```text
Waiting
Backpressure
Message Scheduling
Cancellation
```

---

# 15. BlockingCollection

Before moving to `Channel<T>`, we may also explore:

```text
BlockingCollection<T>
```

which provides a higher-level Producer / Consumer abstraction.

This helps show the evolution:

```text
Queue<T>
+
Monitor
        │
        ▼
BlockingCollection<T>
        │
        ▼
Channel<T>
```

---

# 16. Task

We will move from manually created Threads toward:

```text
Task
Task<TResult>
Task.Run
Task.WhenAll
Task.WhenAny
Task.Delay
```

and understand that:

> A Task is not the same thing as a Thread.

Important topics:

```text
Thread vs Task

Task Scheduling

ThreadPool

CPU-bound work

I/O-bound work
```

---

# 17. async / await

The project will gradually move from blocking code:

```csharp
Thread.Sleep(5000);
```

toward asynchronous code:

```csharp
await Task.Delay(5000);
```

This allows us to understand:

```text
Blocking vs Non-Blocking

Synchronous vs Asynchronous

async

await

Continuation

I/O-bound operations
```

---

# 18. Concurrency vs Parallelism

These two concepts are often confused.

We will distinguish:

```text
Concurrency

Multiple tasks making progress during overlapping periods
```

from:

```text
Parallelism

Multiple operations physically executing at the same time
```

Our queue will eventually demonstrate both.

---

# 19. CancellationToken

Long-running operations need cooperative cancellation.

We will explore:

```text
CancellationToken
CancellationTokenSource
Cancel
CancelAfter
ThrowIfCancellationRequested
IsCancellationRequested
```

Handlers may receive:

```csharp
CancellationToken cancellationToken
```

and propagate it into operations such as:

```csharp
await httpClient.SendAsync(
    request,
    cancellationToken);
```

---

# 20. Linked Cancellation Tokens

A queue operation may need to stop because of:

```text
User Cancellation

Queue Shutdown

Timeout
```

Instead of handling each separately, we can create:

```csharp
CancellationTokenSource.CreateLinkedTokenSource(...)
```

This gives us a unified cancellation model.

---

# 21. TaskCompletionSource

A queued message executes sometime in the future.

But the caller may want to await its result.

For example:

```text
Caller
   │
   ▼
Queue Message
   │
   ▼
Worker
   │
   ▼
Result
   │
   ▼
Caller continues
```

This introduces:

```text
TaskCompletionSource<T>
```

which allows infrastructure code to manually complete a `Task`.

It will become the foundation of:

```text
MessageHandle
Query Result
Message Completion
```

---

# 22. Producer / Consumer

One of the core patterns of the project:

```text
Producer
    │
    ▼
Queue
    │
    ▼
Consumer
```

Later:

```text
                  Queue
                    │
         ┌──────────┼──────────┐
         ▼          ▼          ▼
     Consumer 1 Consumer 2 Consumer 3
```

This will introduce:

```text
Multiple Consumers
Work Distribution
Ordering
Concurrency
Load
```

---

# 23. Channel<T>

Eventually manual synchronization:

```text
Queue<T>
lock
Monitor.Wait
Monitor.Pulse
```

becomes complicated.

This leads to:

```csharp
Channel<T>
```

We will explore:

```text
Channel<T>

ChannelReader<T>

ChannelWriter<T>

ReadAsync

WriteAsync

WaitToReadAsync

TryRead

TryWrite
```

---

# 24. Bounded vs Unbounded Channels

An unbounded queue can continue growing:

```text
Producer
Producer
Producer
Producer
Producer
        │
        ▼
      Queue
        │
        ▼
     Consumer
```

If producers are faster:

```text
Queue Size
10
100
1,000
10,000
100,000
...
```

Memory usage may continuously increase.

This introduces:

```text
BoundedChannelOptions
Capacity
```

---

# 25. Backpressure

Once a queue has limited capacity:

```text
Queue Capacity = 100
```

we need to decide what happens when it is full.

Possible strategies:

```text
Wait

Drop Oldest

Drop Newest

Drop Write
```

This introduces the important distributed systems concept:

> **Backpressure**

---

# 26. Multiple Workers

Later versions will move from:

```text
Queue
  │
  ▼
Worker
```

to:

```text
                   Queue
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
     Worker 1     Worker 2     Worker 3
```

This creates new problems:

```text
Ordering

Race Conditions

Shared State

Resource Contention

Duplicate Processing

Concurrency Limits
```

---

# 27. Ordering

With one worker:

```text
A
B
C
```

usually becomes:

```text
A → B → C
```

With multiple workers:

```text
A → Worker 1
B → Worker 2
C → Worker 3
```

completion may become:

```text
B → C → A
```

This forces us to discuss:

```text
FIFO

Processing Order

Completion Order

Partitioning

Ordered Consumers
```

---

# 28. Retry

External systems fail.

Our message system therefore needs:

```text
Retry
Maximum Attempt Count
Retryable Error
Non-Retryable Error
```

---

# 29. Retry Backoff

Instead of:

```text
Retry
Retry
Retry
Retry
Retry
```

we may use:

```text
Attempt 1
    │
  1 second
    ▼
Attempt 2
    │
  2 seconds
    ▼
Attempt 3
    │
  4 seconds
    ▼
Attempt 4
```

Strategies will include:

```text
Fixed Retry

Linear Backoff

Exponential Backoff

Custom Retry Policy
```

and eventually:

```text
Jitter
```

to prevent many consumers from retrying at exactly the same moment.

---

# 30. Delayed Retry

A worker should not be blocked while waiting for a retry.

Bad:

```text
Message fails
     │
     ▼
Worker sleeps 5 minutes
```

Better:

```text
Message fails
     │
     ▼
Schedule retry
     │
     └───────── 5 minutes ──────────┐
                                    │
Worker                              │
  │                                 │
  ├── Message B                     │
  ├── Message C                     │
  └── Message D                     │
                                    │
                                    ▼
                              Retry Message A
```

---

# 31. Timeout

A message may have a maximum allowed execution time.

```text
Message
   │
   ▼
Handler
   │
   │ > 5 seconds
   ▼
Timeout
```

This will help separate:

```text
Cancellation
```

from:

```text
Timeout
```

---

# 32. Circuit Breaker

Retries alone are not enough.

If a downstream API is completely unavailable:

```text
Request
   X
Retry
   X
Retry
   X
Retry
   X
```

we may be making the problem worse.

This introduces:

```text
Circuit Breaker

Closed
Open
Half-Open
```

and helps explain frameworks such as Polly.

---

# 33. Priority Queues

Not every message is equally important.

Later versions may introduce:

```text
Critical
High
Normal
Low
```

and explore:

```text
PriorityQueue<TElement, TPriority>
```

---

# 34. Scheduled and Delayed Messages

Messages may need to execute:

```text
5 minutes later

Tomorrow at 10:00

At a specific DateTime
```

This introduces:

```text
Delayed Messages
Scheduled Messages
Timers
Scheduling
```

and helps explain tools such as Hangfire and Quartz.

---

# 35. Graceful Shutdown

What happens when the application shuts down while messages are being processed?

We will explore:

```text
Stop accepting messages

Complete current work

Cancel remaining work

Drain queue

Shutdown workers

Dispose resources
```

rather than simply killing the process.

---

# 36. Persistence

In-memory messages disappear if the process crashes.

Eventually we will introduce:

```text
Persistent Queue

SQL Server

Redis

Message Store
```

and states such as:

```text
Pending

Processing

Completed

Failed
```

---

# 37. Recovery

After restart:

```text
Application starts
        │
        ▼
Read unfinished messages
        │
        ▼
Recover
        │
        ▼
Continue processing
```

This moves the architecture from an in-memory queue toward a durable messaging system.

---

# 38. Dead Letter Queue

What happens when a message has failed too many times?

Instead of deleting it:

```text
Message
   │
Retry
   X
Retry
   X
Retry
   X
   │
   ▼
Dead Letter Queue
```

This introduces:

```text
DLQ

Poison Messages

Manual Investigation

Replay
```

---

# 39. Idempotency

Retries create an important problem.

Suppose:

```text
ChargeCustomerCommand
```

runs successfully, but the acknowledgement is lost.

The message is retried.

Without protection:

```text
100 TL charge
+
100 TL charge
```

may occur.

This introduces:

> **Idempotency**

---

# 40. Deduplication

Distributed systems may deliver the same message more than once.

Using:

```text
MessageId
```

we can detect already processed messages.

This introduces:

```text
Deduplication

Processed Message Store
```

---

# 41. Delivery Semantics

As the project becomes distributed we will discuss:

```text
At-Most-Once

At-Least-Once

Exactly-Once
```

and why true exactly-once processing is much harder than it first appears.

---

# 42. Message Envelope

Business data and infrastructure metadata should be separated.

Instead of putting everything inside the business message:

```text
MessageEnvelope
│
├── MessageId
├── CorrelationId
├── CreatedAt
├── Attempt
├── Timeout
├── RetryPolicy
├── Cancellation
└── Message
```

This becomes a fundamental messaging abstraction.

---

# 43. Command / Query

Messages may represent different intentions:

```text
IMessage
   │
   ├── ICommand
   │
   └── IQuery<TResult>
```

For example:

```csharp
CreateOrderCommand
```

means:

> Do something.

while:

```csharp
GetOrderQuery
```

means:

> Return something.

---

# 44. Message Dispatcher

Instead of:

```csharp
switch (message.Type)
{
    ...
}
```

we will introduce:

```text
Message
    │
    ▼
Dispatcher
    │
    ▼
Handler
```

This reduces coupling between infrastructure and business logic.

---

# 45. Custom Queues

The infrastructure should not require:

```text
RefundQueue class
EmailQueue class
ReportQueue class
PaymentQueue class
```

Instead:

```csharp
bus.CreateQueue("commands");

bus.CreateQueue("queries");

bus.CreateQueue("background");
```

or:

```csharp
bus.CreateQueue("payments");
bus.CreateQueue("notifications");
```

The important principle is:

> **The domain uses the queue infrastructure.  
> The queue infrastructure does not know the domain.**

---

# 46. CorrelationId

When one operation produces multiple messages:

```text
HTTP Request
CorrelationId = ABC
        │
        ▼
CreateOrderCommand
CorrelationId = ABC
        │
        ▼
PaymentCommand
CorrelationId = ABC
        │
        ▼
SendEmailCommand
CorrelationId = ABC
```

we need a way to follow the entire operation.

This introduces:

```text
CorrelationId
CausationId
MessageId
```

---

# 47. Observability

As the system grows, we need answers to questions such as:

```text
How many messages are waiting?

How many failed?

How many retries occurred?

How long does processing take?

Which handler is slow?

Which queue is overloaded?
```

This introduces:

```text
Structured Logging

Metrics

Tracing

OpenTelemetry
```

---

# 48. Distributed Messaging

Eventually the queue leaves the process.

Instead of:

```text
Application
    │
    ▼
In-Memory Queue
```

we move toward:

```text
Order Service
      │
      ▼
   RabbitMQ
      │
   ┌──┴─────────────┐
   ▼                ▼
Payment Service  Notification Service
```

This introduces a completely new set of concurrency and reliability problems.

---

# 49. RabbitMQ

The project will eventually compare our in-memory implementation with a real message broker.

Concepts will include:

```text
Producer

Consumer

Exchange

Queue

Binding

Routing Key

Acknowledgement

Nack

Requeue

Prefetch

Dead Letter Exchange
```

The goal is to understand **why RabbitMQ provides these concepts**, not simply how to configure them.

---

# 50. Outbox Pattern

A classic distributed systems problem:

```text
Save database record
        │
        ✓
Publish message
        │
        X
```

Now the database says the operation succeeded but no message was published.

This introduces:

> **Transactional Outbox Pattern**

---

# 51. Inbox Pattern

Consumers also need protection against duplicate delivery.

This introduces:

```text
Inbox

Processed Message Tracking

Deduplication

Idempotent Consumer
```

---

# 52. Eventual Consistency

Once multiple services own different databases:

```text
Order DB

Payment DB

Notification DB
```

they cannot always update everything inside one transaction.

This introduces:

> **Eventual Consistency**

and prepares the architecture for real microservice messaging.

---

# Evolution Roadmap

| Version | Stage | Status | Main Focus |
|---|---|---|---|
| V1 | `01-PrimitiveQueue` | ✅ Completed | Thread, Queue, lock, Monitor, Producer / Consumer |
| V2 | `02-GenericMessageBus` | 🚧 Current | Task, async/await, Channel, ConcurrentDictionary, Command / Query, Retry, Cancellation |
| V3 | `03-ResilientMessageBus` | ⏳ Planned | Retry Policies, Backoff, Timeout, Circuit Breaker |
| V4 | `04-ConcurrentMessageBus` | ⏳ Planned | Multiple Workers, SemaphoreSlim, Race Conditions, Ordering |
| V5 | `05-AdvancedQueueing` | ⏳ Planned | Priority, Scheduling, Delayed Messages, Backpressure |
| V6 | `06-PersistentMessageBus` | ⏳ Planned | Persistence, Recovery, Durable Messages |
| V7 | `07-ReliableMessageBus` | ⏳ Planned | DLQ, Idempotency, Deduplication, Delivery Semantics |
| V8 | `08-ObservableMessageBus` | ⏳ Planned | CorrelationId, Metrics, Logging, Tracing, OpenTelemetry |
| V9 | `09-DistributedMessageBus` | ⏳ Planned | RabbitMQ, Producers, Consumers, Acknowledgements |
| V10 | `10-ProductionMessagingPlatform` | ⏳ Planned | Outbox, Inbox, Eventual Consistency, Production Architecture |

> ## Current Stage
>
> 🚧 **V2 — GenericMessageBus**

---

# V1 — PrimitiveQueue

The first implementation intentionally uses low-level primitives:

```text
Queue<T>

Thread

lock

Monitor.Wait

Monitor.Pulse
```

The goal is to understand how Producer / Consumer communication works before replacing it with higher-level abstractions.

Conceptually:

```text
Producer
   │
   │ Enqueue
   ▼
┌─────────────┐
│  Queue<T>   │
└──────┬──────┘
       │
       │ Dequeue
       ▼
    Worker
       │
       ▼
    Execute
```

The worker sleeps while no work exists:

```csharp
Monitor.Wait(...)
```

and the producer wakes the worker:

```csharp
Monitor.Pulse(...)
```

V1 intentionally contains more domain-specific and duplicated implementations.

They allow us to discover problems such as:

```text
Domain Coupling

Duplicate Retry Logic

Manual Synchronization

Message Type Switches

Blocking Operations

Thread Management
```

These problems create V2.

---

# V2 — GenericMessageBus

> 🚧 **Current Version**

V2 begins moving away from domain-specific queues and manual low-level processing.

Important concepts introduced include:

```text
IMessage

ICommand

IQuery<TResult>

MessageEnvelope

MessageHandle

MessageDispatcher

WorkQueue

MessageBus

Task

async / await

Channel<T>

ConcurrentDictionary

CancellationToken

TaskCompletionSource<T>

RetryPolicy

Timeout

Bounded Queues

Backpressure
```

The architecture begins to evolve toward:

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

# Why Console Applications?

The early stages intentionally use Console Applications.

The Console Application is only a lightweight host used to observe the messaging architecture.

It allows us to focus on:

```text
Threading

Synchronization

Concurrency

async / await

Queues

Workers

Retries

Cancellation
```

without introducing unrelated concerns such as:

```text
Controllers

HTTP

Swagger

Authentication

UI

Web Framework Configuration
```

Later versions will naturally introduce:

```text
ASP.NET Core

Worker Services

SQL Server

RabbitMQ

OpenTelemetry

Multiple Services
```

when the architecture actually requires them.

---

# What This Repository Is Not

This repository is not intended to replace:

- RabbitMQ
- MassTransit
- Kafka
- Azure Service Bus
- MediatR
- Hangfire
- Polly

It is an educational architecture experiment designed to understand **why systems like these need the abstractions they provide**.

Instead of memorizing:

```text
Retry
DLQ
Acknowledgement
Idempotency
Outbox
Backpressure
Consumer
Dispatcher
Circuit Breaker
```

we want to naturally encounter the problems that require them.

---

# Final Goal

We start here:

```text
Queue<T>
   +
Thread
   +
lock
   +
Monitor
```

and gradually move toward:

```text
Message Bus
   │
   ├── Commands / Queries
   ├── Multiple Consumers
   ├── Retry / Backoff
   ├── Cancellation / Timeout
   ├── Backpressure
   ├── Persistence
   ├── Dead Letter Queue
   ├── Idempotency
   ├── Observability
   ├── RabbitMQ
   ├── Outbox / Inbox
   └── Eventual Consistency
```

But the final implementation is not the most important part.

The important part is understanding **why every step became necessary**.

---

# Core Idea

> **Do not start with the abstraction. Start with the problem that creates the abstraction.**

And most importantly:

> **The queue is the project. Concurrency is the subject.**

We are not learning concurrency just to build a queue.

**We are building a queue so that we can understand concurrency.**

---

# Status

🚧 **Work in Progress**

Current stage:

```text
V2 — GenericMessageBus
```

Older implementations are intentionally kept in the repository.

Each version should make it possible to compare:

```text
Previous Solution
        │
        ▼
New Problem
        │
        ▼
New Concept
        │
        ▼
Better Architecture
```

The destination matters.

But in this repository:

> **The evolution is the actual lesson.**
