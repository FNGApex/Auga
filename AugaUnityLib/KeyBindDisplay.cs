using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    [RequireComponent(typeof(Text))]
    public class KeyBindDisplay : MonoBehaviour
    {
        public string ZInputId;

        private Text _text;
        private string _key;

        public virtual void Awake()
        {
            _text = GetComponent<Text>();
        }

        public virtual void Start()
        {
            Update();
        }

        public virtual void Update()
        {
            string key;
            try
            {
                key = Localization.instance.GetBoundKeyString(ZInputId);
            }
            catch (System.Exception)
            {
                // Valheim 1.0 port (widgets-9): gamepad-only defs without a sprite mapping throw.
                key = "";
            }

            // Valheim 1.0 port (auga-lib-9 / widgets-9): shorten Input System names ("Numpad 1" -> "Num1") from the
            // bound control, and turn gamepad TMP sprite tags into text; this label is a legacy UnityEngine.UI.Text.
            if (ZInput.instance != null && ZInput.instance.m_buttons.TryGetValue(ZInputId, out var button)
                && button.Source != ZInput.InputSource.Gamepad)
            {
                key = AugaBindingDisplay.GetShortLabel(AugaBindingDisplay.GetBoundControl(button)) ?? key;
            }

            key = AugaBindingDisplay.ToPlainText(key);
            if (key != _key)
            {
                _key = key;
                _text.text = _key;
            }
        }
    }
}
