using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using ThunderRoad.Skill;
using TOR;
using TORLightsaberAddons.ForcePowers;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;

namespace TORLightsaberAddons
{
    public class SaberOptions : ThunderScript
    {
        public static SaberOptions local;
        public Material material;
        public GameObject slamEffect;
        [ModOption("Allow Any Item", "Enables/Disables allowing the saber throw feature to work on any item", category = "Lightsaber Addons")]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        public static bool allowThrowOnAnyItem;

        public static ModOptionFloat[] screamLevels = new ModOptionFloat[]
        {
            new ModOptionFloat("Whisper", 0.1f),
            new ModOptionFloat("Low", 0.2f),
            new ModOptionFloat("Medium", 0.3f),
            new ModOptionFloat("High", 0.4f),
            new ModOptionFloat("Very High", 0.5f),
            new ModOptionFloat("Extreme", 0.6f),
            new ModOptionFloat("Mosh-pit", 0.7f)
        };
        
        [ModOption("Minimum Force Scream Level", valueSourceName = nameof(screamLevels), defaultValueIndex = 5, category = "Audio Input")] 
        [ModOptionOrder(1)]
        [ModOptionSave]
        [ModOptionSaveValue(true)]
        [ModOptionTooltip("Minimum volume level for force scream to activate")] 
        public static float minimumScreamLevel;
        
        public static int audioDevice = 0;
        public static ModOptionInt[] AudioDevices()
        {
            var options = new ModOptionInt[Microphone.devices.Length];
            for (var i = 0; i < options.Length; i++)
            {
                options[i] = new ModOptionInt(Microphone.devices[i], i);
            }
            return options;
        }
        
        [ModOptionCategory("Audio Input", 1)]
        [ModOptionOrder(0)]
        [ModOptionTooltip("Determines the audio device to listen to")]
        [ModOption("Audio device", valueSourceName = nameof(AudioDevices), defaultValueIndex = 0, saveValue = true)]
        public static void AudioDevice(int value)
        {
            if (!Player.local || !Player.local.creature)
                return;
            var previousMicrophoneName = Microphone.devices[audioDevice];
            audioDevice = Mathf.Clamp(value, 0, Microphone.devices.Length);
            if (Player.local.creature.gameObject.GetComponent<ForceScreamMono>() is ForceScreamMono mono)
            {
                mono.SetMicrophoneClip(previousMicrophoneName);
            }
            else ReloadInputDevice(previousMicrophoneName);
        }

        private static bool loaded;
        public bool allowTORSkills;
        public static AudioClip ReloadInputDevice(string previousMicrophoneName = null, AudioClip _microphoneClip = null)
        {
            if (!loaded)
                return null;

            if (previousMicrophoneName != null)
                Microphone.End(previousMicrophoneName);

            var microphoneName = Microphone.devices[SaberOptions.audioDevice];
            if (_microphoneClip != null)
                Microphone.End(microphoneName);
            return Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate);
        }
        public override void ScriptEnable()
        {
            base.ScriptEnable();
            if(local == null) local = this;
            loaded = true;
            ReloadInputDevice();
            Catalog.LoadAssetAsync<Material>("braxton3300.material", (shader) => { this.material = shader; Debug.LogError("Found material: " + shader);},"MeltingMaterial");
            Catalog.LoadAssetAsync<GameObject>("braxton3300.slam", (slam) => { this.slamEffect = slam; Debug.LogError("Found slam: " + slam);},"SlamEffect");

            string assemblyName = "TheOuterRim";
            var assemblyLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == assemblyName);
            
            allowTORSkills = assemblyLoaded;
            
            if(!assemblyLoaded) Debug.Log("The Outer Rim Mod is not installed... please go to https://www.nexusmods.com/bladeandsorcery/mods/528 and download the mod to continue using the TOR Addons mod.");
            else
            {
                Debug.Log("The Outer Rim Mod is installed, loading TOR Addons...");
            }

            /*Player.onSpawn += player =>
            {
                player.onCreaturePossess += (creatureSpawn) =>
                {
                    //var type = typeof(LightsaberMastery);
                    if (!Player.local.creature.container.HasSkillContent("LightsaberMastery"))
                    {
                        Player.local.creature.container.AddSkillContent("LightsaberMastery");
                    }
                };
            };*/
        }
    }
    public class LightsaberThrow : SkillData
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
            if (!handle.item.gameObject.GetComponent<ThrowPID>() && ragdollhand.creature.isPlayer) handle.item.gameObject.AddComponent<ThrowPID>();
        }
        
    }

    public class ThrowPID : MonoBehaviour
    {
         public bool isThrown = false;
        private PID controller = new PID();
        private Vector3 targetPosition;
        private Item item;
        private float power = 80f;
        private bool saberOn = false;
        private Vector3 initialAngularVelocityDirection;

        private bool canReturn;
        bool alreadyThrown = false;
        
        private RagdollHand currentHand;
        private bool isReturning = false;
        bool allowCatch = false;
        private bool rotationAxisSet = false;
        private Vector3 rotationAxis;
        bool checkPenetration = false;

        private ItemLightsaber lightsaber;

        private void OnDestroy()
        {
            item.OnThrowEvent -= OnThrow;
        }

        private MeshSlicer slicer;
        private void Start()
        {
            controller.proportionalGain = 1f;
            controller.derivativeGain = 0.3f;
            controller.integralGain = 0f;
            controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
            item = GetComponent<Item>();
            currentHand = item.mainHandler;
            item.OnThrowEvent += OnThrow;
            item.OnGrabEvent += OnGrab;
            item.OnUngrabEvent += UnGrab;
            if(item.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber saber) lightsaber = saber;
        }

        private void UnGrab(Handle handle, RagdollHand ragdollhand, bool throwing)
        {
            if(throwing) currentHand = ragdollhand;
        }

        private void OnThrow(Item item1)
        {
            if (this.lightsaber && !isThrown &&
                item.physicBody.velocity.magnitude > 7f)
            {
                item1.isThrowed = true;
                currentHand.caster.telekinesis.Disable(this);
                var type = typeof(ItemLightsaber);
                var field = type.GetField("isActive", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) saberOn = (bool)field.GetValue(lightsaber);
                isThrown = true;
                saberOn = true;
                initialAngularVelocityDirection = item.physicBody.rigidBody.angularVelocity;
                item.physicBody.rigidBody.maxAngularVelocity = 1000000f;
                item.physicBody.rigidBody.useGravity = false;
    
                foreach (var blade in lightsaber.blades)
                {
                    Player.local.creature.ragdoll.IgnoreCollision( blade.collisionBlade, true);
                    if(blade.collisionBlade.enabled) blade.collisionBlade.attachedRigidbody.useGravity = false;
                }
            }
            else if (SaberOptions.allowThrowOnAnyItem && !isThrown && item.physicBody.velocity.magnitude > 7f)
            {
                item1.isThrowed = true;
                currentHand.caster.telekinesis.Disable(this);
                isThrown = true;
                saberOn = true;
                item.physicBody.rigidBody.maxAngularVelocity = 1000000f;
                item.physicBody.rigidBody.useGravity = false;
            }
        }
        
        private void OnCollisionEnter(Collision other)
        {
            if(isThrown) item.physicBody.rigidBody.angularVelocity = initialAngularVelocityDirection;
            /*if (!item.GetComponent<ItemLightsaber>()) return;
            if(other.collider.gameObject.GetComponentInParent<ItemBlasterBolt>()) return;
            
            if (other.gameObject.GetComponentInParent<Item>() is Item sliceable)
            {
                slicer = sliceable.gameObject.AddComponent<MeshSlicer>();
                if (other.collider.gameObject.GetComponentInParent<ItemLightsaber>() is I--temLightsaber saber)
                {
                    foreach (var blade in saber.blades)
                    {
                        if (other.collider.Equals(blade.collisionBlade)) return;
                    }

                    foreach (var blade in item.gameObject.GetComponent<ItemLightsaber>().blades)
                    {
                        Vector3 endPosition = blade.saberBodyTrans.position +
                                              (-blade.saberBodyTrans.forward * blade.currentLength);
                        if(sliceable.mainHandler) sliceable.mainHandler.UnGrab(false);
                        slicer.Slice(sliceable, blade.collisionBlade.GetPhysicBody().angularVelocity, blade.saberBodyTrans.position,
                            endPosition);
                        return;
                    }
                }
                else
                {
                    foreach (var blade in item.gameObject.GetComponent<ItemLightsaber>().blades)
                    {
                        Vector3 endPosition = blade.saberBodyTrans.position +
                                              (-blade.saberBodyTrans.forward * blade.currentLength);
                        if(sliceable.mainHandler) sliceable.mainHandler.UnGrab(false);
                        slicer.Slice(sliceable, blade.collisionBlade.GetPhysicBody().angularVelocity, blade.saberBodyTrans.position,
                            endPosition);
                        return;
                    }
                }
            }*/
        }

        private void OnGrab(Handle handle, RagdollHand ragdollhand)
        {
            if (isThrown)
            {
                isThrown = false;
            }
            currentHand = item.mainHandler;
            allowCatch = false;
            canReturn = false;
            item.physicBody.rigidBody.useGravity = true;
            if (item.GetComponent<ItemLightsaber>() is ItemLightsaber lightsaber)
            {
                foreach (var blade in lightsaber.blades)
                {
                    if(blade.collisionBlade.enabled) blade.collisionBlade.attachedRigidbody.useGravity = true;
                }
            }
            currentHand.caster.telekinesis.Enable(this);
            
        }
        private void FixedUpdate()
        {
            if (isThrown)
            {
                if (lightsaber)
                {
                    var type = typeof(ItemLightsaber);
                    var method = type.GetField("returning", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method != null) isReturning = (bool)method.GetValue(lightsaber);
                    item.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                    foreach (var blade in lightsaber.blades)
                    {
                        Player.local.creature.ragdoll.IgnoreCollision( blade.collisionBlade, true);
                    }
                }
                else if (SaberOptions.allowThrowOnAnyItem)
                {
                    item.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                }

                controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
                

                if (!isReturning)
                {
                    try
                    {
                        if (currentHand != null)
                        {
                            if (currentHand.grabbedHandle == null)
                            {
                                targetPosition = currentHand.caster.magicSource.position +
                                                 (currentHand.caster.magicSource.forward * 0.08f);
                            }
                            else if (currentHand.grabbedHandle != null)
                            {
                                if (currentHand.otherHand.grabbedHandle == null)
                                {
                                    currentHand = currentHand.otherHand;
                                    targetPosition = currentHand.otherHand.caster.magicSource.position +
                                                     (currentHand.otherHand.caster.magicSource.forward * 0.08f);
                                }
                                else
                                {
                                    currentHand.caster.telekinesis.Enable(this);
                                    currentHand = null;
                                    targetPosition = Player.local.creature.ragdoll.headPart.transform.position +
                                                     (Player.local.creature.ragdoll.headPart.transform.forward * 0.7f);
                                }
                            }
                        }
                        else
                        {
                            targetPosition = Player.local.creature.ragdoll.headPart.transform.position +
                                             (Player.local.creature.ragdoll.headPart.transform.forward * 0.3f);
                        }

                        if (item.isPenetrating && !checkPenetration)
                        {
                            StartCoroutine(WaitAndUnpenetrate());
                            checkPenetration = true;
                        }

                        item.SetColliderLayer(GameManager.GetLayer(LayerName.MovingItem));

                        Vector3 input = controller.Update(Time.fixedDeltaTime, item.physicBody.rigidBody.position,
                            targetPosition);
                        item.physicBody.rigidBody.AddForce(input * power);

                        if (saberOn &&
                            Vector3.Distance(item.gameObject.transform.position, targetPosition) >= 0.6f)
                        {
                            if (!canReturn) canReturn = true;

                            if (!rotationAxisSet)
                            {
                                rotationAxis = new Vector3(0, initialAngularVelocityDirection.y, 0).normalized;
                                rotationAxisSet = true;
                            }

                            float direction =
                                Mathf.Sign(Vector3.Dot(this.initialAngularVelocityDirection, rotationAxis));
                            if (direction == 0) direction = 1;

                            float torqueStrength = Mathf.Clamp(100f - initialAngularVelocityDirection.magnitude, 10f,
                                100f);
                            item.physicBody.rigidBody.AddTorque(rotationAxis * direction * torqueStrength,
                                ForceMode.Acceleration);
                        }

                        if (saberOn && Vector3.Distance(item.gameObject.transform.position, targetPosition) < 0.6f &&
                            canReturn)
                        {
                            item.physicBody.rigidBody.angularVelocity = Vector3.zero;
                            if (currentHand) transform.rotation = currentHand.transform.rotation;
                            else transform.rotation = Player.local.creature.ragdoll.headPart.transform.rotation;
                            canReturn = false;
                            allowCatch = true;
                        }

                        if (saberOn &&
                            Vector3.Distance(item.gameObject.transform.position, targetPosition) < 0.4f && allowCatch)
                        {
                            if (currentHand) currentHand.Grab(item.handles[0]);
                            allowCatch = false;
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        // ignored
                        Debug.Log("Current Hand change, throw exception");
                    }
                }
                
            }
        }
        
        IEnumerator WaitAndUnpenetrate()
        {
            yield return new WaitForSeconds(2f);
            if (item.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber lightsaber && item.isPenetrating)
            {
                var type = typeof(ItemLightsaber);
                var method = type.GetMethod("Unpenetrate", BindingFlags.NonPublic | BindingFlags.Instance);
                if(method != null) method.Invoke(lightsaber, new object[] { });
            }
            else if (SaberOptions.allowThrowOnAnyItem)
            {
                item.FullyUnpenetrate();
            }
            checkPenetration = false;
        }
    }
}