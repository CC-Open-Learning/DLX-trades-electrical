using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VARLab.TradesElectrical
{

    public class MountingAnimationHandler : MonoBehaviour
    {
        /// <summary>
        /// Data class to hold animation details for drilling
        /// </summary>
        /// <remarks>
        /// Used for unity editor to easily add animation controller with associated mountable types to play animation
        /// with given trigger. 
        /// </remarks>
        [System.Serializable]
        public class BoxAnimations
        {
            public RuntimeAnimatorController MountAnimationController;
            public string AnimationTrigger;
            [Tooltip("Mountable types that will use controller")]
            public MountableName[] MountableTypes;
        }

        [field: SerializeField] public GameObject Drill { get; private set;}
        [field: SerializeField] public BoxAnimations[] AnimationControllers { get; private set; }

        /// Used for quick look up via mountable name rather than parsing through an BoxAnimations array to get animation
        private readonly Dictionary<MountableName, BoxAnimations> boxAnimationControllersMap = new();

        private void Start()
        {
            AddSerializedAnimationControllersArrayToDict();
        }

        /// <summary>
        /// Parses through Animation Controller array that is serialized into a dictionary for quick lookup
        /// </summary>
        private void AddSerializedAnimationControllersArrayToDict()
        {
            foreach (BoxAnimations boxAnim in AnimationControllers)
            {
                foreach (MountableName mountable in boxAnim.MountableTypes)
                {
                    boxAnimationControllersMap.Add(mountable, boxAnim);
                }
            }
        }

        /// <summary>
        /// Invoked by <see cref="BoxMounter.mountingFinalized"/>
        /// Adds animator to box and plays animation by given trigger string
        /// </summary>
        /// <param name="mountLoc">Mounted location of device that needs to be animated for drill</param>
        public void TriggerDrillAnimation(MountLocation mountLoc)
        {
            // Spawn Drill
            var spawnedDrill = SpawnDrillAsChildOfMountObject(mountLoc);
            mountLoc.MountedItem.gameObject.AddComponent<Animator>();
            Animator animator = mountLoc.MountedItem.gameObject.GetComponent<Animator>();
            MountableName mountable = mountLoc.MountedItem.GetComponentInChildren<Mountable>().Name;
            
            BoxAnimations boxAnimation = boxAnimationControllersMap[mountable]; 
            animator.runtimeAnimatorController = boxAnimation.MountAnimationController;
            animator.SetTrigger(boxAnimation.AnimationTrigger);
        }

        private GameObject SpawnDrillAsChildOfMountObject(MountLocation mountLoc)
        {
            GameObject spawnedDrill = Instantiate(Drill, mountLoc.MountedItem.transform); // Animation requires drill to be child of  object
            spawnedDrill.name = "Power Drill";
            return spawnedDrill;
        }
    }
}
