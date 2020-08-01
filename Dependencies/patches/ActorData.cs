using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Fabric;
using UnityEngine;
using UnityEngine.Networking;

public class ActorData : NetworkBehaviour, IGameEventListener
{
	public ActorStats GetActorStats()
	{
		return this.m_actorStats;
	}

	public ActorStatus GetActorStatus()
	{
		return this.m_actorStatus;
	}

	public ActorTargeting GetActorTargeting()
	{
		return this.m_actorTargeting;
	}

	public FreelancerStats GetFreelancerStats()
	{
		return this.m_freelancerStats;
	}
}
