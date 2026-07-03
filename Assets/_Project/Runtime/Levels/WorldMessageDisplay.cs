using TMPro;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class WorldMessageDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshPro messageText;
        [SerializeField] private string message = "Message";

        public string Message => message;

        private void Awake()
        {
            ResolveReferences();
            ApplyMessage();
        }

        private void OnValidate()
        {
            ResolveReferences();
            ApplyMessage();
        }

        public void SetMessage(string value)
        {
            message = value ?? string.Empty;
            ApplyMessage();
        }

        private void ApplyMessage()
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void ResolveReferences()
        {
            if (messageText == null)
            {
                messageText = GetComponent<TextMeshPro>();
            }
        }
    }
}
