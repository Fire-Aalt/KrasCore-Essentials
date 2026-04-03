using Unity.Entities;

namespace KrasCore.Essentials
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SceneLoaderPauseSystem : ISystem
    {
    }
}