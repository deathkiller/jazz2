using Duality;

namespace Jazz2.Actors
{
    partial class Player
    {
        public enum ShieldType : byte
        {
            None,

            Fire,
            Water,
            Lightning,
            Laser
        }

        private ActorBase shieldComponent;
        private float shieldTime;

        public void SetShield(ShieldType shieldType, float secs)
        {
            if (shieldComponent != null) {
                ParentScene.RemoveObject(shieldComponent);
            }

            if (shieldType == ShieldType.None) {
                shieldTime = 0f;
                return;
            }

            shieldTime = secs * Time.FramesPerSecond;

            switch (shieldType) {
                case ShieldType.Fire:
                    shieldComponent = new ShieldComponent();
                    shieldComponent.OnAttach(new ActorInstantiationDetails {
                        Api = api
                    });
                    shieldComponent.Parent = this;
                    break;

                case ShieldType.Water:
                    // ToDo
                    break;

                case ShieldType.Lightning:
                    // ToDo
                    break;

                case ShieldType.Laser:
                    // ToDo
                    break;
            }
        }

        public void IncreaseTime(float secs)
        {
            if (shieldTime <= 0f) {
                return;
            }

            shieldTime += secs * Time.FramesPerSecond;
        }

        private class ShieldComponent : ActorBase
        {
            public override void OnAttach(ActorInstantiationDetails details)
            {
                base.OnAttach(details);

                RequestMetadata("Interactive/Shields");

                SetAnimation("Fire");
            }

            protected override void OnUpdate()
            {
                //base.OnUpdate();

                float z = (renderer.AnimTime / renderer.AnimDuration < 0.5f ? -2f : 2f);
                Transform.RelativePos = new Vector3(0f, 0f, z);
            }
        }
    }
}