using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fusion.Animations
{
	public static partial class ReflectionUtility
	{
		public static List<Type> GetInheritedTypes(Type baseType, bool includeAbstract = true)
		{
			return GetInheritedTypes(baseType, new Assembly[] { baseType.Assembly }, includeAbstract);
		}

		public static List<Type> GetInheritedTypes(Type baseType, Assembly assembly, bool includeAbstract = true)
		{
			return GetInheritedTypes(baseType, new Assembly[] { assembly }, includeAbstract);
		}

		public static List<Type> GetInheritedTypes(Type baseType, Assembly[] assemblies, bool includeAbstract = true)
		{
			List<Type> inheritedTypes = new List<Type>(16);

			for (int j = 0, assemblyCount = assemblies.Length; j < assemblyCount; ++j)
			{
				Type[] assemblyTypes = assemblies[j].GetTypes();
				for (int i = 0, count = assemblyTypes.Length; i < count; ++i)
				{
					Type assemblyType = assemblyTypes[i];
					if (assemblyType.IsSubclassOf(baseType) == true)
					{
						if (includeAbstract == false && assemblyType.IsAbstract == true)
							continue;

						inheritedTypes.Add(assemblyType);
					}
				}
			}

			return inheritedTypes;
		}

		public static List<Type> GetAssignableTypes(Type baseType, bool includeAbstract = true)
		{
			return GetAssignableTypes(baseType, new Assembly[] { baseType.Assembly }, includeAbstract);
		}

		public static List<Type> GetAssignableTypes(Type baseType, Assembly assembly, bool includeAbstract = true)
		{
			return GetAssignableTypes(baseType, new Assembly[] { assembly }, includeAbstract);
		}

		public static List<Type> GetAssignableTypes(Type baseType, Assembly[] assemblies, bool includeAbstract = true)
		{
			List<Type> assignableTypes = new List<Type>(16);

			for (int j = 0, assemblyCount = assemblies.Length; j < assemblyCount; ++j)
			{
				Type[] assemblyTypes = assemblies[j].GetTypes();
				for (int i = 0, count = assemblyTypes.Length; i < count; ++i)
				{
					Type assemblyType = assemblyTypes[i];
					if (baseType.IsAssignableFrom(assemblyType) == true)
					{
						if (includeAbstract == false && assemblyType.IsAbstract == true)
							continue;

						assignableTypes.Add(assemblyType);
					}
				}
			}

			return assignableTypes;
		}
	}
}
