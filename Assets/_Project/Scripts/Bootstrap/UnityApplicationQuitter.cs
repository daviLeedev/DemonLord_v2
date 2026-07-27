using DemonLord.Application;
using UnityEngine;

namespace DemonLord.Bootstrap
{
    public sealed class UnityApplicationQuitter : IApplicationQuitter
    {
        public void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("Game quit requested. Application.Quit is skipped in the Unity Editor.");
#else
            Application.Quit();
#endif
        }
    }
}
