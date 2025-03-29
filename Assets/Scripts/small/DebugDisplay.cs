// using TMPro;
// using UnityEngine;

// public class XRDebugDisplay : MonoBehaviour
// {
//     [SerializeField] private XRManager xrManager;
//     [SerializeField] private TextMeshPro debugText;

//     void Update()
//     {
//         if (xrManager != null && debugText != null)
//         {
//             if (xrManager.IsHMDConnected)
//             {
//                 Vector3 pos = xrManager.GetHeadPosition();
//                 Quaternion rot = xrManager.GetHeadRotation();
//                 // var rot = rot
//                 debugText.text = $"{pos}\n {rot.eulerAngles}";
//             }
//             else
//             {
//                 debugText.text = "HMD Not Connected";
//             }
//         }
//     }
// }