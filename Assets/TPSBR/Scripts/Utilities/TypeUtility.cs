using System;
using System.Collections.Generic;

namespace TPSBR
{
	using System.Reflection;

	public static partial class TypeUtility
	{
		public static List<Assembly> GetDefaultAssemblies()
		{
			List<Assembly> assemblies = new List<Assembly>();
			assemblies.Add(typeof(TypeUtility).Assembly);
			assemblies.Add(typeof(Fusion.ILogger).Assembly);
			assemblies.Add(typeof(Fusion.SessionProperty).Assembly);
			assemblies.Add(typeof(Fusion.Accuracy).Assembly);
			assemblies.Add(typeof(Fusion.Sockets.NetAddress).Assembly);
			return assemblies;
		}

		public static void DumpTypes(IList<Assembly> assemblies = null)
		{
			if (assemblies == null)
			{
				assemblies = new Assembly[] { typeof(TypeUtility).Assembly };
			}

			List<string> list = new List<string>(4096);

			for (int i = 0; i < assemblies.Count; ++i)
			{
				Type[] types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; ++j)
				{
					list.Add(types[j].FullName);
				}
			}

			list.Sort(StringComparer.Ordinal);

			string fileID        = $"{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
			string statsFileName = $"Types_{fileID}.txt";

			StatsRecorder statsRecorder = new StatsRecorder();
			statsRecorder.Initialize(ApplicationUtility.GetFilePath(statsFileName), 1);

			for (int i = 0; i < list.Count; ++i)
			{
				statsRecorder.Write($"{list[i]}");
			}

			statsRecorder.Deinitialize();
		}
	}
}
