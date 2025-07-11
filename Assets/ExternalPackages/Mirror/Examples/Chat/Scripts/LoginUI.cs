using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.Chat
{
    public class LoginUI : MonoBehaviour
    {
        public static LoginUI instance;

        [Header("UI Elements")] [SerializeField]
        internal InputField networkAddressInput;

        [SerializeField] internal InputField usernameInput;
        [SerializeField] internal Button hostButton;
        [SerializeField] internal Button clientButton;
        [SerializeField] internal Text errorText;

        private string originalNetworkAddress;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            // if we don't have a networkAddress, set a default one.
            if (string.IsNullOrWhiteSpace(NetworkManager.singleton.networkAddress))
                NetworkManager.singleton.networkAddress = "localhost";

            // cache the original networkAddress for resetting if blank.
            originalNetworkAddress = NetworkManager.singleton.networkAddress;
        }

        private void Update()
        {
            // bidirectional sync of networkAddressInput and NetworkManager.networkAddress
            // Order of operations is important here...Don't switch the order of these steps.
            if (string.IsNullOrWhiteSpace(NetworkManager.singleton.networkAddress))
                NetworkManager.singleton.networkAddress = originalNetworkAddress;

            if (networkAddressInput.text != NetworkManager.singleton.networkAddress)
                networkAddressInput.text = NetworkManager.singleton.networkAddress;
        }

        // Called by UI element UsernameInput.OnValueChanged
        public void ToggleButtons(string username)
        {
            hostButton.interactable = !string.IsNullOrWhiteSpace(username);
            clientButton.interactable = !string.IsNullOrWhiteSpace(username);
        }
    }
}