using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using TOR;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.VFX;

namespace TORLightsaberAddons.ForcePowers
{
    public class ForceScream : SkillData
    {
        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            this.allowSkill = SaberOptions.local.allowTORSkills;
            if (!creature.gameObject.GetComponent<ForceScreamMono>())
            {
                creature.gameObject.AddComponent<ForceScreamMono>();
            }
        }
    }

    public class ForceScreamMono : MonoBehaviour
    {
        private AudioClip _microphoneClip;
        private bool hasScreamed;
        private bool prewarmed;
        private Queue<GameObject> pool;
        private float volumeCheckCooldown = 0f;
        private float currentVolume = 0f;
        private VisualEffect activeVfx;
        private float[] waveData = new float[64];

        private RaycastHit[] hits = new RaycastHit[50];

        private void Start()
        {
            _microphoneClip = SaberOptions.ReloadInputDevice();
            pool = VFXPool.VFXPool.local.GetPoolByReference("ForceScream");
            if (pool.Count() < 5){
                for (int i = 0; i < 5; i++)
                {
                    StartCoroutine(InstantiateVFX(Vector3.zero, Quaternion.identity));
                }
            }
        }

        public void SetMicrophoneClip(string previous)
        {
            _microphoneClip = SaberOptions.ReloadInputDevice(previous);
        }

        private void OnDestroy()
        {
            if (_microphoneClip != null && Microphone.devices.Length > SaberOptions.audioDevice)
            {
                Microphone.End(Microphone.devices[SaberOptions.audioDevice]);
            }
        }

        private void Update()
        {
            var head = Player.local?.creature?.ragdoll?.headPart?.transform;
            if (head == null) return;

            if (activeVfx != null)
            {
                activeVfx.transform.position = head.position;
            }

            if (Time.time >= volumeCheckCooldown)
            {
                volumeCheckCooldown = Time.time + 0.1f;
                currentVolume = GetMaxVolume();
            }

            if (currentVolume >= SaberOptions.minimumScreamLevel && !hasScreamed)
            {
                hasScreamed = true;
                StartCoroutine(ResetScream());
            }
        }

        private IEnumerator ResetScream()
        {
            var head = Player.local.creature.ragdoll.headPart.transform;
            ManageVFXPool(head.position, head.rotation);

            int mask = (1 << 9) | (1 << 3);
            int hitCount = Physics.SphereCastNonAlloc(head.position, 2f, head.forward, hits, 7f, mask);

            Debug.Log("Force Scream hit count: " + hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                Vector3 toTarget = (hit.collider.transform.position - head.position).normalized;
                float angle = Vector3.Angle(head.forward, toTarget);
                if (angle > 30f) continue;

                if (hit.collider.attachedRigidbody == null) continue;
                Rigidbody refRB = hit.collider.GetComponentInParent<Rigidbody>();
                if (refRB == null) continue;

                var creature = hit.collider.GetComponentInParent<Creature>();
                var item = hit.collider.GetComponentInParent<Item>();

                if ((creature != null && !creature.isPlayer) ||
                    item != null)
                {
                    if (item)
                    {
                        if(item.mainHandler != null && item.mainHandler.creature.isPlayer)
                            continue;
                    }
                    Rigidbody rb = hit.collider.attachedRigidbody;
                    if (creature != null) creature.ragdoll.SetState(Ragdoll.State.Destabilized);

                    Vector3 force = (hit.collider.transform.position - head.position + Vector3.up * 0.5f).normalized;
                    rb.AddForce(force * 200f * rb.mass, ForceMode.Impulse);
                }
            }

            yield return new WaitForSeconds(1.5f);
            hasScreamed = false;
            Array.Clear(hits, 0, hits.Length);
        }

        private void ManageVFXPool(Vector3 position, Quaternion rotation)
        {
            GameObject pooledObj = pool.FirstOrDefault(obj => !obj.activeSelf);

            if (pooledObj == null)
            {
                GameManager.local.StartCoroutine(InstantiateVFX(position, rotation));
                return;
            }

            pooledObj.transform.position = position;
            pooledObj.transform.rotation = rotation;
            pooledObj.SetActive(true);

            var vfx = pooledObj.GetComponent<VisualEffect>();
            var audio = pooledObj.GetComponents<AudioSource>();

            if (vfx) StartCoroutine(DelayVFX(vfx));

            if (audio != null)
            {
                foreach (var sound in audio)
                {
                    sound.Play();
                }
            }
        }

        private IEnumerator InstantiateVFX(Vector3 position, Quaternion rotation)
        {
            bool isDone = false;

            Catalog.InstantiateAsync("braxton3300.scream", position, rotation, null, callback =>
            {
                callback.SetActive(false);
                pool.Enqueue(callback);
                if (pool.Count <= 5) return;
                callback.SetActive(true);
                activeVfx = callback.GetComponent<VisualEffect>();
                StartCoroutine(DelayVFX(activeVfx, callback.GetComponents<AudioSource>()));
                
                isDone = true;
            }, "ForceScreamHandler");

            yield return new WaitUntil(() => isDone);
        }

        private IEnumerator DelayVFX(VisualEffect vfx, AudioSource[] audio = null)
        {
            yield return null;
            vfx.Reinit();
            vfx.Play();

            if (audio != null)
            {
                foreach (var sound in audio)
                {
                    sound.Play();
                }
            }
        }

        private float GetMaxVolume()
        {
            if (_microphoneClip == null || Microphone.devices.Length <= SaberOptions.audioDevice)
                return 0f;

            int position = Microphone.GetPosition(Microphone.devices[SaberOptions.audioDevice]) - waveData.Length;
            if (position < 0) return 0f;

            _microphoneClip.GetData(waveData, position);

            float maxVolume = 0f;
            foreach (float wave in waveData)
            {
                float abs = Mathf.Abs(wave);
                if (abs > maxVolume) maxVolume = abs;
            }

            return maxVolume;
        }
    }
}
