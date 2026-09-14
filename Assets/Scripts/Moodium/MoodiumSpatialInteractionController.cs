using System.Collections.Generic;
using Unity.PolySpatial.InputDevices;
using Moodium.Interaction;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Moodium.Flow.ObjectPicker;

namespace Moodium.Flow
{
    public sealed class MoodiumSpatialInteractionController : MonoBehaviour
    {
        readonly Dictionary<int, MoodiumManipulable> m_Selections = new();
        readonly Dictionary<int, MoodiumObjectInteraction> m_PlayInteractions = new();
        readonly Dictionary<int, WorldCarouselController> m_CarouselInteractions = new();
        readonly Dictionary<int, ChocolateCapsuleInteraction> m_ChocolateInteractions = new();
        readonly Dictionary<int, MoodiumWindowHandle> m_WindowInteractions = new();
        readonly HashSet<MoodiumSpatialButton> m_HoveredButtons = new();
        readonly HashSet<MoodiumSpatialButton> m_ButtonsThisFrame = new();
        readonly HashSet<MoodiumObjectPickerItem> m_HoveredPickerItems = new();
        readonly HashSet<MoodiumObjectPickerItem> m_PickerItemsThisFrame = new();

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Update()
        {
            m_ButtonsThisFrame.Clear();
            m_PickerItemsThisFrame.Clear();
            foreach (var touch in Touch.activeTouches)
            {
                var pointer = EnhancedSpatialPointerSupport.GetPointerState(touch);
                var id = pointer.interactionId;
                var hoverButton = pointer.targetObject != null
                    ? pointer.targetObject.GetComponentInParent<MoodiumSpatialButton>()
                    : null;
                if (hoverButton != null)
                    m_ButtonsThisFrame.Add(hoverButton);
                var hoverPickerItem = pointer.targetObject != null
                    ? pointer.targetObject.GetComponentInParent<MoodiumObjectPickerItem>()
                    : null;
                if (hoverPickerItem != null)
                    m_PickerItemsThisFrame.Add(hoverPickerItem);

                if (pointer.phase == SpatialPointerPhase.Began)
                {
                    var target = pointer.targetObject;
                    if (target != null)
                    {
                        var pickerItem = target.GetComponentInParent<MoodiumObjectPickerItem>();
                        if (pickerItem != null)
                        {
                            pickerItem.Press();
                            continue;
                        }

                        var windowHandle = target.GetComponentInParent<MoodiumWindowHandle>();
                        if (windowHandle != null)
                        {
                            m_WindowInteractions[id] = windowHandle;
                            windowHandle.PointerStarted(id, pointer.interactionPosition);
                            continue;
                        }

                        var chocolate = target.GetComponentInParent<ChocolateCapsuleInteraction>();
                        if (chocolate != null && chocolate.InteractionEnabled && chocolate.SpatialPointerEnabled)
                        {
                            m_ChocolateInteractions[id] = chocolate;
                            chocolate.PointerStarted(id, pointer.interactionPosition);
                            continue;
                        }

                        var worldItem = target.GetComponentInParent<WorldCarouselItem>();
                        var carousel = worldItem != null
                            ? worldItem.GetComponentInParent<WorldCarouselController>()
                            : null;
                        if (carousel != null)
                        {
                            m_CarouselInteractions[id] = carousel;
                            carousel.PointerStarted(id, worldItem, pointer.interactionPosition);
                            continue;
                        }

                        var button = target.GetComponentInParent<MoodiumSpatialButton>();
                        if (button != null)
                        {
                            button.Press();
                            continue;
                        }

                        var manipulable = target.GetComponentInParent<MoodiumManipulable>();
                        if (manipulable != null && manipulable.CanManipulate)
                        {
                            m_Selections[id] = manipulable;
                            manipulable.BeginPointer(
                                id,
                                pointer.interactionPosition,
                                pointer.inputDeviceRotation);
                        }
                        else
                        {
                            var interaction = target.GetComponentInParent<MoodiumObjectInteraction>();
                            if (interaction != null && interaction.InteractionEnabled)
                            {
                                m_PlayInteractions[id] = interaction;
                                interaction.PointerStarted(id, pointer.interactionPosition);
                            }
                        }
                    }
                }

                if (m_ChocolateInteractions.TryGetValue(id, out var chocolateInteraction))
                {
                    if (pointer.phase == SpatialPointerPhase.Ended ||
                        pointer.phase == SpatialPointerPhase.Cancelled ||
                        pointer.phase == SpatialPointerPhase.None)
                    {
                        chocolateInteraction.PointerEnded(id);
                        m_ChocolateInteractions.Remove(id);
                    }
                    continue;
                }

                if (m_WindowInteractions.TryGetValue(id, out var windowInteraction))
                {
                    switch (pointer.phase)
                    {
                        case SpatialPointerPhase.Moved:
                            windowInteraction.PointerMoved(id, pointer.interactionPosition);
                            break;
                        case SpatialPointerPhase.Ended:
                        case SpatialPointerPhase.Cancelled:
                        case SpatialPointerPhase.None:
                            windowInteraction.PointerEnded(id);
                            m_WindowInteractions.Remove(id);
                            break;
                    }
                    continue;
                }

                if (m_CarouselInteractions.TryGetValue(id, out var carouselInteraction))
                {
                    switch (pointer.phase)
                    {
                        case SpatialPointerPhase.Moved:
                            carouselInteraction.PointerMoved(id, pointer.interactionPosition);
                            break;
                        case SpatialPointerPhase.Ended:
                        case SpatialPointerPhase.Cancelled:
                        case SpatialPointerPhase.None:
                            carouselInteraction.PointerEnded(id);
                            m_CarouselInteractions.Remove(id);
                            break;
                    }
                    continue;
                }

                if (m_PlayInteractions.TryGetValue(id, out var playInteraction) &&
                    pointer.phase == SpatialPointerPhase.Moved)
                {
                    playInteraction.PointerMoved(id, pointer.interactionPosition);
                }
                else if (m_PlayInteractions.TryGetValue(id, out playInteraction) &&
                         (pointer.phase == SpatialPointerPhase.Ended ||
                          pointer.phase == SpatialPointerPhase.Cancelled ||
                          pointer.phase == SpatialPointerPhase.None))
                {
                    playInteraction.PointerEnded(id);
                    m_PlayInteractions.Remove(id);
                }

                if (!m_Selections.TryGetValue(id, out var selection))
                    continue;

                switch (pointer.phase)
                {
                    case SpatialPointerPhase.Moved:
                        selection.UpdatePointer(
                            id,
                            pointer.interactionPosition,
                            pointer.inputDeviceRotation);
                        break;
                    case SpatialPointerPhase.Ended:
                    case SpatialPointerPhase.Cancelled:
                    case SpatialPointerPhase.None:
                        selection.EndPointer(id);
                        m_Selections.Remove(id);
                        break;
                }
            }

            foreach (var button in m_HoveredButtons)
                if (button != null && !m_ButtonsThisFrame.Contains(button))
                    button.SetHovered(false);
            foreach (var button in m_ButtonsThisFrame)
                if (button != null)
                    button.SetHovered(true);
            m_HoveredButtons.Clear();
            foreach (var button in m_ButtonsThisFrame)
                m_HoveredButtons.Add(button);
            foreach (var item in m_HoveredPickerItems)
                if (item != null && !m_PickerItemsThisFrame.Contains(item))
                    item.SetHovered(false);
            foreach (var item in m_PickerItemsThisFrame)
                if (item != null)
                    item.SetHovered(true);
            m_HoveredPickerItems.Clear();
            foreach (var item in m_PickerItemsThisFrame)
                m_HoveredPickerItems.Add(item);
        }
    }
}
