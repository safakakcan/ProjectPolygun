using UnityEngine;

namespace Mirror.Examples.Hex2D
{
    [AddComponentMenu("")]
    public class Hex2DPlayer : NetworkBehaviour
    {
        [Range(1, 20)] public float speed = 15f;

        [Header("Diagnostics")] [ReadOnly] [SerializeField]
        private HexSpatialHash2DInterestManagement.CheckMethod checkMethod;

        private void Awake()
        {
#if UNITY_2022_2_OR_NEWER
            checkMethod = FindAnyObjectByType<HexSpatialHash2DInterestManagement>().checkMethod;
#else
            checkMethod = FindObjectOfType<HexSpatialHash2DInterestManagement>().checkMethod;
#endif
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");
            Vector3 dir;

            if (checkMethod == HexSpatialHash2DInterestManagement.CheckMethod.XY_FOR_2D)
                dir = new Vector3(h, v, 0);
            else
                dir = new Vector3(h, 0, v);

            transform.position += dir.normalized * (Time.deltaTime * speed);
        }

        private void OnGUI()
        {
            if (isLocalPlayer)
            {
                GUILayout.BeginArea(new Rect(10, Screen.height - 25, 300, 300));
                GUILayout.Label("Use WASD to move");
                GUILayout.EndArea();
            }
        }
    }
}