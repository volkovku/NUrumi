namespace NUrumi.Test.Model
{
    public sealed class TestRegistry : Registry<TestRegistry>
    {
        public TestComponent Test;
        public Position Position;
        public Velocity Velocity;
        public Parent Parent;
    }
}