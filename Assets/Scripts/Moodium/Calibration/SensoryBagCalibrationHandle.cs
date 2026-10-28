using UnityEngine;

namespace Moodium.Calibration
{
    public enum CalibrationAction
    {
        None,
        Confirm,
        Reset,
        RaiseTable,
        LowerTable
    }

    [DisallowMultipleComponent]
    public sealed class SensoryBagCalibrationHandle : MonoBehaviour
    {
        SensoryBagCalibrator m_Calibrator;
        CalibrationHandleKind m_HandleKind;
        CalibrationAction m_Action;
        int m_ActivePointerId = -1;
        public bool CanInteract=>isActiveAndEnabled&&m_Calibrator!=null&&m_Calibrator.FrameVisible&&!m_Calibrator.IsConfirmed;
        public bool IsDragHandle=>m_Action==CalibrationAction.None;
        public bool IsHeightHandle=>m_HandleKind==CalibrationHandleKind.Height&&IsDragHandle;
        public Vector3 FrameNormal=>m_Calibrator!=null&&m_Calibrator.FrameRoot!=null?m_Calibrator.FrameRoot.forward:transform.forward;

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
            if (!CanInteract||m_ActivePointerId!=-1)
                return;
            m_Calibrator.NotifyUserInteraction();

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
            if(m_Action==CalibrationAction.RaiseTable||m_Action==CalibrationAction.LowerTable)
            {
                m_Calibrator.AdjustPlaneHeight(m_Action==CalibrationAction.RaiseTable ? .005f : -.005f);return;
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
        void OnDisable(){if(m_ActivePointerId!=-1)PointerEnded(m_ActivePointerId);}
    }
}
