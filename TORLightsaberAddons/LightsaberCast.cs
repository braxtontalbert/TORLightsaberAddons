/*using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using TOR;
using UnityEngine;
using Object = System.Object;

namespace TORLightsaberAddons
{
    public class LightsaberCast : SpellCastCharge
    {
        private List<Item> lightsabers = new List<Item>();
        private int maxCount = 1;

        public override void UpdateCaster()
        {
            base.UpdateCaster();
            if (spellCaster.isFiring)
            {
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
                else
                {
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
                        }
                    }
                }
            }
            else
            {
                foreach (var saber in lightsabers)
                {
                    if (saber.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber lightsaber)
                    {
                        var type = typeof(ItemLightsaber);
                        var method = type.GetMethod("TurnOff", BindingFlags.NonPublic | BindingFlags.Instance);
                        if(method != null) method.Invoke(lightsaber, new Object[] { true });
                    }
                    saber.SetPhysicModifier(this, 1f, 1f, -1f, -1f);
                    var PID = saber.gameObject.GetComponent<PIDMono>();
                    PID.enabled = false;
                }
                lightsabers.Clear();
            }
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            foreach (var saber in lightsabers)
            {
                saber.RemovePhysicModifier(this);
                saber.ResetRagdollCollision();
                var PID = saber.gameObject.GetComponent<PIDMono>();
                GameObject.Destroy(PID);
                lightsabers.Clear();
            }
        }
    }

    public class PIDMono : MonoBehaviour
    {
        public bool enabled = false;
        private PID controller = new PID();
        private Vector3 targetPosition;
        private Vector3 previousTargetPosition;
        private Item item;
        private float power = 20f;
        private bool saberOn = false;
        
        
        private void Start()
        {
            controller.proportionalGain = 1f;
            controller.derivativeGain = 0.3f;
            controller.integralGain = 0f;
            controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
            item = GetComponent<Item>();
        }
        
        private float accelMax = 1f;
        /*public void applyTorque(Quaternion targetRot, PhysicBody pb, float speedMod)
        {
            var rotation = Vector3.Cross(pb.transform.rotation * Vector3.forward, targetRot * Vector3.forward)
                           + Vector3.Cross(pb.transform.rotation * Vector3.up, targetRot * Vector3.up)
                           + Vector3.Cross(pb.transform.rotation * Vector3.right, targetRot * Vector3.right);


            var torque = controller.Update(rotation, Time.deltaTime) * speedMod;
            pb.AddTorque(torque.ClampMagnitude(0, accelMax), ForceMode.Acceleration);
        }#1#

        private void FixedUpdate()
        {
            if (enabled)
            {
                if (!item.mainHandler) item.mainHandler = Player.currentCreature.handRight;
                if (!saberOn)
                {
                    if (item.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber lightsaber)
                    {
                        var type = typeof(ItemLightsaber);
                        var method = type.GetMethod("TurnOn", BindingFlags.NonPublic | BindingFlags.Instance);
                        if(method != null) method.Invoke(lightsaber, new Object[] { true });
                    }
                }
                Vector3 toMoveTo = new Vector3();
                bool moveToSet = false;
                float distance = 100f;

                Item reference = null;
                foreach (var activeItem in Item.allActive)
                {
                    if (activeItem.mainHandler is RagdollHand hand && activeItem != item)
                    {
                        
                        var distanceInside = Vector3.Distance(Player.local.creature.transform.position,
                            hand.grabbedHandle.item.parryPoint.position);
                        if (distanceInside < distance)
                        {
                            distance = distanceInside;
                            reference = hand.grabbedHandle.item;
                        }
                    }
                    if (reference)
                    {
                        //var velocityVector = reference.physicBody.velocity;
                        var velocityDistance = Vector3.Distance(reference.parryPoint.position ,Player.local.creature.transform.position);
                        if (velocityDistance < 3f && reference.physicBody.velocity.magnitude > 0f)
                        {
                            var positionMinus = (Player.local.head.transform.position - item.parryPoint.position);
                            var next = (positionMinus + ((item.parryPoint.position - item.transform.position))).normalized * 0.5f;
                            toMoveTo = reference.parryPoint.position + next;
                            moveToSet = true;
                        }
                        else
                        {
                            moveToSet = false;
                            distance = 100f;
                        }
                    }
                }

                if (moveToSet)
                {
                    if (previousTargetPosition != targetPosition)
                    {
                        controller.derivativeMeasurement = PID.DerivativeMeasurement.Velocity;
                        controller.proportionalGain = 2f;
                        power = 500f;
                        controller.derivativeGain = 0.6f;
                    }
                    else
                    {
                        controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
                        controller.proportionalGain = 1f;
                        controller.derivativeGain = 0.4f;
                        power = 25f;
                    }
                    controller.derivativeMeasurement = PID.DerivativeMeasurement.Velocity;
                    controller.proportionalGain = 2f;
                    power = 1000f;
                    controller.derivativeGain = 0.7f;
                    targetPosition = toMoveTo;
                }
                else
                {
                    power = 20f;
                    controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
                    controller.proportionalGain = 1f;
                    controller.derivativeGain = 0.3f;
                    targetPosition = Player.local.head.transform.position + (Player.local.head.transform.forward * 1.2f);
                }
                Vector3 input = controller.Update(Time.fixedDeltaTime, item.physicBody.rigidBody.position,
                    targetPosition);
                item.physicBody.rigidBody.AddForce(input * power);
            }
            else
            {
                saberOn = false;
            }
        }
    }
}*/