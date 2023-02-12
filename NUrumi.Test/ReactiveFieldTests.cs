using System;
using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class ReactiveFieldTests
    {
        [Test]
        public void ReactiveFieldTest()
        {
            var context = new Context<TestRegistry>();
            var component = context.Registry.ReactiveComponent;
            var changes = 0;

            int changedEntityId = default;
            int? changedValue = default;
            int assignedValue = default;

            // ReSharper disable once ConvertToLocalFunction
            ReactiveField<int>.OnReactiveFieldValueChangedEventHandler subscription =
                (changedComponent, changedField, id, oldValue, newValue) =>
                {
                    Assert.AreEqual(component, changedComponent);
                    Assert.AreEqual(component.ReactiveField, changedField);
                    changes += 1;
                    changedEntityId = id;
                    changedValue = oldValue;
                    assignedValue = newValue;
                };

            component.ReactiveField.OnValueChanged += subscription;

            var testReactiveField = new Action<int, int>((entityId, newValue) =>
            {
                var changesBefore = changes;
                var valueBefore = entityId.TryGet(component.ReactiveField, out var prev) ? (int?) prev : null;
                entityId.Set(component.ReactiveField, newValue);
                changedEntityId.Should().Be(entityId);
                changedValue.Should().Be(valueBefore);
                assignedValue.Should().Be(newValue);
                changes.Should().Be(changesBefore + 1);
            });

            // It should fire event when reactive field is changed
            var rnd = new Random();
            var entity1 = context.CreateEntity();
            var entity2 = context.CreateEntity();
            var entity3 = context.CreateEntity();
            var entity4 = context.CreateEntity();
            var entities = new[] {entity1, entity2, entity3, entity4};
            for (var i = 0; i < 100; i++)
            {
                var entityId = entities[Math.Abs(rnd.Next()) % entities.Length];
                testReactiveField(entityId, rnd.Next());
            }

            // It should not fire event if not reactive field changed
            var lastChanges = changes;
            for (var i = 0; i < 100; i++)
            {
                var entityId = entities[Math.Abs(rnd.Next()) % entities.Length];
                entityId.Set(component.SimpleField, rnd.Next());
                changes.Should().Be(lastChanges);
            }

            // It should not fire event if subscription was cancelled
            component.ReactiveField.OnValueChanged -= subscription;
            for (var i = 0; i < 100; i++)
            {
                var entityId = entities[Math.Abs(rnd.Next()) % entities.Length];
                entityId.Set(component.ReactiveField, rnd.Next());
                changes.Should().Be(lastChanges);
            }
        }

        private class TestRegistry : Registry<TestRegistry>
        {
            public ReactiveComponent ReactiveComponent;
        }

        private class ReactiveComponent : Component<ReactiveComponent>
        {
            public Field<int> SimpleField;
            public ReactiveField<int> ReactiveField;
        }
    }
}