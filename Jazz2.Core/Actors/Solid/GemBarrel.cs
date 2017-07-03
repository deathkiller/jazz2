using Jazz2.Game.Structs;

namespace Jazz2.Actors.Solid
{
    public class GemBarrel : BarrelContainer
    {
        public override void OnAttach(ActorInstantiationDetails details)
        {
            ushort red = details.Params[0];
            ushort green = details.Params[1];
            ushort blue = details.Params[2];
            ushort purble = details.Params[3];

            details.Params[0] = 0;

            base.OnAttach(details);

            GenerateContents(EventType.Gem, red, 0);
            GenerateContents(EventType.Gem, green, 1);
            GenerateContents(EventType.Gem, blue, 2);
            GenerateContents(EventType.Gem, purble, 3);
        }
    }
}