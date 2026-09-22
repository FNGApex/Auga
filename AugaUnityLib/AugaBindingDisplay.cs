using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class AugaBindingDisplay : MonoBehaviour
    {
        public string AutomaticKeyName;

        public Text KeybindText;
        public GameObject KeybindBox;
        public Text LongKeybindText;
        public GameObject LongKeybindBox;
        public GameObject Mouse1;
        public GameObject Mouse2;
        public GameObject Mouse3;
        public GameObject MouseX;
        public Text MouseXText;

        public void Update()
        {
            if (!string.IsNullOrEmpty(AutomaticKeyName))
            {
                SetBinding(AutomaticKeyName);
            }
        }

        public void SetBinding(string keyName)
        {
            if (!ZInput.instance.m_buttons.ContainsKey(keyName))
            {
                Debug.LogError($"[AugaBindingDisplay.SetBinding] Couldn't find key: {keyName}");
                return;
            }

            // Valheim 1.0: buttons are Input System actions. The bound control is the last part of a path
            // such as "<Mouse>/leftButton" or "<Keyboard>/numpadPlus" instead of a legacy KeyCode.
            var control = GetBoundControl(ZInput.instance.m_buttons[keyName]);
            var localizedKeyString = Localization.instance.GetBoundKeyString(keyName);

            var showMouse = -1;
            switch (control)
            {
                case "leftButton": showMouse = 0; break;
                case "rightButton": showMouse = 1; break;
                case "middleButton": showMouse = 2; break;
                case "backButton": showMouse = 3; break;
                case "forwardButton": showMouse = 4; break;
            }

            switch (localizedKeyString)
            {
                case "Equals": localizedKeyString = "="; break;
                case "BackQuote": localizedKeyString = "`"; break;
            }

            if (localizedKeyString.StartsWith("Keypad"))
            {
                localizedKeyString = localizedKeyString.Replace("Keypad", "Num");
            }
            else if (localizedKeyString.StartsWith("Alpha"))
            {
                localizedKeyString = localizedKeyString.Replace("Alpha", "");
            }

            switch (control)
            {
                case "numpadDivide": localizedKeyString = localizedKeyString.Replace("Divide", "/"); break;
                case "numpadMinus": localizedKeyString = localizedKeyString.Replace("Minus", "-"); break;
                case "numpadMultiply": localizedKeyString = localizedKeyString.Replace("Multiply", "*"); break;
                case "numpadEquals": localizedKeyString = localizedKeyString.Replace("Equals", "="); break;
                case "numpadPeriod": localizedKeyString = localizedKeyString.Replace("Period", "."); break;
                case "numpadPlus": localizedKeyString = localizedKeyString.Replace("Plus", "+"); break;

                case "leftArrow": localizedKeyString = "←"; break;
                case "rightArrow": localizedKeyString = "→"; break;
                case "upArrow": localizedKeyString = "↑"; break;
                case "downArrow": localizedKeyString = "↓"; break;
            }

            SetText(localizedKeyString, showMouse);
        }

        /// <summary>Last segment of the first keyboard/mouse binding path, e.g. "leftButton"; "" when unbound.</summary>
        private static string GetBoundControl(ZInput.ButtonDef button)
        {
            var action = button?.ButtonAction;
            if (action == null)
            {
                return "";
            }

            foreach (var binding in action.bindings)
            {
                var path = binding.effectivePath;
                if (string.IsNullOrEmpty(path) || !(path.StartsWith("<Mouse>") || path.StartsWith("<Keyboard>")))
                {
                    continue;
                }

                var slash = path.LastIndexOf('/');
                return slash >= 0 ? path.Substring(slash + 1) : path;
            }

            return "";
        }

        public void SetText(string localizedKeyString, int showMouse = -1)
        {
            var isOneCharLong = localizedKeyString.Length == 1;
            (isOneCharLong ? KeybindText : LongKeybindText).text = localizedKeyString;
            KeybindBox.SetActive(showMouse < 0 && isOneCharLong);
            LongKeybindBox.SetActive(showMouse < 0 && !isOneCharLong);

            Mouse1.SetActive(showMouse == 0);
            Mouse2.SetActive(showMouse == 1);
            Mouse3.SetActive(showMouse == 2);
            MouseX.SetActive(showMouse > 2);
            MouseXText.text = (showMouse + 1).ToString();
        }
    }
}
