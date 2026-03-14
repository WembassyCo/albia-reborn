using UnityEngine;

namespace Albia.Debug
{
    /// <summary>
    /// Draws selected creature paths and debug info
    /// MVP: Path visualization
    /// Full: Comprehensive debug draw
    /// </summary>
    public class DebugDrawer : MonoBehaviour
    {
        [SerializeField] private bool showPaths = true;
        [SerializeField] private bool showSensors = true;
        [SerializeField] private float pathHeight = 0.1f;
        
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            // Draw paths for selected creature
            if (Player.SelectionManager.Instance?.SelectedOrganism != null)
            {
                DrawSelectedCreatureGizmos();
            }
        }
        
        void DrawSelectedCreatureGizmos()
        {
            var creature = Player.SelectionManager.Instance.SelectedOrganism;
            if (creature == null) return;
            
            // Draw position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(creature.transform.position, 1f);
            
            // Draw forward
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(creature.transform.position, 
                creature.transform.position + creature.transform.forward * 2f);
            
            // Draw sensor range
            if (showSensors)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(creature.transform.position, 10f);
            }
        }
    }
}