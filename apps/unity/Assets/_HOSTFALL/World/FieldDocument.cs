using Hostfall.Field;
using Hostfall.Interaction;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class FieldDocument : MonoBehaviour, IInteractable
    {
        [SerializeField] private string documentId;
        private FieldSystemUI field;
        public string Prompt => "[E] Прочитать запись";
        public void Configure(string id) { documentId = id; }
        public void Initialize(FieldSystemUI view) { field = view; }
        public void Interact() => field.OpenDocument(documentId);
    }
}
