using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ArtemisServer.GameServer
{
    class ArtemisServerBarrierManager : MonoBehaviour
    {
        private static ArtemisServerBarrierManager instance;

        public void UpdateTurn()
        {
            List<Barrier> barriersToRemove = new List<Barrier>();
            foreach (Barrier barrier in BarrierManager.Get().m_barriers)
            {
                barrier.m_time.age++;
                if (barrier.m_time.age >= barrier.m_time.duration)
                {
                    Log.Info($"Barrier by {barrier.Caster.DisplayName} expired");
                    barriersToRemove.Add(barrier);
                }
            }
            if (barriersToRemove.Count > 0)
            { 
                foreach (Barrier barrier in barriersToRemove)
                {
                    BarrierManager.Get().RemoveBarrier(barrier);
                    SharedEffectBarrierManager.Get().EndBarrier(barrier.m_guid);
                }
                BarrierManager.Get().CallRpcUpdateBarriers();
            }
        }


        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static ArtemisServerBarrierManager Get()
        {
            return instance;
        }
    }
}
