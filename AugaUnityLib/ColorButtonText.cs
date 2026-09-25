using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class ColorButtonText : Button
    {
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            var allValueSets = GetComponents<ColorButtonTextValues>();
            foreach (var values in allValueSets)
            {
                var targetColor =
                    state == SelectionState.Disabled ? values.TextColors.disabledColor :
                    state == SelectionState.Highlighted ? values.TextColors.highlightedColor :
                    state == SelectionState.Normal ? values.TextColors.normalColor :
                    state == SelectionState.Pressed ? values.TextColors.pressedColor :
                    state == SelectionState.Selected ? values.TextColors.selectedColor : Color.white;

                // AUDIT2 api-7: every Auga button prefab leaves the legacy Text ref empty (their labels are TMP), so
                // per-state colours set through the API (Button_SetTextColors) fall back to the button's TMP label.
                Graphic label = values.Text;
                if (label == null && values.UseChildLabel)
                {
                    label = GetComponentInChildren<TMP_Text>(true);
                }

                if (label != null)
                {
                    label.CrossFadeColor(targetColor, instant ? 0 : values.TextColors.fadeDuration, true, true);
                }
            }

            base.DoStateTransition(state, instant);
        }
    }

    public class ColorButtonTextValues : MonoBehaviour
    {
        public Text Text;
        public ColorBlock TextColors = ColorBlock.defaultColorBlock;
        /// <summary>Set by API.Button_SetTextColors: tint the child TMP label when <see cref="Text"/> is empty.</summary>
        [System.NonSerialized] public bool UseChildLabel;
    }
}
