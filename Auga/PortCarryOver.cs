using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port. Auga's prefabs carry their own copies of vanilla components (Menu, TextViewer,
    /// KeyHints...) as they were serialized in 2023, so every reference field the game has added or renamed
    /// since is empty on them and vanilla code throws on first use. Rather than destroying the vanilla
    /// object that is being replaced, it is kept as an inactive "donor": each reference that is empty on
    /// Auga's component is pointed at the donor's object. Vanilla finds what it expects (hidden, because
    /// the donor is inactive), and the log lists those fields - that list is the UI Auga still has to build.
    /// </summary>
    public static class PortCarryOver
    {
        public const string DonorSuffix = "_VanillaDonor";

        /// <summary>Hide the vanilla object and get it out of the way of path lookups. Safe inside its own Awake prefix.</summary>
        public static GameObject MakeDonor(GameObject original)
        {
            original.SetActive(false);
            original.name += DonorSuffix;
            original.transform.SetAsLastSibling();
            return original;
        }

        /// <summary>
        /// Instantiates <paramref name="prefab"/> with its Awake held back until the empty references have
        /// been filled from <paramref name="donor"/>, so an Awake that already uses the new fields works too.
        /// </summary>
        public static GameObject InstantiateFilled(GameObject prefab, Transform parent, GameObject donor)
        {
            var wasActive = prefab.activeSelf;
            prefab.SetActive(false);
            GameObject instance;
            try
            {
                instance = Object.Instantiate(prefab, parent, false);
            }
            finally
            {
                prefab.SetActive(wasActive);
            }

            Fill(instance, donor);
            WrapInCanvas(instance, donor);
            instance.SetActive(wasActive);
            return instance;
        }

        /// <summary>For every game component type present on both roots, fill the empty references of the new one.</summary>
        public static void Fill(GameObject target, GameObject donor)
        {
            if (target == null || donor == null)
            {
                return;
            }

            foreach (var donorComponent in donor.GetComponents<MonoBehaviour>())
            {
                if (donorComponent == null)
                {
                    continue;
                }

                var type = donorComponent.GetType();
                // Only the game's own components: Unity's and Auga's serialize the same today as in 2023.
                if (type.Assembly != typeof(Hud).Assembly && type.Assembly != typeof(GuiBar).Assembly)
                {
                    continue;
                }

                // Tooltip anchors taken from a hidden donor would parent the live tooltip under something inactive;
                // UITooltip copes with empty anchors.
                if (type == typeof(UITooltip))
                {
                    continue;
                }

                var targetComponent = target.GetComponent(type) ?? target.GetComponentInChildren(type, true);
                if (targetComponent != null)
                {
                    FillComponent((MonoBehaviour)targetComponent, donorComponent);
                }
            }
        }

        /// <summary>
        /// In 1.0 every vanilla screen root is its own full-screen Canvas (with CanvasScaler, GuiScaler and
        /// GraphicRaycaster) under an empty 0x0 "IngameGui"; in 2023 they all sat under one shared full-screen
        /// canvas. Auga's replacements were laid out against that shared parent - some stretch over it, some are
        /// small widgets anchored to a corner of it - so each one gets a full-screen canvas holder cloned from
        /// the vanilla object it replaces, and keeps its own anchors inside it. Without a canvas they are never
        /// drawn at all; copying the vanilla rect onto them instead would throw their own placement away.
        /// </summary>
        public static void WrapInCanvas(GameObject target, GameObject donor)
        {
            if (target == null || donor == null)
            {
                return;
            }

            var donorCanvas = donor.GetComponent<Canvas>();
            if (donorCanvas == null || target.GetComponent<Canvas>() != null || target.transform.parent == null
                || target.transform.parent.name.EndsWith(CanvasSuffix))
            {
                return;
            }

            var holder = new GameObject(target.name + CanvasSuffix, typeof(RectTransform));
            // Inactive while it is assembled: GuiScaler caches its CanvasScaler in Awake, so both must exist first.
            holder.SetActive(false);
            holder.layer = donor.layer;
            var holderRect = (RectTransform)holder.transform;
            holderRect.SetParent(target.transform.parent, false);
            holderRect.SetSiblingIndex(target.transform.GetSiblingIndex());
            if (donor.transform is RectTransform donorRect)
            {
                holderRect.anchorMin = donorRect.anchorMin;
                holderRect.anchorMax = donorRect.anchorMax;
                holderRect.pivot = donorRect.pivot;
                holderRect.sizeDelta = donorRect.sizeDelta;
                holderRect.anchoredPosition = donorRect.anchoredPosition;
                holderRect.localScale = donorRect.localScale;
            }

            var canvas = holder.AddComponent<Canvas>();
            canvas.renderMode = donorCanvas.renderMode;
            canvas.worldCamera = donorCanvas.worldCamera;
            canvas.planeDistance = donorCanvas.planeDistance;
            canvas.pixelPerfect = donorCanvas.pixelPerfect;
            canvas.overrideSorting = donorCanvas.overrideSorting;
            canvas.sortingLayerID = donorCanvas.sortingLayerID;
            canvas.sortingOrder = donorCanvas.sortingOrder;
            canvas.additionalShaderChannels = donorCanvas.additionalShaderChannels;

            // CanvasScaler, GuiScaler (game), GraphicRaycaster: same types, same serialized values.
            foreach (var donorBehaviour in donor.GetComponents<Behaviour>())
            {
                if (donorBehaviour == null || donorBehaviour is Canvas)
                {
                    continue;
                }

                var type = donorBehaviour.GetType();
                if (type != typeof(UnityEngine.UI.CanvasScaler) && type != typeof(UnityEngine.UI.GraphicRaycaster) && type.Name != "GuiScaler")
                {
                    continue;
                }

                var copy = holder.AddComponent(type);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(donorBehaviour), copy);
            }

            // Some Auga scripts read their own sibling index (AugaTopLeftMessage fades out and destroys itself when
            // it is child 0). Under the 2023 shared parent these roots were never first, so keep them off index 0.
            var pad = new GameObject("AugaCanvasPad", typeof(RectTransform));
            pad.transform.SetParent(holderRect, false);
            pad.SetActive(false);

            target.transform.SetParent(holderRect, false);
            // A root that stretches over its whole parent has no meaningful offset; AugaMenu's prefab carries a stray
            // (-1235, -1042) from the editor that would put the pause menu and compendium off screen.
            if (target.transform is RectTransform targetRect && targetRect.anchorMin == Vector2.zero && targetRect.anchorMax == Vector2.one
                && targetRect.sizeDelta == Vector2.zero)
            {
                targetRect.anchoredPosition = Vector2.zero;
            }

            holder.SetActive(true);
            Debug.LogWarning($"[PortDiagnostics] '{target.name}': placed in its own full-screen canvas (order {donorCanvas.sortingOrder}) like the vanilla '{donor.name}'");
        }

        public const string CanvasSuffix = "_AugaCanvas";
        private static void FillComponent(MonoBehaviour target, MonoBehaviour donor)
        {
            var filled = new List<string>();
            for (var t = target.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }

                    if (field.IsNotSerialized || field.IsInitOnly)
                    {
                        continue;
                    }

                    var donorValue = field.GetValue(donor);
                    var targetValue = field.GetValue(target);

                    if (typeof(Object).IsAssignableFrom(field.FieldType))
                    {
                        if ((Object)targetValue == null && (Object)donorValue != null)
                        {
                            field.SetValue(target, donorValue);
                            filled.Add(field.Name);
                        }
                    }
                    else if (IsObjectCollection(field.FieldType))
                    {
                        // A list that came out of the bundle empty while vanilla has entries is a new or renamed field.
                        if (Count(targetValue) == 0 && Count(donorValue) > 0)
                        {
                            field.SetValue(target, donorValue);
                            filled.Add(field.Name + "[]");
                        }
                    }
                }
            }

            if (filled.Count > 0)
            {
                Debug.LogWarning($"[PortDiagnostics] {target.GetType().Name} on '{target.name}': {filled.Count} empty references filled from the hidden vanilla object: {string.Join(", ", filled)}");
            }
        }

        private static bool IsObjectCollection(Type type)
        {
            if (type.IsArray)
            {
                return typeof(Object).IsAssignableFrom(type.GetElementType());
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
                   && typeof(Object).IsAssignableFrom(type.GetGenericArguments()[0]);
        }

        private static int Count(object collection)
        {
            return collection is ICollection c ? c.Count : 0;
        }
    }
}
