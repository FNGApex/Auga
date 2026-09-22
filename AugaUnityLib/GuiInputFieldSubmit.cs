using System;
using GUIFramework;
using UnityEngine;

namespace AugaUnity
{
    public class GuiInputFieldSubmit : MonoBehaviour
    {
        public Action<string> m_onSubmit;
        private GuiInputField m_field;

        private void Awake() => m_field = GetComponent<GuiInputField>();

        private void Update()
        {
            // Valheim 1.0 port: only the minimap pin field assigns a handler. On the sign/portal/tame text box nothing
            // does, and since the ZInput port this Update is live there: no handler means hands off (no throw, no clearing).
            if (m_onSubmit == null)
                return;

            m_field.ActivateInputField();
            if (!(m_field.text != "") || !ZInput.GetKeyDown(KeyCode.Return) && !ZInput.GetKeyDown(KeyCode.KeypadEnter) && !ZInput.GetButtonDown("JoyButtonA"))
                return;
            
            m_onSubmit(m_field.text);
            m_field.text = "";
        }
    }
}