using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Navigation
{
    public sealed class WaypointTrackerRow : MonoBehaviour
    {
        public TMP_Text waypointName;
        public TMP_Text distance;
        public RawImage direction;
        int displayedDistance = -1;

        public void Bind(string name, Color color)
        {
            waypointName.text = name;
            direction.color = color;
        }

        public void Show(int meters, Vector2 bearing)
        {
            if (displayedDistance != meters)
            {
                displayedDistance = meters;
                distance.text = meters + "m";
            }
            direction.rectTransform.localRotation = Quaternion.Euler(0, 0, NavigationArrow.Angle(bearing));
        }
    }
}
