using System;
using UnityEngine;

namespace Moodium.CandyWorld
{
    public sealed class CandyEnergyController : MonoBehaviour
    {
        [SerializeField, Range(1f, 100f)] float m_EnergyPerPinch = 5f;
        [SerializeField, Range(1f, 100f)] float m_MaxEnergy = 100f;

        float m_Energy;

        public float Energy => m_Energy;
        public float NormalizedEnergy => m_MaxEnergy > 0f ? m_Energy / m_MaxEnergy : 0f;
        public event Action<float, float> EnergyChanged;
        public event Action<int> StageReached;

        public void ResetEnergy()
        {
            m_Energy = 0f;
            EnergyChanged?.Invoke(m_Energy, NormalizedEnergy);
        }

        public void RegisterPinch()
        {
            var previous = m_Energy;
            m_Energy = Mathf.Min(m_MaxEnergy, m_Energy + m_EnergyPerPinch);
            EnergyChanged?.Invoke(m_Energy, NormalizedEnergy);

            CheckThreshold(previous, m_Energy, 20f, 20);
            CheckThreshold(previous, m_Energy, 50f, 50);
            CheckThreshold(previous, m_Energy, 80f, 80);
            CheckThreshold(previous, m_Energy, 100f, 100);
            Debug.Log($"[Candy Energy] {m_Energy:0}/{m_MaxEnergy:0}");
        }

        void CheckThreshold(float previous, float current, float threshold, int stage)
        {
            if (previous < threshold && current >= threshold)
                StageReached?.Invoke(stage);
        }
    }
}
