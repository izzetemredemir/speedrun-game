using Fusion;

namespace TPSBR
{
	public interface IContextBehaviour
	{
		SceneContext Context { get; set; }
	}

    public abstract class ContextBehaviour : NetworkBehaviour, IContextBehaviour
    {
		public SceneContext Context { get; set; }
    }

	public abstract class ContextSimulationBehaviour : SimulationBehaviour, IContextBehaviour
    {
		public SceneContext Context { get; set; }
    }

    public abstract class ContextAreaOfInterestBehaviour : NetworkAreaOfInterestBehaviour, IContextBehaviour
    {
		public SceneContext Context { get; set; }
    }
}
