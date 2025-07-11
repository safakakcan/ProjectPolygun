using UnityEngine;

namespace Mirror.Examples.CharacterSelection
{
    public class CharacterSelection : NetworkBehaviour
    {
        public Transform floatingInfo;

        [SyncVar] public int characterNumber;

        public TextMesh textMeshName;

        [SyncVar(hook = nameof(HookSetName))] public string playerName = "";

        [SyncVar(hook = nameof(HookSetColor))] public Color characterColour;

        public MeshRenderer[] characterRenderers;
        private Material cachedMaterial;

        private void OnDestroy()
        {
            if (cachedMaterial) Destroy(cachedMaterial);
        }

        private void HookSetName(string _old, string _new)
        {
            //Debug.Log("HookSetName");
            AssignName();
        }

        private void HookSetColor(Color _old, Color _new)
        {
            //Debug.Log("HookSetColor");
            AssignColours();
        }

        public void AssignColours()
        {
            foreach (var meshRenderer in characterRenderers)
            {
                cachedMaterial = meshRenderer.material;
                cachedMaterial.color = characterColour;
            }
        }

        public void AssignName()
        {
            textMeshName.text = playerName;
        }

        // To change server controlled sync vars, clients end Commands, and the hooks will fire
        // Although not used in this example, we could change some character aspects without replacing current prefab.
        //[Command]
        //public void CmdSetupCharacter(string _playerName, Color _characterColour)
        //{
        //    playerName = _playerName;
        //    characterColour = _characterColour;
        //}
    }
}