using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace DemonLord.Tests.EditMode
{
    public sealed class ProjectBootstrapTests
    {
        private static readonly string[] ExpectedScenePaths =
        {
            "Assets/_Project/Scenes/00_Boot.unity",
            "Assets/_Project/Scenes/10_Frontend.unity",
            "Assets/_Project/Scenes/90_GameShell.unity",
            "Assets/_Project/Scenes/91_LabInterior.unity",
            "Assets/_Project/Scenes/92_BureauCourtyard.unity",
        };

        [Test]
        public void BootstrapScenes_AreConfiguredInExpectedOrder()
        {
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            CollectionAssert.AreEqual(ExpectedScenePaths, scenePaths);
        }
    }
}
