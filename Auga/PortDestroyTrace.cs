using UnityEngine;

namespace Auga
{
    /// <summary>Valheim 1.0 port debugging aid: says when and where an object dies.</summary>
    public class PortDestroyTrace : MonoBehaviour
    {
        private void OnDestroy()
        {
            var chain = name;
            for (var p = transform.parent; p != null; p = p.parent)
            {
                chain += " < " + p.name;
            }

            Debug.LogWarning($"[PortDiagnostics] DESTROYED frame {Time.frameCount}: {chain}");
        }
    }
}
