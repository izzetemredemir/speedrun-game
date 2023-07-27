using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class ColorTag : SimulationBehaviour, ISpawned
{
	public Color ServerColor = Color.green;
	public Color ClientColor = Color.red;

	void ISpawned.Spawned()
	{
		var r = GetComponent<Renderer>();
		r.material.color = Runner.Simulation.IsClient ? ClientColor : ServerColor;
	}
}
