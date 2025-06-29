using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace TORLightsaberAddons
{
    public class MeshSlicer : MonoBehaviour
    {
        private Material meltMaterial;
        public Plane planeTransform;
        public List<Item> itemsCantBeSliced = new List<Item>();
        private void Start()
        {
            meltMaterial = SaberOptions.local.material;
        }

        public void Slice(Item target, Vector3 angularVelocity, Vector3 startPosition, Vector3 endPosition)
        {
            Vector3 planeNormal = Vector3.Cross(endPosition - startPosition, angularVelocity);
            planeNormal.Normalize();
            SlicedHull hull = target.gameObject.Slice(endPosition, planeNormal);
            
            if (hull != null)
            {
                GameObject upperHull = hull.CreateUpperHull(target.gameObject, SaberOptions.local.material.DeepCopyByExpressionTree());
                //GameManager.local.StartCoroutine(AssignMaterialNextFrame(upperHull.gameObject, meltMaterial));
                SetupSliceComponent(upperHull);
                GameObject lowerHull = hull.CreateLowerHull(target.gameObject, SaberOptions.local.material.DeepCopyByExpressionTree());
                //GameManager.local.StartCoroutine(AssignMaterialNextFrame(lowerHull.gameObject, meltMaterial));
                SetupSliceComponent(lowerHull);
                
                target.Despawn();
                
            }
        }
        
        IEnumerator AssignMaterialNextFrame(GameObject obj, Material crossSectionMat) {
            yield return null; // Wait one frame

            if (obj != null && obj.GetComponent<Renderer>() != null) {
                Material[] currentMaterials = obj.GetComponent<Renderer>().sharedMaterials;
                Material[] newMaterials = new Material[currentMaterials.Length];

                System.Array.Copy(currentMaterials, newMaterials, currentMaterials.Length - 1);
                newMaterials[newMaterials.Length - 1] = crossSectionMat ?? newMaterials[0]; // Fallback to first material

                obj.GetComponent<Renderer>().sharedMaterials = newMaterials;
            }
        }

        public void SetupSliceComponent(GameObject sliceObject)
        {
            Rigidbody rb = sliceObject.AddComponent<Rigidbody>();
            MeshCollider collider = sliceObject.AddComponent<MeshCollider>();
            collider.convex = true;
            rb.AddExplosionForce(5f, sliceObject.transform.position, 0.4f);
        }
    }
}
