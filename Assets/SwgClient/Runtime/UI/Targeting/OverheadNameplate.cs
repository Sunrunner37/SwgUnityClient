using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.UI.Hud;
using Swg.Client.Runtime.World.SceneObjects;
using TMPro;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Targeting
{
    /// <summary>Prefab-owned overhead name, HAM/posture, and weapon range modifier, shared by hover and selection.</summary>
    public sealed class OverheadNameplate : MonoBehaviour
    {
        public TMP_Text objectName;
        public TMP_Text attackModifier;
        public CreatureStatusDisplay status;
        public Vector2 screenOffset = new Vector2(0, 10);
        public Color neutralColor = Color.yellow;
        public Color hostileColor = new Color32(255, 40, 30, 255);
        public Color friendlyColor = Color.white;
        public Color memberColor = new Color(1, .15f, 1);
        public Color leaderColor = new Color(.6f, 1, .1f);
        public Color inRangeColor = Color.cyan;
        public Color outOfRangeColor = new Color32(115, 0, 0, 255);
        CurrentPlayer player;
        WorldState state;
        SceneObjectNameResolver names;

        void Awake()
        {
            player = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }

        public Color ColorFor(SceneObject sceneObject)
        {
            var group = player.Group;
            if (group != null && group.Contains(sceneObject.SwgId))
                return group.LeaderSwgId == sceneObject.SwgId ? leaderColor : memberColor;
            return sceneObject is TangibleSceneObject tangible && tangible.CanAttack
                ? tangible.IsHostile ? hostileColor : neutralColor : friendlyColor;
        }

        public void Show(SwgId swgId, Camera camera)
        {
            if (swgId == player.SwgId || camera == null || !state.SceneObjects.TryGet(swgId, out var sceneObject)
                || !ManagedSceneObject.TryGetObjectFrom(swgId, out var body)
                || !body.TryGetComponent<TargetableSceneObject>(out var selectable))
            {
                gameObject.SetActive(false);
                return;
            }
            var top = selectable.LocalBounds.center + Vector3.up * selectable.LocalBounds.extents.y;
            var screen = camera.WorldToScreenPoint(body.transform.TransformPoint(top));
            bool visible = screen.z > camera.nearClipPlane && camera.pixelRect.Contains(screen);
            gameObject.SetActive(visible);
            if (!visible) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, screen, null, out var point);
            ((RectTransform)transform).anchoredPosition = point + screenOffset;
            objectName.text = names.Resolve(sceneObject);
            objectName.color = ColorFor(sceneObject);
            var creature = sceneObject as CreatureSceneObject;
            bool vehicle = body.GetComponentInChildren<VehicleSeat>() != null;
            bool combatDetails = creature != null && creature.CanAttack && creature.Ham.HasAnAttackableElement;
            status.gameObject.SetActive(vehicle || combatDetails);
            attackModifier.gameObject.SetActive(combatDetails && !vehicle);
            if (vehicle && sceneObject is TangibleSceneObject tangible)
            {
                status.ham.ShowCondition(tangible.ConditionFraction);
                status.posture.SetActive(false);
                return;
            }
            if (!combatDetails) return;
            status.Show(creature);
            float distance = Vector3.Distance(ManagedSceneObject.CurrentPlayer.transform.position, body.transform.position);
            bool inRange = player.WeaponRanges.TryAttackModifierAt(distance, out int modifier);
            attackModifier.text = inRange ? modifier.ToString("+0;-0;0") : "--";
            attackModifier.color = inRange ? inRangeColor : outOfRangeColor;
        }
    }
}
