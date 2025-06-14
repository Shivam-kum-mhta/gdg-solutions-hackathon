using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ARLabs.UI
{
    public class TextField : Field
    {
        public TMP_InputField inputField;
        public TextFieldInfo textInfo;

        // calling some event or something to notify text changing
        // we'll subscribe to this in the apparatus

        public void Initialize(TextFieldInfo _textFieldInfo)
        {
            textInfo.field = this;

            textInfo = _textFieldInfo;

            labelText.text = _textFieldInfo.label;
            inputField.text = textInfo.value;
            inputField.placeholder.GetComponent<TMP_Text>().text = textInfo.placeholder;

            // Apply readonly state
            inputField.interactable = !textInfo.isReadOnly;

            _initialized = true;
        }

        public void OnChange()
        {
            if (textInfo.value != inputField.text && _initialized)
            {
                textInfo.value = inputField.text;
                textInfo.OnChange?.Invoke(textInfo.value);
            }
        }

        public void SetValue(string _value)
        {
            inputField.text = _value;
            OnChange();
        }
    }
}
