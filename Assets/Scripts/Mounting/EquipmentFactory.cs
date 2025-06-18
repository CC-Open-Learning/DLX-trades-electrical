using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VARLab.Interactions;

namespace VARLab.TradesElectrical
{
    public class EquipmentFactory : MonoBehaviour
    {
        public UnityEvent<Interactable> CreatedInteractable = new();
        
        [SerializeField] private GameObject[] mountablePrefabs;

        private readonly Dictionary<MountableName, GameObject> prefabMap = new();

        private void Start()
        {
            foreach (GameObject prefabInstance in mountablePrefabs)
            {
                Mountable mountable = prefabInstance.GetComponentInChildren<Mountable>();
                if (mountable == null)
                {
                    Debug.LogError($"{prefabInstance.name} is an invalid mountable prefab!");
                    continue;
                }
                prefabMap.Add(mountable.Name, prefabInstance);
            }
        }

        public GameObject Spawn(MountableName mountableName)
        {
            GameObject obj = Instantiate(prefabMap[mountableName]);

            var interactables = obj.GetComponentsInChildren<Interactable>(includeInactive: true);
            foreach (Interactable interactable in interactables)
            {
                CreatedInteractable?.Invoke(interactable);
            }

            return obj;
        }
    }
}
