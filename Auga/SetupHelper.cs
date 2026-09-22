using UnityEngine;

namespace Auga
{
    public static class SetupHelper
    {
        public static bool DirectObjectReplace(Transform original, GameObject prefab, string originalName)
        {
            return DirectObjectReplace(original, prefab, originalName, out _);
        }

        public static bool DirectObjectReplace(Transform original, GameObject prefab, string originalName, out GameObject newObject)
        {
            if (original.name != originalName)
            {
                newObject = null;
                return false;
            }

            var parent = original.parent;
            var siblingIndex = original.GetSiblingIndex();
            // Valheim 1.0 port: the vanilla object stays as a hidden donor for references Auga's copy lacks.
            var donor = PortCarryOver.MakeDonor(original.gameObject);

            newObject = PortCarryOver.InstantiateFilled(prefab, parent, donor);
            newObject.transform.SetSiblingIndex(siblingIndex);
            return true;
        }

        /// <summary>
        /// Places two child objects from the prefab in-place at the original, and at a sibling of the original.
        /// Call this method in an Awake prefix, and return !result to avoid calling the Awake of the original.
        /// </summary>
        /// <param name="primaryOriginal">Reference to the original object</param>
        /// <param name="prefab">Prefab that contains two children, one with a different name as the original, but will replace it, and one with the same name as the secondary object</param>
        /// <param name="originalName">The original gameObject name of the object to be replaced</param>
        /// <param name="secondaryName">The name of the secondary gameObject to be replaced. This should be the same in both the original and the new prefab</param>
        /// <param name="newPrimaryName">The name of the object in the prefab that will replace the original. It should be different than the originalName</param>
        /// <returns>true if the objects were replaced (this was called on the original), false otherwise (this was called on the replacement)</returns>
        public static bool IndirectTwoObjectReplace(Transform primaryOriginal, GameObject prefab, string originalName, string secondaryName, string newPrimaryName)
        {
            if (primaryOriginal.name.StartsWith("Auga"))
                return false;
            
            if (primaryOriginal.name != originalName)
            {
                return false;
            }

            if (!prefab)
            {
                Auga.LogWarning($"Prefab for {originalName} converting to {newPrimaryName} for {secondaryName} not found.");
                return false;
            }
                

            
            var parent = primaryOriginal.parent;
            if (parent != null)
            {
                
                var secondaryOriginal = parent.Find(secondaryName);
                if (secondaryOriginal != null)
                {
                    var secondarySiblingIndex = secondaryOriginal.GetSiblingIndex();
                    var primarySiblingIndex = primaryOriginal.GetSiblingIndex();
                    
                    // Valheim 1.0 port: keep both vanilla objects as hidden donors (see PortCarryOver).
                    var secondaryDonor = PortCarryOver.MakeDonor(secondaryOriginal.gameObject);
                    var primaryDonor = PortCarryOver.MakeDonor(primaryOriginal.gameObject);

                    var prefabWasActive = prefab.activeSelf;
                    prefab.SetActive(false);
                    var newPrefab = Object.Instantiate(prefab, parent);
                    prefab.SetActive(prefabWasActive);
                    var secondary = newPrefab.transform.Find(secondaryName);
                    var primary = newPrefab.transform.Find(newPrimaryName);
                    PortCarryOver.Fill(secondary.gameObject, secondaryDonor);
                    PortCarryOver.Fill(primary.gameObject, primaryDonor);

                    secondary.SetParent(parent);
                    primary.SetParent(parent);
                    secondary.SetSiblingIndex(secondarySiblingIndex);
                    primary.SetSiblingIndex(primarySiblingIndex);
                    if (Auga.PortDiagnosticsEnabled != null && Auga.PortDiagnosticsEnabled.Value)
                    {
                        secondary.gameObject.AddComponent<PortDestroyTrace>();
                        primary.gameObject.AddComponent<PortDestroyTrace>();
                        Debug.LogWarning($"[PortDiagnostics] indirect replace frame {Time.frameCount}: secondary '{secondary.name}' parent '{secondary.parent.name}', primary '{primary.name}' parent '{primary.parent.name}', holder '{newPrefab.name}' children {newPrefab.transform.childCount}");
                    }

                    PortCarryOver.WrapInCanvas(secondary.gameObject, secondaryDonor);
                    PortCarryOver.WrapInCanvas(primary.gameObject, primaryDonor);
                    // The children were detached while their (inactive) holder kept them asleep; wake them now.
                    newPrefab.SetActive(prefabWasActive);

                    Object.Destroy(newPrefab);
                    
                    return true;                    
                }
            }

            return false;
        }
    }
}
