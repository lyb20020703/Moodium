using UnityEngine;

namespace Moodium.Calibration
{
    public enum CalibrationAction
    {
        None,
        Confirm,
        Reset
    }

    [DisallowMultipleComponent]
    public sealed class SensoryBagCalibrationHandle : MonoBehaviour
    {
        SensoryBagCalibrator m_Calibrator;
        CalibrationHandleKind m_HandleKind;
        CalibrationAction m_Action;
        int m_ActivePointerId = -1;

        public void Configure(
            SensoryBagCalibrator calibrator,
            CalibrationHandleKind handleKind,
            CalibrationAction action = CalibrationAction.None)
        {
            m_Calibrator = calibrator;
            m_HandleKind = handleKind;
            m_Action = action;
        }

        public void PointerStarted(int pointerId, Vector3 worldPosition)
        {
            if (m_Calibrator == null)
                return;

            if (m_Action == CalibrationAction.Confirm)
            {
                m_Calibrator.ConfirmCalibration();
                return;
            }
            if (m_Action == CalibrationAction.Reset)
            {
                m_Calibrator.ResetCalibration();
                return;
            }

            m_ActivePointerId = pointerId;
            m_Calibrator.BeginPointerDrag(pointerId, m_HandleKind, worldPosition);
        }

        public void PointerMoved(int pointerId, Vector3 worldPosition)
        {
            if (pointerId == m_ActivePointerId)
                m_Calibrator?.MovePointerDrag(pointerId, worldPosition);
        }

        public void PointerEnded(int pointerId)
        {
            if (pointerId != m_ActivePointerId)
                return;
            m_Calibrator?.EndPointerDrag(pointerId);
            m_ActivePointerId = -1;
        }
    }
}
