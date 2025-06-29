/*using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using ThunderRoad.AI.Get;
using TOR;
using UnityEngine;
using Object = System.Object;

namespace TORLightsaberAddons
{
    public class TestingClass : ThunderScript
    {
        
        private List<Item> lightsabers = new List<Item>();
        private int maxCount = 1;
        public bool testingEnabled = true;

        IEnumerator SpawnItems(Item itemInput)
        {
            
            Debug.Log("Spawning items");
             var loop =  true;
             float elapsedTime = 0f;
             while (loop)
             {
                 itemInput.transform.position = Player.local.head.transform.position + (Player.local.head.transform.forward * 1.2f);
                 elapsedTime += Time.deltaTime;
                 float percentageComplete = elapsedTime / 5f;

                 itemInput.transform.localScale =
                     Vector3.Lerp(itemInput.transform.localScale, new Vector3(1, 1, 1), percentageComplete);

                 if (percentageComplete >= 1f)
                 {
                     elapsedTime = 0f;
                     loop = false;
                     
                 }

                 yield return null;
             }

             if (lightsabers.Count < maxCount)
             {
                                        foreach (var item in Item.allActive)
                                        {
                                            var distance = Vector3.Distance(Player.local.creature.transform.position,
                                                item.transform.position);
                                            if (distance <= 4f)
                                            {
                                                if (item.gameObject.GetComponent<ItemLightsaber>())
                                                {
                                                    if (!lightsabers.Contains(item))
                                                    {
                                                        if (item.holder)
                                                        {
                                                            item.holder.UnSnap(item);
                                                        }
                                                        item.DisallowDespawn = true;
                                                        item.gameObject.AddComponent<PIDMono>();
                                                        lightsabers.Add(item);
                                                    }
                                                }
                                            }
                                        }
                                    }
             foreach (var saber in lightsabers)
             {
                 var PID = saber.gameObject.GetComponent<PIDMono>();
                 if (!PID.enabled)
                 {
                     saber.SetPhysicModifier(this, 0f, 1f, -1f, -1f);
                     PID.enabled = true;
                     if (saber.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber lightsaber)
                     {
                         var type = typeof(ItemLightsaber);
                         var method = type.GetMethod("TurnOn", BindingFlags.NonPublic | BindingFlags.Instance);
                         if (method != null) method.Invoke(lightsaber, new Object[] { true });
                     }
                     saber.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                     foreach (var collider in Player.local.gameObject.GetComponentsInChildren<Collider>())
                     {
                         saber.IgnoreColliderCollision(collider);
                     }
                 }
             }
            
        }
        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (!testingEnabled) return;
            Player.onSpawn += (player =>
            {
                Debug.Log("Player spawned");
                Debug.Log("On Creature posses");
                List<ItemData> itemDatas = new List<ItemData>();
                for(int i = 0; i < maxCount;i++)
                {
                    itemDatas.Add(Catalog.GetData<ItemData>("Lightsaber_RevanSith"));
                }

                foreach (var itemData in itemDatas)
                {
                    itemData.SpawnAsync(item =>
                    {
                        item.transform.position = Player.local.head.transform.position + (Player.local.head.transform.forward * 1.2f);
                        item.transform.localScale = new Vector3(0, 0, 0);
                        GameManager.local.StartCoroutine(SpawnItems(item));
                    });
                }
            });
        }
    }
}*/