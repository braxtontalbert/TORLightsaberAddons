/*using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using TOR;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TORLightsaberAddons
{
    public class LightsaberMastery : SkillData
    {
        private bool testModeOn = true;
        private List<Item> lightsabers = new List<Item>();
        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            ItemData data = Catalog.GetData<ItemData>("Lightsaber_Luke_EP4");
            data.SpawnAsync(callback =>
            {
                callback.physicBody.rigidBody.useGravity = false;
                callback.IgnoreRagdollCollision(creature.ragdoll);
                callback.gameObject.transform.position = Player.local.head.transform.position +
                                                         (Player.local.head.transform.forward * 0.3f);
                callback.gameObject.AddComponent<MasteryMono>();
            });
        }
    }

    public class MasteryMono : MonoBehaviour
    {
        public bool enabled;
        private PID controller = new PID();
        private Item item;
        private bool saberOn;
        private float power = 200f;

        void Start()
        {
            item = GetComponent<Item>();
            controller.proportionalGain = 1f;
            controller.derivativeGain = 0.4f;
            controller.integralGain = 0f;
            controller.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
        }

        private Vector3 previousTargetPosition;
        private Vector3 targetPosition;
        private Vector3 targetAngle;
        private void FixedUpdate()
        {
            if (item.mainHandler != null) return;
            if (!saberOn)
            {
                if (item.gameObject.GetComponent<ItemLightsaber>() is ItemLightsaber saber)
                {
                    saber.TurnOn();
                    saberOn = true;
                }
            }

            float distance = 10f * 10f;
            Item reference = null;
            Vector3 toMoveTo = new Vector3();
            bool moveToSet = false;
            foreach (Item item in Item.allActive)
            {
                if (item.mainHandler is RagdollHand hand && item != this.item)
                {
                    var distanceSquared = (Player.local.creature.transform.position -
                                           hand.grabbedHandle.item.parryPoint.position).sqrMagnitude;
                    if (distanceSquared < distance)
                    {
                        distance = distanceSquared;
                        reference = hand?.grabbedHandle?.item;
                    }
                }

                if (reference)
                {
                    var velocityDistance = (Player.local.creature.transform.position - reference.parryPoint.position)
                        .sqrMagnitude;
                    if (velocityDistance < 2f)
                    {
                        var positionMinus = (Player.local.head.transform.position - item.parryPoint.position);
                        var next = (positionMinus + (item.parryPoint.position - item.transform.position)).normalized *
                                   0.5f;
                        toMoveTo = reference.parryPoint.position + next;
                        moveToSet = true;
                        power = 200f;
                    }
                    else
                    {
                        moveToSet = false;
                        distance = 100f;
                        controller.proportionalGain = 1f;
                        controller.derivativeGain = 0.4f;
                        controller.integralGain = 0f;
                        power = 200f;
                    }
                }
            }

            if (moveToSet && reference)
            {
                if(!reference.mainHandler.creature.brain.isAttacking && reference.physicBody.velocity.magnitude <= 0.5f) targetPosition = toMoveTo;
                Vector3 toTarget = (item.parryPoint.position - reference.parryPoint.position).normalized;
                Vector3 forwardA = reference.parryPoint.forward; // or .up, depending on your use
                Vector3 bisector = (forwardA + toTarget).normalized;

                Vector3 currentDir = item.parryPoint.forward; // or up
                Vector3 targetDir = reference.parryPoint.forward;

                Vector3 rotationAxis = Vector3.Cross(currentDir, targetDir);
                float angleDifference = Vector3.Angle(currentDir, targetDir);

                Vector3 torque = rotationAxis.normalized * angleDifference * power;
                item.physicBody.rigidBody.AddTorque(torque, ForceMode.Force);
            }
            else
            {
                targetPosition =  Player.local.head.transform.position + (Player.local.head.transform.forward * 1.2f);
                Vector3 currentDir = item.transform.up;
                Vector3 rotationAxis = Vector3.Cross(currentDir, Vector3.up);
                float angleDifference = Vector3.Angle(currentDir, Vector3.up);


                // Apply torque with randomized direction
                Vector3 torque = rotationAxis.normalized * angleDifference * power;
                item.physicBody.rigidBody.AddTorque(torque * power);
            }
            
            RaycastHit hit;
            Vector3 avoidForce = Vector3.zero;

            Vector3[] directions = {
                transform.forward,
                Quaternion.Euler(0, 30, 0) * transform.forward,
                Quaternion.Euler(0, -30, 0) * transform.forward
            };

            foreach (Vector3 dir in directions)
            {
                if (Physics.Raycast(transform.position, dir, out hit, 2f, (1 << 9) | (1 << 3)))
                {
                    if (hit.transform.GetComponentInParent<Creature>() is Creature creature && creature.isPlayer)
                    {
                        Vector3 awayFromObstacle = -dir.normalized * (2f - hit.distance);
                        avoidForce += awayFromObstacle;
                    }
                }
            }

            // Combine avoidance with your PID target direction
            var pidTargetDirection = (targetPosition - item.transform.position).normalized;
            Vector3 desiredDirection = pidTargetDirection + avoidForce.normalized;
            
            Vector3 input = controller.Update(Time.fixedDeltaTime, item.physicBody.rigidBody.position, targetPosition);
            
            item.physicBody.rigidBody.AddForce(input * power);
        }
    }
}*/