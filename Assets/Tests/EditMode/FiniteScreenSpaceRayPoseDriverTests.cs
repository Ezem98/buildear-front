using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

namespace BuildeAR.Tests.EditMode
{
    public class FiniteScreenSpaceRayPoseDriverTests
    {
        private static readonly Rect CameraPixelRect = new(0f, 0f, 1440f, 3088f);

        [TestCase(float.PositiveInfinity, float.NegativeInfinity)]
        [TestCase(float.NaN, 100f)]
        [TestCase(100f, float.NaN)]
        [TestCase(-1f, 100f)]
        [TestCase(100f, -1f)]
        [TestCase(1441f, 100f)]
        [TestCase(100f, 3089f)]
        public void IsValidScreenPosition_WithInvalidPosition_ReturnsFalse(float x, float y)
        {
            Assert.That(
                FiniteScreenSpaceRayPoseDriver.IsValidScreenPosition(
                    new Vector2(x, y),
                    CameraPixelRect
                ),
                Is.False
            );
        }

        [TestCase(0f, 0f)]
        [TestCase(720f, 1544f)]
        [TestCase(1439f, 3087f)]
        public void IsValidScreenPosition_WithPositionInsideCamera_ReturnsTrue(float x, float y)
        {
            Assert.That(
                FiniteScreenSpaceRayPoseDriver.IsValidScreenPosition(
                    new Vector2(x, y),
                    CameraPixelRect
                ),
                Is.True
            );
        }
    }
}
