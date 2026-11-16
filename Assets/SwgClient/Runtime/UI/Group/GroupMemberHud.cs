using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.UI.Hud;
using Swg.Client.Runtime.UI.Navigation;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Group
{
    public sealed class GroupMemberHud : MonoBehaviour, IPointerClickHandler
    {
        public TMP_Text memberName;
        public TMP_Text status;
        public TMP_Text worldName;
        public CreatureStatusDisplay statusDisplay;
        public RawImage direction;
        public Color memberColor = new Color(1, .15f, 1);
        public Color leaderColor = new Color(.6f, 1, .1f);
        string displayedName;
        int displayedDistance = int.MinValue;
        GroupHud owner;
        public SwgId SwgId { get; private set; }
        public void Bind(GroupHud owner, SwgId swgId) { this.owner = owner; SwgId = swgId; }
        public void Show(GroupMember member, bool leader, CreatureSceneObject creature, GameObject body, Transform player, Camera camera)
        {
            bool available = creature != null && body != null && player != null && camera != null;
            var color = leader ? leaderColor : memberColor;
            memberName.color = color;
            int distance = available ? Mathf.RoundToInt(Vector3.Distance(player.position, body.transform.position)) : -1;
            if (displayedName != member.Name || displayedDistance != distance)
            {
                displayedName = member.Name;
                displayedDistance = distance;
                memberName.text = member.Name + (available ? " (" + distance + "m)" : "");
                worldName.text = member.Name;
            }
            status.gameObject.SetActive(!available);
            status.text = "Out of range / off planet";
            statusDisplay.Show(available ? creature : null);
            direction.gameObject.SetActive(available);
            worldName.gameObject.SetActive(false);
            if (!available) return;
            direction.color = color;
            direction.rectTransform.localRotation = Quaternion.Euler(0, 0, NavigationArrow.Angle(NavigationArrow.Bearing(camera, player.position, body.transform.position)));
            var top = body.TryGetComponent<TargetableSceneObject>(out var selectable)
                ? body.transform.TransformPoint(selectable.LocalBounds.center + Vector3.up * selectable.LocalBounds.extents.y) : body.transform.position + Vector3.up * 2;
            var screen = camera.WorldToScreenPoint(top);
            if (screen.z < camera.nearClipPlane || screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height) return;
            worldName.color = color;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)worldName.transform.parent, screen, null, out var local);
            worldName.rectTransform.anchoredPosition = local + Vector2.up * 12;
            worldName.gameObject.SetActive(true);
        }
        public void OnPointerClick(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Right) owner.OpenMenu(SwgId, pointer.position);
            else if (pointer.button == PointerEventData.InputButton.Left) owner.Select(SwgId);
        }
    }
}
