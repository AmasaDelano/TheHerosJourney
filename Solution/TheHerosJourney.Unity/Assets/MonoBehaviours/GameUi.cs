using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.MonoBehaviours
{
    public class GameUi : MonoBehaviour
    {
        public float buttonFadeInSeconds = 0.5F;
        public float buttonFadeOutSeconds = 0.1F;
        public float menuFadeInSeconds = 0.5F;
        public float menuFadeOutSeconds = 0.1F;

        public int lettersPerSecond = 25;
        public float currentCharacterIndex = 0F;
        public bool stillRevealingText = true;

        [Header("Choice Buttons")]
        [SerializeField]
#pragma warning disable 0649
        internal GameObject choice1Button;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal GameObject choice2Button;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup clickToContinueText;
#pragma warning restore 0649

        [Header("Sound Effects")]
        [SerializeField]
#pragma warning disable 0649
        internal SoundEffects soundEffects;
#pragma warning restore 0649

        [Header("Various Menus, etc.")]
        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI morale;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup almanacMenu;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI almanacText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI almanacBadgeNumber;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup inventoryMenu;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI inventoryText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI inventoryBadgeNumber;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup exitWarning;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal GameObject scrollToEndButton;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal GameObject tutorial;
#pragma warning restore 0649

        [Header("Feedback Form")]
        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup feedbackFormParent;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal ToggleGroup feedbackType;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal TMP_InputField feedbackText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup feedbackForm;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal CanvasGroup feedbackThankYou;
#pragma warning restore 0649

        [Header("Story Scroll")]

        [SerializeField]
#pragma warning disable 0649
        internal TextMeshProUGUI storyText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        internal RectTransform scrollContainer;
#pragma warning restore 0649
    }
}
