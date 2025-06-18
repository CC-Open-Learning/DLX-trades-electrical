using System.Collections.Generic;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class MeshMerger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Override resulting material. If this is empty, material of a given mesh will be selected. " +
                 "\nNote that merged meshes can only have one material. It's recommended to override a desired" +
                 " material if meshes to merge have different materials.")]
        private Material materialOverride;

        [SerializeField]
        [Tooltip("Override meshes to merge. If this is empty, all the child meshes, including the mesh in the " +
                 "current object will be merged.")]
        private MeshFilter[] overriddenMeshes;

        /// <summary>
        /// Merge given meshes
        /// </summary>
        public void MergeMeshes()
        {
            MeshFilter[] meshesToMerge = overriddenMeshes.Length > 0 ? overriddenMeshes : GetMeshFiltersInChildren();
            if (meshesToMerge.Length == 0)
            {
                Debug.LogWarning($"No meshes given to merge at {name}");
                return;
            }

            Mesh combinedMesh = CombineGivenMeshes(meshesToMerge);

            GetComponent<MeshCollider>().sharedMesh = combinedMesh;
            GetComponent<MeshFilter>().sharedMesh = combinedMesh;

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (materialOverride)
            {
                meshRenderer.sharedMaterial = materialOverride;
            }
            else
            {
                Material material = meshesToMerge[0].gameObject.GetComponent<Renderer>().sharedMaterial;
                meshRenderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Get MeshFilter components of children including the current transform. If the current
        /// transform doesn't have a mesh, this will only get valid MeshFilters of children.
        /// </summary>
        /// <returns>MeshFilter components of child GameObjects</returns>
        private MeshFilter[] GetMeshFiltersInChildren()
        {
            MeshFilter[] meshFilters;

            // Parent have empty mesh in the MeshFilter. Therefore, only return child MeshFilters.
            if (!transform.GetComponent<MeshFilter>().sharedMesh)
            {
                List<MeshFilter> tempFilters = new();
                for (int i = 0; i < transform.childCount; i++)
                {
                    if (!transform.GetChild(i).TryGetComponent(out MeshFilter meshFilter)) { continue; }
                    if (!meshFilter.sharedMesh) { continue; }

                    tempFilters.Add(meshFilter);
                }

                meshFilters = tempFilters.ToArray();
            }
            else // Return child MeshFilters along with parent's MeshFilter
            {
                meshFilters = GetComponentsInChildren<MeshFilter>(includeInactive: false);
            }

            return meshFilters;
        }

        /// <summary>
        /// Combine meshes from given MeshFilter Components. This will disable host GameObjects of those
        /// meshes.
        /// </summary>
        /// <param name="meshFilters">MeshFilter components that hold meshes to merge</param>
        /// <returns>Combined mesh</returns>
        private Mesh CombineGivenMeshes(MeshFilter[] meshFilters)
        {
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combineInstances[i].mesh = meshFilters[i].sharedMesh;

                if (meshFilters[i].transform != transform)
                {
                    // Only disable child objects
                    meshFilters[i].gameObject.SetActive(false);
                }
            }

            Mesh mesh = new()
            {
                name = $"MergedMesh_{name}"
            };
            mesh.CombineMeshes(combineInstances, mergeSubMeshes: true, useMatrices: false);

            return mesh;
        }
    }
}
