using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.AutoLANClientController
{
    public class NetworkSceneScript : NetworkBehaviour
    {
        public Button clientButton;
        public Text textResult;
        public GameObject panelClient, panelServer;

        private void Start()
        {
            clientButton.onClick.AddListener(ClientButton);

            panelServer.SetActive(false);
            panelClient.SetActive(false);

            if (isServer) panelServer.SetActive(true);
            if (isClient) panelClient.SetActive(true);
        }

        private void ClientButton()
        {
            if (isClient) CmdSendText("Text: " + Random.Range(0, 999));
        }

        [Command(requiresAuthority = false)]
        public void CmdSendText(string _value)
        {
            textResult.text = _value;
        }
    }
}