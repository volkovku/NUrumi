using System.Numerics;
using IComponent = Entitas.IComponent;

public partial class PerfTest
{
    public class EntitasVelocity : IComponent
    {
        public Vector2 Value;
    }
}