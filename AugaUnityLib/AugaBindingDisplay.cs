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
            string localizedKeyString;
            try
            {
                localizedKeyString = Localization.instance.GetBoundKeyString(keyName);
            }
            catch (System.Exception)
            {
                // Valheim 1.0 port (widgets-9): some gamepad-only defs (JoyRStickLeft/Right) have no sprite mapping.
                localizedKeyString = "";
            }

            var showMouse = -1;
            switch (control)
            {
                case "leftButton": showMouse = 0; break;
                case "rightButton": showMouse = 1; break;
                case "middleButton": showMouse = 2; break;
                case "backButton": showMouse = 3; break;
                case "forwardButton": showMouse = 4; break;
            }

            // Valheim 1.0 port (auga-lib-9 / widgets-9): 1.0 returns Input System display strings ("Numpad 1",
            // "Numpad +") instead of KeyCode names ("Keypad1", "Equals", "BackQuote", "Alpha1"), so the old string
            // fixups never fired. Shorten from the bound control path instead; everything else keeps the game's label.
            var shortLabel = ZInput.instance.m_buttons[keyName].Source == ZInput.InputSource.Gamepad ? null : GetShortLabel(control);
            if (shortLabel != null)
            {
                localizedKeyString = shortLabel;
            }

            SetText(localizedKeyString, showMouse);
        }

        /// <summary>
        /// Valheim 1.0 port (auga-lib-9): short labels for Input System controls whose display names do not fit Auga's
        /// key badge ("Numpad 1" -> "Num1", matching the pre-1.0 "Keypad1" -> "Num1" rewrite). null = keep the game's.
        /// </summary>
        public static string GetShortLabel(string control)
        {
            if (string.IsNullOrEmpty(control))
            {
                return null;
            }

            switch (control)
            {
                case "numpadDivide": return "Num/";
                case "numpadMinus": return "Num-";
                case "numpadMultiply": return "Num*";
                case "numpadEquals": return "Num=";
                case "numpadPeriod": return "Num.";
                case "numpadPlus": return "Num+";
                case "numpadEnter": return "NumEnter";
                case "equals": return "=";
                case "backquote": return "`";

                case "leftArrow": return "←";
                case "rightArrow": return "→";
                case "upArrow": return "↑";
                case "downArrow": return "↓";
            }

            if (control.Length == 7 && control.StartsWith("numpad") && char.IsDigit(control[6]))
            {
                return "Num" + control[6];
            }

            if (control.Length == 6 && control.StartsWith("digit") && char.IsDigit(control[5]))
            {
                return control.Substring(5);
            }

            return null;
        }

        /// <summary>
        /// Valheim 1.0 port (widgets-9): for gamepad bindings GetBoundKeyString returns a TMP sprite tag
        /// (&lt;sprite="xbox" name="button_a"&gt;), which UnityEngine.UI.Text prints literally. Turn it into a short text
        /// label ("A", "LB", "Dpad Up"); any other string is returned unchanged.
        /// </summary>
        public static string ToPlainText(string keyString)
        {
            if (string.IsNullOrEmpty(keyString) || !keyString.Contains("<sprite"))
            {
                return keyString ?? "";
            }

            const string nameAttribute = "name=\"";
            var nameStart = keyString.IndexOf(nameAttribute, System.StringComparison.Ordinal);
            if (nameStart < 0)
            {
                return "";
            }

            nameStart += nameAttribute.Length;
            var nameEnd = keyString.IndexOf('"', nameStart);
            var spriteName = nameEnd > nameStart ? keyString.Substring(nameStart, nameEnd - nameStart) : "";
            if (spriteName.StartsWith("button_"))
            {
                spriteName = spriteName.Substring("button_".Length);
                if (spriteName.Length <= 2)
                {
                    return spriteName.ToUpperInvariant();
                }
            }

            var words = spriteName.Split('_');
            for (var i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>Last segment of the first keyboard/mouse binding path, e.g. "leftButton"; "" when unbound.</summary>
        // Valheim 1.0 port (auga-lib-9): public so KeyBindDisplay shares the path-based short labels.
        public static string GetBoundControl(ZInput.ButtonDef button)
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
            // Valheim 1.0 port (widgets-9): these are legacy Text components; no TMP sprite tags.
            localizedKeyString = ToPlainText(localizedKeyString);
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
