# NUrumi

ECS engine with aiming to extensibility and flexibility like an [Entitas](https://github.com/sschmid/Entitas) and
performance near the [LeoEcsLite](https://github.com/Leopotam/ecslite) project.

__*NUrumi*__ project is free from dependencies and code generation.

## Content

* [Core concepts](#Core-concepts)
* [Basics](#Basics)
* [Groups](#Groups)

## Core concepts

Like an other ECS engines __*NUrumi*__ lays on __*Entities*__ and __*Components*__.
__*NUrumi*__ doesn't provides systems because their implementation is trivial and varies from
project to project.

Entity is a logic union of components.
In __*NUrumi*__ entity represented as integer value also known as identifier which can be associated with components.

All entities lives in context. Each context holds entities and their components.
A components allowed in context specified in special class called registry.

Component is a part of __*entity*__. In __*NUrumi*__ component is a composition of fields.
Each component field in __*NUrumi*__ has special type like __*Field*__, __*RefField*__,
__*IndexField*__, __*ReactiveField*__. It allows to balance between performance and
flexibility. For simple structs and primitive types you can use __*Field*__ which provides
best performance. But if you want to track field value changes you can use __*ReactiveField*__
which of course has less performance as __*Field*__. As a developer you can choose what
you need in any situation. Types of fields will be explained in next sections.

## Basics

Lest see some code

```csharp

// Registry of our game which containns a set of registered components
public class GameRegistry : Registry<GameRegistry>
{
    public PositionComponent Position;
    public VelocityComponent Velocity;
    public PlayerNameComponent PlayerName;
}

// A component to hold entity position
public class PositionComponent : Component<PositionComponent>
{
    public Field<Vector3> Value;
}

// A component to hold entity velocity
public class VelocityComponent : Component<VelocityComponent>
{
    public Field<Vector3> Value;
}

// A component to hold player name
public class PlayerNameComponent : Component<PlayerNameComponent>
{
    public RefField<string> Value;
}

// An example function
void Main()
{
    // Create new game context
    var context = new Context<GameRegistry>();

    // Cache fields to reduce noise in code
    var playerName = context.Registry.PlayerName.Value; 
    var position = context.Registry.Position.Value; 
    var velocity = context.Registry.Velocity.Value; 
    
    // Create new entity in context whith components
    var thor = context
        .CreateEntity()
        .Set(playerName, "Thor")
        .Set(position, Vector3.Zero)
        .Set(velocity, Vector3.Forward);

    // Print entity name
    Console.WriteLine(thor.Get(playerName));
    
    // For strucure type Field class provies performance efficient methods
    ref var thorPosition = ref thor.GetRef(position);
    ref var thorVelocity = ref thor.GetRef(velocity);
    position.Value += thorVelocity;
    
    // Removes velocity from entity
    thor.Remove(velocity);
    
    // Removes an entity with all it components 
    context.RemoveEntity(thor);
}

```

As you can see usage of __*NUrumi*__ is easy but some code looks like a noise.
Components which contains only one field can be reduced to form
of ```Component<TComponent>.Of<TValue>``` or ```Component<TComponent>.OfRef<TValue>```.
Our example can be rewrote to:

```csharp

// Registry of our game which containns a set of registered components
public class GameRegistry : Registry<GameRegistry>
{
    public PositionComponent Position;
    public VelocityComponent Velocity;
    public PlayerNameComponent PlayerName;
}

// A component to hold entity position
public class PositionComponent : Component<PositionComponent>.Of<Vector3> {}

// A component to hold entity velocity
public class VelocityComponent : Component<VelocityComponent>.Of<Vector3> {}

// A component to hold player name
public class PlayerNameComponent : Component<PlayerNameComponent>.OfRef<string> {}

// An example function
void Main()
{
    // Create new game context
    var context = new Context<GameRegistry>();

    // Cache fields to reduce noise in code
    var playerName = context.Registry.PlayerName; 
    var position = context.Registry.Position; 
    var velocity = context.Registry.Velocity; 
    
    // Create new entity in context whith components
    var thor = context
        .CreateEntity()
        .Set(playerName, "Thor")
        .Set(position, Vector3.Zero)
        .Set(velocity, Vector3.Forward);

    // Print entity name
    Console.WriteLine(thor.Get(playerName));
    
    // For strucure types Field class provies performance efficient methods
    ref var thorPosition = ref thor.GetRef(position);
    ref var thorVelocity = ref thor.GetRef(velocity);
    
    // Mutate entity position
    position.Value += thorVelocity;
    
    // Removes velocity from entity
    thor.Remove(velocity);
    
    // Removes an entity with all it components 
    context.RemoveEntity(thor);
}

```

## Groups

Groups provides a fast way to iterate over an entities with specific subset of components.

```csharp

// Create a group which contains all entities in a context with 
// Position and Velocity components
var group = context.CreateGroup(GroupFilter
    .Include(position)
    .Include(velocity));
 
// Iterate over all entities in the group
foreach (var entity in group)
{
    ref var entityPosition = ref entity.GetRef(position);
    ref var entityVelocity = ref entity.GetRef(velocity);
    entityPosition.Value += entityVelocity;
}

```

Subset of components in group can be specified with ```GroupFilter``` and
their methods ```Include(component)``` and ```Exclude(component)```.

Groups in __*NUrumi*__ contains cached entities. So you can iterate over group
without addition costs.

In some cases you may want to known when entities added or removed from group.
You can achieve it via group event ```OnGroupChanged(int entityIndex, bool add)```.

```csharp

// Subscribe on group changes
group.OnGroupChanged += (entity, add) => 
{
    Console.WriteLine($"Entity #{entity} was {(add ? "added" : "removed")}");
}

```

One more ability of __*NUrumi*__ groups is that you can mutate it entities
in iteration progress. All mutation will be accumulated through iteration process
and applied when iteration was finished. So you don't need to create some temp
buffers to interact with you entities.

## Collector

Collector provides an easy way to collect changes in a group or reactive fields over time.

```csharp

public class GameRegistry : Registry<GameRegistry>
{
    public HealthComponent Health;
    public PositionComponent Position;
    public VelocityComponent Velocity;
    ...
}

public class HealthComponent : Component<HealthComponent>
{
    public ReactiveField<int> Value;
}

var context = new Context<TestRegistry>();
var position = context.Registry.Position;
var velocity = context.Registry.Velocity;
var health = context.Registry.Health.Value;

var group = context.CreateGroup(GroupFilter
    .Include(position)
    .Include(velocity));
    
var collector = context
    .CreateCollector()
    .WatchEntitiesAddedTo(group)
    .WatchChangesOf(health);
    
// After this operation entity1 will be in collector
var entity1 = context
    .CreateEntity()
    .Set(position, /* some position */)
    .Set(velocity, /* some velocity */);
    
// After this operation entity2 will be in collector
var entity2 = context.CreateEntity().Set(health, 100);

// Collected results can be dropped by Clear method
// after that you can collect changes of next iteration
collector.Clear();

```
:warning:

One important thing to known is that collector just collects entities which
was touched but collector does not check actual state of entity. So if you track
entities added to some group but other part of code removes entity from this group
entity will present in collector until it will be clear. 

For example:

```csharp

var group = context.CreateGroup(GroupFilter
    .Include(position)
    .Include(velocity));

var collector = context
    .CreateCollector()
    .WatchEntitiesAddedTo(group);
    
// This code adds entitity to group
entity
    .Set(position, /* some position */)
    .Set(velocity, /* some velocity */);
    
// This code prints true
Console.WriteLine(collector.Has(entity));

// This code removes entitity from group
entity.Remove(position);

// This code prints true
Console.WriteLine(collector.Has(entity));

// Clear collector
collector.Clear();

// This code prints false
Console.WriteLine(collector.Has(entity));

```