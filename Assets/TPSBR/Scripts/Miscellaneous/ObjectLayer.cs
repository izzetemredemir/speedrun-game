using UnityEngine;

namespace TPSBR
{
	public static class ObjectLayer
	{
		public static int Default      { get; private set; }
		public static int Agent        { get; private set; }
		public static int AgentKCC     { get; private set; }
		public static int Projectile   { get; private set; }
		public static int Target       { get; private set; }
		public static int Interaction  { get; private set; }
		public static int Pickup       { get; private set; }

		static ObjectLayer()
		{
			Default          = LayerMask.NameToLayer("Default");
			Agent            = LayerMask.NameToLayer("Agent");
			AgentKCC         = LayerMask.NameToLayer("AgentKCC");
			Projectile       = LayerMask.NameToLayer("Projectile");
			Target           = LayerMask.NameToLayer("Target");
			Interaction      = LayerMask.NameToLayer("Interaction");
			Pickup           = LayerMask.NameToLayer("Pickup");
		}
	}

	public static class ObjectLayerMask
	{
		public static LayerMask Default              { get; private set; }
		public static LayerMask Agent                { get; private set; }
		public static LayerMask Target               { get; private set; }
		public static LayerMask BlockingProjectiles  { get; private set; }
		public static LayerMask Environment          { get; private set; }

		static ObjectLayerMask()
		{
			Default              = 1 << ObjectLayer.Default;
			Agent                = 1 << ObjectLayer.Agent;
			Target               = 1 << ObjectLayer.Target;

			Environment = Default;
			BlockingProjectiles  = Default | Agent | Target;
		}
	}
}
