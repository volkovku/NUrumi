# NUrumi

ECS engine with aiming to extensibility and flexibility like an [Entitas](https://github.com/sschmid/Entitas) and
performance near the [LeoEcsLite](https://github.com/Leopotam/ecslite) project.

__*NUrumi*__ project is free from dependencies and code generation.

## Content

* [Core concepts](#Core-concepts)
* [Basics](#Basics)
* [Groups](#Groups)
* [Collector](#Collector)
* [Relationships](#Relationships)
    * [Assign component multiple times](#Assign-component-multiple-times)
    * [Hierarchies](#Hierarchies)
* [Fields](#Fields)
    * [Field](#Field)
    * [RefField](#RefField)
    * [ReactiveField](#ReactiveField)
    * [IndexField](#IndexField)

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
    
    // Prints true because thor has a name
    Console.WriteLine(thor.Has(context.Registry.PlayerName));

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
    
    // Prints true because thor has a name
    Console.WriteLine(thor.Has(playerName));

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

You can iterate over collected entities. Mutations in loop will not break it.

```csharp
foreach (var entity in collector)
{
    // Do something ...
}
```

:warning: One important thing to known is that collector just collects entities which
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

## Relationships

__*NUrumi*__ provides a __*Relationships*__. This mechanism provides two major possibilities:

* allows to assign component multiple times
* allows to build hierarchies

### Assign component multiple times

```csharp
// Define registry wich contains Likes relationship
public class RelationRegistry : Registry<RelationRegistry>
{
    public Likes Likes;
}

// Define relationship
public class Likes : Relation<Likes>
{
}

// Common boilerplate code
var context = new Context<RelationRegistry>();
var likes = context.Registry.Likes;

// Define entities
var alice = context.CreateEntity();

var apples = context.CreateEntity();
var burgers = context.CreateEntity();
var sweets = context.CreateEntity();

// Assing relationships
alice.Add(likes, apples);
alice.Add(likes, burgers);
alice.Add(likes, sweets);

// Get relationship; returns [apples, burgers, sweets]
var aliceLikes = alice.Relationship(likes);

// Get reverse-relationship; returns [alice]
var whoLikeApples = apples.Target(likes);

// Check is relationship exists
alice.Has(likes, apples);
apples.Targets(likes, alice);

// Remove relationship
alice.Remove(likes, apples);

// When entity removed all relationships will removed automatically
context.RemoveEntity(alice);
var whoLikeSweets = sweets.Target(likes); // returns []
```

### Hierarchies

Relationships is an easy way to organize hierarchies. Lets see how we can create parent-child relationship.

```csharp
public class GameRegistry : Registry<GameRegistry>
{
    public ChildOf ChildOf;
}

public class ChildOf : Relation<ChildOf>
{
}

// Common boilerplate code
var context = new Context<RelationRegistry>();
var childOf = context.Registry.ChildOf;

// Create hierarchy
var parent = context.CreateEntity();
var child1 = context.CreateEntity().Add(childOf, parent);
var child2 = context.CreateEntity().Add(childOf, parent);

// Get all children of parent
var children = parent.Target(childOf); // [child1, child2]

// Get parent
var childParent = child1.Relationship(childOf).Single();
```

## Fields

__*NUrumi*__ provides a wide range of predefined fields for different purposes.

### Field

__*Field*__ is a simplest and most performant way to store data. __*Field*__ have only one restriction
it can not store class, or structs which contains class. __*Field*__ is a perfect
candidate for store positions, velocity, rotations and etc.

__*Field*__ provides common methods:

* ```TValue Get(int entityId)```
* ```bool TryGet(int entityId, out TValue result)```
* ```void Set(int entityId, TValue value)```

Also __*Field*__ provides high performance methods:

* ```ref TValue GetRef(int entityId)```
* ```ref TValue GetOrAdd(int entityId)```
* ```ref TValue GetOrSet(int entityId, TValue value)```

Common scenario of __*Field*__ usage:

```csharp
// Boilerplate
var context = new Context<GameRegistry>();
var position = context.Registry.Position.Value; 
var velocity = context.Registry.Velocity.Value; 
var group = context.CreateGroup(GroupFilter
    .Include(position)
    .Include(velocity));

// Some system executes something like this
foreach (var entity in group)
{
  ref var pos = ref entity.GetRef(position);
  ref var vel = ref entity.GetRef(velocity);
  pos.Value += vel;
}
```

### RefField

__*RefField*__ is a sister of __*Field*__. It allows to store reference type values.
As a handicap her hasn't high performance methods like a __*Field*__.

__*RefField*__ provides methods:

* ```TValue Get(int entityId)```
* ```bool TryGet(int entityId, out TValue result)```
* ```void Set(int entityId, TValue value)```

### ReactiveField

__*ReactiveField*__ can hold pure structures and track value changes.

To track value changes your can subscribe on OnValueChanged event.

__*ReactiveField*__ provides events:

```csharp
OnValueChanged(
    IComponent component,     // A component which field was changed
    IField field,             // A flied which values was changed
    int entityId,             // An identifier of entity which value was changed
    TValue? oldValue,         // The old value
    TValue newValue)          // The new value
```

__*ReactiveField*__ provides methods:

* ```TValue Get(int entityId)```
* ```bool TryGet(int entityId, out TValue result)```
* ```void Set(int entityId, TValue value)```

### IndexField

An __*IndexField*__ provides a one-to-many relationship.
It's alternative way to organize some kind of hierarchies based on external identifiers or composite keys.

```csharp
class TestRegistry : Registry<TestRegistry>
{
    public Parent Parent;
}

class Parent : Component<Parent>
{
    public IndexField<int> Value;
}

// Boilerplate
var context = new Context<TestRegistry>();
var parentComponent = context.Registry.Parent;
var parent = parentComponent.Value;

// Create hierarhy
var parentEntity = context.CreateEntity();

var childEntity1 = context.CreateEntity();
childEntity1.Set(parent, parentEntity);

var childEntity2 = context.CreateEntity();
childEntity2.Set(parent, parentEntity);

var childEntity3 = context.CreateEntity();
childEntity3.Set(parent, parentEntity);

// Get parent children
var children = parent.GetEntitiesAssociatedWith(parentEntity);
```
