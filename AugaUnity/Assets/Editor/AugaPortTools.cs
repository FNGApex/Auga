using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Valheim 1.0 port: command-line entry points, so prefab work can be scripted and checked without opening the editor.
///   Unity.exe -batchmode -projectPath AugaUnity -executeMethod AugaPortTools.RenderPrefab -augaPrefab Assets/Prefabs/X.prefab -augaOut C:\out.png [-augaSize 1920x1080] -quit
/// Do NOT pass -nographics: rendering needs the GPU.
/// </summary>
public static class AugaPortTools
{
    private static string Arg(string name, string fallback = null)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    /// <summary>Instantiates a UI prefab under a camera-space canvas and writes what it looks like to a PNG.</summary>
    public static void RenderPrefab()
    {
        var prefabPath = Arg("-augaPrefab");
        var outPath = Arg("-augaOut");
        var size = Arg("-augaSize", "1920x1080").Split('x');
        int width = int.Parse(size[0]), height = int.Parse(size[1]);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[AugaPortTools] prefab not found: " + prefabPath);
            EditorApplication.Exit(2);
            return;
        }

        var cameraGo = new GameObject("RenderCamera");
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.18f, 0.2f, 1f);

        var canvasGo = new GameObject("RenderCanvas", typeof(Canvas), typeof(CanvasScaler));
        // World space: a screen-space canvas has no screen to size itself against in batch mode.
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        var canvasRect = (RectTransform)canvasGo.transform;
        canvasRect.sizeDelta = new Vector2(width, height);
        canvasRect.position = new Vector3(0f, 0f, 10f);
        camera.orthographicSize = height / 2f;
        camera.aspect = (float)width / height;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
        instance.SetActive(true);
        // Screen roots are authored for a full-screen parent: make them fill the render canvas.
        if (instance.transform is RectTransform rootRect)
        {
            if (Arg("-augaFill", "1") == "1")
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
            }
            else
            {
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
            }

            rootRect.localScale = Vector3.one;
        }
        // Screens usually ship with their root panel hidden; -augaShow "A/B;C" activates paths, default = first-level children.
        var show = Arg("-augaShow");
        if (show != null)
        {
            foreach (var path in show.Split(';'))
            {
                var child = instance.transform.Find(path);
                if (child != null)
                {
                    for (var t = child; t != null && t != instance.transform.parent; t = t.parent)
                    {
                        t.gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            foreach (Transform child in instance.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        // Nested canvases in the prefab must not re-route to a screen that does not exist.
        foreach (var nested in instance.GetComponentsInChildren<Canvas>(true))
        {
            Debug.Log("[AugaPortTools] nested canvas on '" + nested.name + "' isRoot=" + nested.isRootCanvas + " mode=" + nested.renderMode + " overrideSorting=" + nested.overrideSorting + " order=" + nested.sortingOrder + " enabled=" + nested.enabled);
            nested.overrideSorting = false;
        }

        foreach (var g in instance.GetComponentsInChildren<Graphic>(false))
        {
            if (g.canvas == null)
            {
                Debug.Log("[AugaPortTools] graphic without canvas: " + g.name);
                break;
            }
        }

        var firstGraphic = instance.GetComponentInChildren<Graphic>(false);
        if (firstGraphic != null)
        {
            Debug.Log("[AugaPortTools] first graphic '" + firstGraphic.name + "' canvas='" + (firstGraphic.canvas != null ? firstGraphic.canvas.name : "null") + "' worldPos=" + firstGraphic.transform.position + " lossyScale=" + firstGraphic.transform.lossyScale + " color=" + firstGraphic.color + " layer=" + firstGraphic.gameObject.layer);
        }
        // Screens fade in through a CanvasGroup / Animator at runtime; show them fully here.
        foreach (var group in instance.GetComponentsInChildren<CanvasGroup>(true))
        {
            group.alpha = 1f;
        }

        foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = false;
        }

        // A marker in the corner proves the render path itself works even when the prefab shows nothing.
        var marker = new GameObject("RenderMarker", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(canvasGo.transform, false);
        var markerRect = (RectTransform)marker.transform;
        markerRect.anchorMin = markerRect.anchorMax = markerRect.pivot = new Vector2(0f, 0f);
        markerRect.sizeDelta = new Vector2(24f, 24f);
        marker.GetComponent<Image>().color = Color.magenta;

        Debug.Log("[AugaPortTools] root '" + instance.name + "' rect=" + ((RectTransform)instance.transform).rect + " scale=" + instance.transform.localScale + " graphics=" + instance.GetComponentsInChildren<Graphic>(false).Length + " (active) / " + instance.GetComponentsInChildren<Graphic>(true).Length + " (all)");

        // Game scripts on the prefab must not run here (no Game, no Player): render the layout only.
        foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null && !(behaviour is UnityEngine.EventSystems.UIBehaviour))
            {
                behaviour.enabled = false;
            }
        }

        // uGUI builds canvas geometry over editor ticks, so render a few frames in, then leave. Run WITHOUT -quit.
        var frames = 0;
        EditorApplication.update += () =>
        {
            frames++;
            EditorApplication.QueuePlayerLoopUpdate();
            if (frames == 8)
            {
                try
                {
                    Capture(camera, width, height, outPath, prefabPath);
                    EditorApplication.Exit(0);
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugaPortTools] " + e);
                    EditorApplication.Exit(3);
                }
            }
        };
    }

    private static void Capture(Camera camera, int width, int height, string outPath, string prefabPath)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        camera.Render();

        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, texture.EncodeToPNG());
        Debug.Log("[AugaPortTools] rendered " + prefabPath + " -> " + outPath);
    }
}
