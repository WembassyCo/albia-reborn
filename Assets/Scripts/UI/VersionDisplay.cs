using UnityEngine;
using UnityEngine.UI;
using Albia.Core;

namespace Albia.UI
{
    /// <summary>
    /// Displays version in corner of screen
    /// </summary>
    public class VersionDisplay : MonoBehaviour
    {
        [SerializeField] private Text versionText;
        
        void Start()
        {
            if (versionText != null)
            {
                versionText.text = GameVersion.GetVersionString();
            }
        }
    }
}