using UnityEngine;
using UnityEngine.Rendering;

namespace WeGrowPeas.PeaMerge
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PeaMergePiece : MonoBehaviour
    {
        public PeaMergeGame Owner { get; private set; }
        public int TierIndex { get; private set; }
        public float Radius { get; private set; }
        public bool IsDropped { get; private set; }
        public bool HasContact { get; private set; }
        public bool IsReserved { get; private set; }
        public float BornAt { get; private set; }
        public float OverflowTime { get; set; }
        public Rigidbody2D Body { get; private set; }
        public float Top => Body.position.y + Radius;
        private CircleCollider2D circle;
        private Vector2 pausedVelocity;
        private float pausedAngularVelocity;
        private bool frozen;

        public void Initialize(PeaMergeGame owner, int tierIndex)
        {
            Owner = owner;
            TierIndex = tierIndex;
            var tier = owner.Settings.tiers[tierIndex];
            Radius = tier.radius;
            Body = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            float scale = Radius / circle.radius;
            transform.localScale = new Vector3(scale, scale, 1f);
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.mass = Mathf.Max(0.4f, Radius * Radius * 3f);
            Body.gravityScale = 1f;
            Body.linearDamping = 0.15f;
            Body.angularDamping = 0.45f;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.simulated = false;
            circle.enabled = false;
            GetComponent<StartPeaAppearanceController>().SetAppearance(tier.appearanceIndex);
            var sorting = GetComponent<SortingGroup>();
            if (sorting != null) sorting.sortingOrder = 100;
            name = $"Pea {tierIndex + 1} - {tier.label}";
        }

        public void Drop(bool fromMerge = false)
        {
            IsDropped = true;
            HasContact = fromMerge;
            BornAt = Owner.GameClock;
            circle.enabled = true;
            Body.simulated = true;
            Body.WakeUp();
            var sorting = GetComponent<SortingGroup>();
            if (sorting != null) sorting.sortingOrder = TierIndex;
        }

        public void Reserve() => IsReserved = true;

        public void Retire()
        {
            IsReserved = true;
            Body.simulated = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public void Freeze(bool value)
        {
            if (!IsDropped || frozen == value) return;
            frozen = value;
            if (value)
            {
                pausedVelocity = Body.linearVelocity;
                pausedAngularVelocity = Body.angularVelocity;
                Body.simulated = false;
            }
            else
            {
                Body.simulated = true;
                Body.linearVelocity = pausedVelocity;
                Body.angularVelocity = pausedAngularVelocity;
            }
            foreach (var animator in GetComponentsInChildren<Animator>()) animator.speed = value ? 0f : 1f;
        }

        private void OnCollisionEnter2D(Collision2D collision) => Contact(collision);
        private void OnCollisionStay2D(Collision2D collision) => Contact(collision);

        private void Contact(Collision2D collision)
        {
            if (!IsDropped || IsReserved || Owner == null) return;
            HasContact = true;
            var other = collision.gameObject.GetComponent<PeaMergePiece>();
            if (other != null) Owner.TryMerge(this, other);
        }
    }
}
