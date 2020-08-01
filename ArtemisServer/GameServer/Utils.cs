using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArtemisServer.GameServer
{
    class Utils
    {
        public static Dictionary<int, int> GetActorIndexToDeltaHP(Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> targetedActors)
        {
            Dictionary<int, int> actorIndexToDeltaHP = new Dictionary<int, int>();
            foreach (var targetedActor in targetedActors)
            {
                int actorIndex = targetedActor.Key.ActorIndex;
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Healing, out int healing);
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Damage, out int damage);
                targetedActor.Value.TryGetValue(AbilityTooltipSymbol.Absorb, out int absorb);  // TODO: how does absorb count here? (does it count at all, does absorb from previous phase somehow affect calculations?)
                int deltaHP = absorb + healing - damage;
                actorIndexToDeltaHP.Add(actorIndex, deltaHP);
            }
            return actorIndexToDeltaHP;
        }

        public static void Add(ref Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> dst, Dictionary<ActorData, Dictionary<AbilityTooltipSymbol, int>> src)
        {
            foreach (var target in src)
            {
                ActorData targetActor = target.Key;
                if (!dst.ContainsKey(targetActor))
                {
                    dst[targetActor] = new Dictionary<AbilityTooltipSymbol, int>();
                }
                foreach (var symbolToValue in target.Value)
                {
                    if (!dst[targetActor].ContainsKey(symbolToValue.Key))
                    {
                        dst[targetActor][symbolToValue.Key] = 0;
                    }
                    dst[targetActor][symbolToValue.Key] += symbolToValue.Value;
                }
            }
        }
    }
}
