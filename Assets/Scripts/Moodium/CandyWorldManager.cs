using Moodium.Flow;
using Moodium.Interaction;
using Moodium.Reality;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Moodium.CandyWorld
{
    public sealed class CandyWorldManager : MonoBehaviour
    {
        CandyEnergyController m_Energy;
        CandyBurstController m_BurstController;
        CandyEnergyWristUI m_EnergyUI;
        CandySpatialMeshTransformationController m_SpatialTransformation;
        CandyFrostParticleController m_Frost;
        CandyRainRewardController m_RainReward;
        CandyWorldProgressController m_ProgressController;
        SpatialMeshTouchRipple m_SpatialTouchFeedback;
        CandyHandOpenBurstGesture m_HandOpenBurstGesture;
        bool m_Running;

        public float Energy => m_Energy != null ? m_Energy.Energy : 0f;

        public void Configure(Camera camera, GameObject glassPanelPrefab, TMP_FontAsset font,
            MoodiumWorldDefinition world, ARMeshManager meshManager, Material candyTransformationMaterial,
            Sprite wristRoundedSprite, Sprite wristCircleSprite, GameObject candyFrostPrefab)
        {
            m_Energy = GetComponent<CandyEnergyController>();
            if (m_Energy == null)
                m_Energy = gameObject.AddComponent<CandyEnergyController>();
            m_BurstController = GetComponent<CandyBurstController>();
            if (m_BurstController == null)
                m_BurstController = gameObject.AddComponent<CandyBurstController>();
            m_BurstController.Configure(world != null ? world.Prefabs : null);
            m_RainReward = GetComponent<CandyRainRewardController>();
            if (m_RainReward == null)
                m_RainReward = gameObject.AddComponent<CandyRainRewardController>();
            m_RainReward.Configure(camera, world != null ? world.Prefabs : null);
            m_SpatialTransformation = GetComponent<CandySpatialMeshTransformationController>();
            if (m_SpatialTransformation == null)
                m_SpatialTransformation = gameObject.AddComponent<CandySpatialMeshTransformationController>();
            m_SpatialTransformation.Configure(meshManager, candyTransformationMaterial);
            m_SpatialTouchFeedback = GetComponent<SpatialMeshTouchRipple>();
            if (m_SpatialTouchFeedback == null)
                m_SpatialTouchFeedback = gameObject.AddComponent<SpatialMeshTouchRipple>();
            m_SpatialTouchFeedback.Configure(
                meshManager,
                m_Energy,
                GetComponent<CandyEnvironmentShaderController>());
            m_SpatialTouchFeedback.SetInteractionEnabled(false);
            m_HandOpenBurstGesture = GetComponent<CandyHandOpenBurstGesture>();
            if (m_HandOpenBurstGesture == null)
                m_HandOpenBurstGesture = gameObject.AddComponent<CandyHandOpenBurstGesture>();
            m_HandOpenBurstGesture.Configure(m_Energy);
            m_HandOpenBurstGesture.SetGestureEnabled(false);

            if (m_Frost == null && candyFrostPrefab != null)
            {
                var frostObject = Instantiate(candyFrostPrefab, transform);
                frostObject.name = "Candy Frost Atmosphere";
                m_Frost = frostObject.GetComponent<CandyFrostParticleController>();
            }

            m_ProgressController = GetComponent<CandyWorldProgressController>();
            if (m_ProgressController == null)
                m_ProgressController = gameObject.AddComponent<CandyWorldProgressController>();
            m_ProgressController.Configure(
                camera, m_SpatialTransformation, m_Frost, m_RainReward,
                world != null ? world.Prefabs : null);

            if (m_EnergyUI == null)
            {
                var ui = new GameObject("Candy Energy Left Wrist HUD");
                ui.transform.SetParent(transform, false);
                m_EnergyUI = ui.AddComponent<CandyEnergyWristUI>();
                m_EnergyUI.Build(camera, font, wristRoundedSprite, wristCircleSprite);
            }
            StopExperience();
        }

        public void StartExperience(MoodiumWorldDefinition world)
        {
            m_BurstController.Configure(world != null ? world.Prefabs : null);
            m_RainReward.Configure(Camera.main, world != null ? world.Prefabs : null);
            m_ProgressController.Configure(
                Camera.main, m_SpatialTransformation, m_Frost, m_RainReward,
                world != null ? world.Prefabs : null);
            m_Energy.ResetEnergy();
            m_ProgressController.StartProgress();
            m_SpatialTouchFeedback?.SetInteractionEnabled(true);
            m_HandOpenBurstGesture?.SetGestureEnabled(true);
            m_EnergyUI.gameObject.SetActive(true);
            if (m_Running)
                return;
            m_Running = true;
            ChocolateCapsuleInteraction.AnyCapsulePinched += OnCapsulePinched;
            m_Energy.EnergyChanged += m_EnergyUI.SetProgress;
            m_Energy.EnergyChanged += m_ProgressController.SetProgress;
            m_EnergyUI.SetProgress(0f, 0f);
            Debug.Log("[Candy World] Reality Enhancement Mode experience started.");
        }

        public void StopExperience()
        {
            if (m_Running)
            {
                ChocolateCapsuleInteraction.AnyCapsulePinched -= OnCapsulePinched;
                m_Energy.EnergyChanged -= m_EnergyUI.SetProgress;
                m_Energy.EnergyChanged -= m_ProgressController.SetProgress;
            }
            m_Running = false;
            m_SpatialTouchFeedback?.SetInteractionEnabled(false);
            m_HandOpenBurstGesture?.SetGestureEnabled(false);
            if (m_EnergyUI != null)
                m_EnergyUI.gameObject.SetActive(false);
            m_ProgressController?.StopAndClear();
            m_BurstController?.StopAndClear();
        }

        void OnDestroy()
        {
            StopExperience();
        }

        void OnCapsulePinched(ChocolateCapsuleInteraction capsule, Vector3 position)
        {
            if (!m_Running || capsule == null || !capsule.gameObject.activeInHierarchy)
                return;
            var trackedSource = capsule.GetComponentInParent<ARTrackedObject>() != null ||
                                capsule.GetComponentInParent<ARTrackedImage>() != null;
            if (!trackedSource)
            {
                var poseFollower = capsule.GetComponent<TrackedObjectPoseFollower>();
                if (poseFollower != null && poseFollower.Anchor != null)
                    trackedSource = poseFollower.Anchor.GetComponentInParent<ARTrackedObject>() != null ||
                                    poseFollower.Anchor.GetComponentInParent<ARTrackedImage>() != null;
            }
            if (!trackedSource)
            {
                Debug.Log("[Candy World] Ignored a Chocolate Capsule that is not attached to a tracked source.");
                return;
            }
            m_ProgressController.SetOrigin(position);
            m_Energy.RegisterPinch();
            m_BurstController.Burst(position, capsule.transform);
        }
    }
}
