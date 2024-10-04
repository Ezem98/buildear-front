using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Components
{
    public class HeaderComponent : VisualElement
    {
        public VisualTreeAsset headerTemplate;

        public new class UxmlFactory : UxmlFactory<HeaderComponent> { }

        private Label _title;

        public HeaderComponent() : this(string.Empty) { }

        public HeaderComponent(string label = "")
        {
            Init(label);
        }

        private void Init(string label = "")
        {
            if (headerTemplate != null)
            {
                headerTemplate.CloneTree(this);
                _title = this.Q<Label>("sectionTitle");
                if (_title != null)
                {
                    _title.text = label;
                }
                else
                {
                    Debug.LogWarning("Label with name 'sectionTitle' not found in UXML.");
                }
            }
            else
            {
                Debug.LogError("VisualTreeAsset not found at path 'Assets/UI/Components/Header.uxml'.");
            }
        }

        public void SetTitle(string label)
        {
            if (_title != null)
            {
                _title.text = label;
            }
        }
    }
}
