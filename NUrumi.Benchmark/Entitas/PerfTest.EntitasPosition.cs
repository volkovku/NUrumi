using System.Numerics;
using IComponent = Entitas.IComponent;

public partial class PerfTest
{
    public class EntitasPosition : IComponent
    {
        public Vector2 Value;
    }
}