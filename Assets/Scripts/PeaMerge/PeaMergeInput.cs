using UnityEngine;
using UnityEngine.EventSystems;

namespace WeGrowPeas.PeaMerge
{
    /// <summary>One pointer owns a gesture; UI buttons and cancelled touches never drop a pea.</summary>
    public sealed class PeaMergeInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public PeaMergeGame game;
        public Camera gameCamera;
        private int? pointerId;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || pointerId.HasValue || !game.CanDrop) return;
            pointerId = eventData.pointerId;
            Move(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (pointerId == eventData.pointerId) Move(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerId != eventData.pointerId) return;
            pointerId = null;
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).fingerId == eventData.pointerId && Input.GetTouch(i).phase == TouchPhase.Canceled) return;
            if (!gameCamera.pixelRect.Contains(eventData.position)) return;
            Move(eventData.position);
            game.Drop();
        }

        private void Update()
        {
            if (game.State != PeaMergeGame.GameState.Playing) pointerId = null;
        }

        private void Move(Vector2 position)
        {
            var world = gameCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, -gameCamera.transform.position.z));
            game.Aim(world.x);
        }

        private void OnApplicationFocus(bool focus) { if (!focus) pointerId = null; }
        private void OnDisable() => pointerId = null;
    }
}
