using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using TOR;
using UnityEngine;
using UnityEngine.VFX;

namespace TORLightsaberAddons
{
    public class LightsaberSlam : SkillData
    {
        
        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            this.allowSkill = SaberOptions.local.allowTORSkills;
            EventManager.OnItemGrab += ItemGrab;
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            EventManager.OnItemGrab -= ItemGrab;
        }

        private void ItemGrab(Handle handle, RagdollHand ragdollhand)
        {
            var itemGO = handle.item.gameObject;
            if (itemGO.TryGetComponent<ItemLightsaber>(out ItemLightsaber saber) &&
                !itemGO.TryGetComponent<SlamEffect>(out SlamEffect slamEffect))
            {
                itemGO.AddComponent<SlamEffect>();
            }
        }
    }
    
    public class SlamEffect : MonoBehaviour
    {
        private Item item;
        private VisualEffect activeVfx;
        private bool hasJumped;
        private AudioSource source;
        private bool prewarmed;
        private Queue<GameObject> pool;
        private GameObject created;

        private void Awake()
        {
            item = GetComponent<Item>();
        }

        private void Start()
        {
            Player.local.locomotion.OnJumpEvent += JumpEvent;
            pool = VFXPool.VFXPool.local.GetPoolByReference("SlamEffect");
            if (pool.Count() < 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    StartCoroutine(InstantiateVFX(Vector3.zero));
                }
            }
        }

        private void JumpEvent()
        {
            hasJumped = true;
        }

        private void OnDestroy()
        {
            if (Player.local?.locomotion != null)
                Player.local.locomotion.OnJumpEvent -= JumpEvent;
        }

        Collider[] array = new Collider[50];
        private const float SlamRadius = 10f;
        private const float ExplosionForce = 2000f;
        private const float UpwardModifier = 35f;
        private const int SlamMask = (1 << 3) | (1 << 9); // Only include layers 3 and 9
        private void OnCollisionEnter(Collision other)
        {
            if (!hasJumped || !item.isPenetrating) return;

            if (other.collider.GetComponentInParent<Creature>() || other.collider.GetComponentInParent<Item>()) return;

            Vector3 contactPoint = other.contacts[0].point;
            ManageVFXPool(contactPoint);

            int hitCount = Physics.OverlapSphereNonAlloc(contactPoint, SlamRadius, array, SlamMask);
            Debug.Log("Hit count: " + hitCount);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = array[i];
                if (collider == null || !collider.attachedRigidbody) continue;
                Rigidbody refRB = collider.GetComponentInParent<Rigidbody>();
                if (refRB == null) continue;
                var hitCreature = collider.GetComponentInParent<Creature>();
                var hitItem =  collider.GetComponentInParent<Item>();
                float mass = collider.attachedRigidbody.mass;

                if (hitCreature != null && !hitCreature.isPlayer)
                {
                    hitCreature.ragdoll.SetState(Ragdoll.State.Destabilized, true);

                    var rb = collider.attachedRigidbody;
                    if (rb != null && rb.isKinematic) rb.isKinematic = false;

                    rb.AddExplosionForce(ExplosionForce, contactPoint, SlamRadius, UpwardModifier, ForceMode.Impulse);
                }
                else if (hitItem != null && hitItem != this.item)
                {
                    collider?.attachedRigidbody?.AddExplosionForce(mass * ExplosionForce, contactPoint, SlamRadius, UpwardModifier);
                }
            }

            hasJumped = false;
            
            Array.Clear(array, 0, hitCount);
        }

        void ManageVFXPool(Vector3 contactPoint)
        {
            GameObject pooledObj = null;
            foreach (var obj in pool)
            {
                if (!obj.activeSelf)
                {
                    pooledObj = obj;
                    break;
                }
            }
            if (pooledObj == null)
            {
                // Defer instantiation to avoid spike on this frame
                GameManager.local.StartCoroutine(InstantiateVFX(contactPoint));
                return;
            }

            pooledObj.transform.position = contactPoint;
            pooledObj.SetActive(true);

            var vfxs = pooledObj.GetComponent<VisualEffect>();
            var audio = pooledObj.GetComponent<AudioSource>();

            if (vfxs)
            {
                StartCoroutine(DelayVFX(vfxs));
            }
            if (audio) audio.Play();
        }

        IEnumerator DelayVFX(VisualEffect vfx, AudioSource audio = null)
        {
            yield return null;
            vfx.Reinit();
            vfx.Play();
            if(audio) audio.Play();
        }
        IEnumerator DelayVFX(VisualEffect[] vfxs, AudioSource audio = null)
        {
            yield return null;
            foreach (var vfx in vfxs)
            {
                vfx.Reinit();
                vfx.Play();   
            }
            if(audio) audio.Play();
        }
        
        IEnumerator InstantiateVFX(Vector3 position)
        {
            bool isDone  = false;
            Catalog.InstantiateAsync("braxton3300.slam", position, Quaternion.identity, null, callback =>
            {
                callback.SetActive(false);
                pool.Enqueue(callback);
                if (pool.Count <= 5) return;
                callback.SetActive(true);
                StartCoroutine(DelayVFX(callback.GetComponent<VisualEffect>(), callback.GetComponent<AudioSource>()));
                isDone = true;
            }, "SlamEffectHandler");

            yield return new WaitUntil(() => isDone);
        }
    }
}