using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TORLightsaberAddons.VFXPool
{
    public class VFXPool : ThunderScript
    {
        public static VFXPool local;

        
        Queue<GameObject> slamEffectPool = new Queue<GameObject>();
        Queue<GameObject> forceScreamPool = new Queue<GameObject>();
        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (local == null) local = this;
            EventManager.onLevelLoad += Unload;
        }

        public override void ScriptUnload()
        {
            base.ScriptUnload();
            EventManager.onLevelLoad -= Unload;
        }

        private void Unload(LevelData leveldata, LevelData.Mode mode, EventTime eventtime)
        {
            ClearVFXPool();
        }

        public Queue<GameObject> GetPoolByReference(String reference)
        {
            switch (reference)
            {
                case "SlamEffect":
                    return slamEffectPool;
                case "ForceScream":
                    return forceScreamPool;
                default:
                    return null;
            }
        }
        
        public void ClearVFXPool()
        {
            foreach (var vfx in slamEffectPool)
            {
                Catalog.ReleaseAsset(vfx);
            }
            slamEffectPool.Clear();
            slamEffectPool.TrimExcess();
            foreach (var forceScream in forceScreamPool)
            {
                Catalog.ReleaseAsset(forceScream);
            }
            forceScreamPool.Clear();
            forceScreamPool.TrimExcess();
        }
    }
}