# Cleipnir = Persistent Programming in .NET = Sagas Simplified 

Cleipnir is a .NET (still experimental) framework allowing persistent programming on the .NET platform.

![alt text](http://cleipnir.net/logo.png "Our logo") 

## Background
As part of my (now failed) PhD I investigated how persistent programming could simplify the implementation of distributed systems and in particular consensus algorithms such as Paxos and Raft. I think there is a potential for persistent programming that has still not been reached in industry. Especially, for the Saga microservice pattern. Hang in there and see if you agree :)

## What is Persistent Programming?
Persistent programming is a programming paradigm where the process state is persisted automatically by the runtime at well-coordinated points in time. Furthermore, the runtime will automatically reinstate the state of the process when it is restarted. Crucially, this occurs without explicitly instructed so in application code.

### Ping-Pong Example
To clarify, consider the toy example of Pinger and Ponger, where each performs the following:

#### Pinger
1. Waits a second
2. Sends a message “Ping” to Ponger
3. Waits for a reply from Ponger afterwards restart form step 1.
#### Ponger 
1. Waits for a “Ping” from Pinger
2. Send a “Pong” to Pinger restart from step 1.

The functionality can be implemented using two classes and a main method. 
```csharp
class Pinger : IPropertyPersistable
{
  public Source<string> Messages { get; set; }
  public int Count { get; set; }

  public async CTask Start()
  {
    while (true)
    {
      await Sleep.Until(1000);
      Messages.Emit($"PING {Count++}");
      var msg = await Messages.Where(m => m.StartsWith("PONG")).Next();
      Console.WriteLine("PINGER: " + msg);
    }
  }
}
```
```csharp
class Ponger : IPropertyPersistable
{
  public Source<string> Messages { get; set; }
  public int Count { get; set; }

  public async CTask Start()
  {
    while (true)
    {
      var msg = await Messages.Where(s => s.StartsWith("PING")).Next();
      Console.WriteLine($"PONGER: {msg}" );
      await Sleep.Until(1000);
      Messages.Emit($"PONG {Count++}");
    }
  }
}
```
```csharp
public static void Main()
{
  var storage = new SimpleFileStorageEngine(@".\PingPong.txt", deleteExisting: true);
  //you can also use new SqlServerStorageEngine(...) and new InMemoryStorageEngine()
  var engine = ExecutionEngineFactory.StartNew(storage);
  engine.Schedule(() =>
  {
     var messages = new Source<string>();
     var pinger = new Pinger { Messages = messages };
     var ponger = new Ponger { Messages = messages };
     
     _ = ponger.Start();
     _ = pinger.Start();
   });

  while (true)
  {
    Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
    Console.ReadLine();

    engine.Dispose();

    Console.WriteLine("PRESS ENTER TO START PING PONG APP");
    Console.ReadLine();
     
    engine = ExecutionEngineFactory.Continue(storage);
   }
}
```

When executing the toy example the process state is persisted when hitting enter the first time and subsequently loaded from persistent storage when hitting enter the second time. Thus, after the process state has persisted the first time. You can continue the process execution across restarts by changing the Main-method as follows:

```csharp
public static void Main()
{
  var storage = new SimpleFileStorageEngine(@".\PingPong.txt", deleteExisting: false);
  var scheduler = ExecutionEngineFactory.Continue(storage);
          
  while (true)
  {
    Console.WriteLine("PRESS ENTER TO STOP PING PONG APP");
    Console.ReadLine();

    scheduler.Dispose();

    Console.WriteLine("PRESS ENTER TO START PING PONG APP");
    Console.ReadLine();

    scheduler = ExecutionEngineFactory.Continue(storage);
  }
}
```

