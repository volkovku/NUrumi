//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Numerics;

public partial class GameEntity {

    public PerfTest.EntitasPosition perfTestEntitasPosition { get { return (PerfTest.EntitasPosition)GetComponent(GameComponentsLookup.PerfTestEntitasPosition); } }
    public bool hasPerfTestEntitasPosition { get { return HasComponent(GameComponentsLookup.PerfTestEntitasPosition); } }

    public void AddPerfTestEntitasPosition(Vector2 newValue) {
        var index = GameComponentsLookup.PerfTestEntitasPosition;
        var component = (PerfTest.EntitasPosition)CreateComponent(index, typeof(PerfTest.EntitasPosition));
        component.Value = newValue;
        AddComponent(index, component);
    }

    public void ReplacePerfTestEntitasPosition(Vector2 newValue) {
        var index = GameComponentsLookup.PerfTestEntitasPosition;
        var component = (PerfTest.EntitasPosition)CreateComponent(index, typeof(PerfTest.EntitasPosition));
        component.Value = newValue;
        ReplaceComponent(index, component);
    }

    public void RemovePerfTestEntitasPosition() {
        RemoveComponent(GameComponentsLookup.PerfTestEntitasPosition);
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public sealed partial class GameMatcher {

    static Entitas.IMatcher<GameEntity> _matcherPerfTestEntitasPosition;

    public static Entitas.IMatcher<GameEntity> PerfTestEntitasPosition {
        get {
            if (_matcherPerfTestEntitasPosition == null) {
                var matcher = (Entitas.Matcher<GameEntity>)Entitas.Matcher<GameEntity>.AllOf(GameComponentsLookup.PerfTestEntitasPosition);
                matcher.componentNames = GameComponentsLookup.componentNames;
                _matcherPerfTestEntitasPosition = matcher;
            }

            return _matcherPerfTestEntitasPosition;
        }
    }
}
