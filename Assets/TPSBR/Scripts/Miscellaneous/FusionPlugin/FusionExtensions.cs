namespace Fusion.Plugin
{
	using System.Reflection;

	public static class FusionExtensions
	{
		public static NetworkAreaOfInterestBehaviour GetAOIPositionSource(this NetworkObject networkObject)
		{
			return networkObject.AoiPositionSource;
		}

		public static double GetRTT(this Simulation simulation)
		{
			return simulation is Simulation.Client client ? client.RttToServer : 0.0;
		}

		public static void SetLocalPlayer(this Simulation simulation, PlayerRef playerRef)
		{
			if (simulation is Simulation.Client client)
			{
				// Hack - Local player is reset back after disconnect, otherwise exceptions are thrown all over the code because Object.HasStateAuthority == true on proxies
				// This shouldn't be harmful as NetworkRunner gets destroyed anyway.
				typeof(Simulation.Client).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, playerRef);
			}
		}

		public static string GetLocalAddress(this Simulation simulation)
		{
			return simulation.LocalAddress.NativeAddress.ToString();
		}

		public static string GetOrderSorterNodes()
		{
			string str     = "";
			var    sorter  = new OrderSorter();
			var    results = sorter.Run();

			foreach (var result in results)
			{
				str += $"{result.Type.Name}\n";
			}

			return str;
		}
	}
}
