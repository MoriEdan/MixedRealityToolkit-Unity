﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoloToolkit.Unity
{
    public class UAudioManagerBaseEditor<TEvent> : Editor where TEvent : AudioEvent, new()
    {
        protected UAudioManagerBase<TEvent> myTarget;
        private string[] eventNames;
        private int selectedEventIndex = 0;
        private readonly string[] posTypes = { "2D", "3D", "Spatial Sound" };
        private Rect editorCurveSize = new Rect(0f, 0f, 1f, 1f);

        protected void SetUpEditor()
        {
            // Having a null array of events causes too many errors and should only happen on first adding anyway.
            if (this.myTarget.EditorEvents == null)
            {
                this.myTarget.EditorEvents = new TEvent[0];
            }
            this.eventNames = new string[this.myTarget.EditorEvents.Length];
            UpdateEventNames(this.myTarget.EditorEvents);
        }

        protected void DrawInspectorGUI(bool showEmitters)
        {
            this.serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawEventHeader(this.myTarget.EditorEvents);

            if (this.myTarget.EditorEvents != null && this.myTarget.EditorEvents.Length > 0)
            {
                // Display current event in dropdown.
                EditorGUI.indentLevel++;
                this.selectedEventIndex = EditorGUILayout.Popup(this.selectedEventIndex, this.eventNames);

                if (this.selectedEventIndex < this.myTarget.EditorEvents.Length)
                {
                    TEvent selectedEvent;

                    selectedEvent = this.myTarget.EditorEvents[this.selectedEventIndex];
                    SerializedProperty selectedEventProperty = this.serializedObject.FindProperty("events.Array.data[" + this.selectedEventIndex.ToString() + "]");
                    EditorGUILayout.Space();

                    if (selectedEventProperty != null)
                    {
                        DrawEventInspector(selectedEventProperty, selectedEvent, this.myTarget.EditorEvents, showEmitters);
                        if (!DrawContainerInspector(selectedEventProperty, selectedEvent))
                        {
                            EditorGUI.indentLevel++;
                            DrawSoundClipInspector(selectedEventProperty, selectedEvent);
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.EndChangeCheck();
            this.serializedObject.ApplyModifiedProperties();

            if (UnityEngine.GUI.changed)
            {
                EditorUtility.SetDirty(this.myTarget);
            }
        }

        private void DrawEventHeader(TEvent[] EditorEvents)
        {
            // Add or remove current event.
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayoutExtensions.Label("Events");

            using (new EditorGUI.DisabledScope((EditorEvents != null) && (EditorEvents.Length < 1)))
            {
                if (EditorGUILayoutExtensions.Button("Remove"))
                {
                    this.myTarget.EditorEvents = RemoveAudioEvent(EditorEvents, this.selectedEventIndex);
                }
            }

            if (EditorGUILayoutExtensions.Button("Add"))
            {
                this.myTarget.EditorEvents = AddAudioEvent(EditorEvents);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawEventInspector(SerializedProperty selectedEventProperty, TEvent selectedEvent, TEvent[] EditorEvents, bool showEmitters)
        {
            // Get current event's properties.
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Name"));

            if (selectedEvent.Name != this.eventNames[this.selectedEventIndex])
            {
                UpdateEventNames(EditorEvents);
            }

            if (showEmitters)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("PrimarySource"));
                if (selectedEvent.IsContinuous())
                {
                    EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("SecondarySource"));
                }
            }

            // Positioning
            selectedEvent.Spatialization = (SpatialPositioningType)EditorGUILayout.Popup("Positioning", (int)selectedEvent.Spatialization, this.posTypes);

            if (selectedEvent.Spatialization == SpatialPositioningType.SpatialSound)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("RoomSize"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("MinGain"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("MaxGain"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("UnityGainDistance"));
                EditorGUILayout.Space();
            }
            else if (selectedEvent.Spatialization == SpatialPositioningType.ThreeD)
            {
                //Quick this : needs an update or the serialized object is not saving the threeD value
                this.serializedObject.Update();

                float curveHeight = 30f;
                float curveWidth = 300f;

                //Simple 3D Sounds properties
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("MaxDistanceAttenuation3D"));

                //volume attenuation
                selectedEventProperty.FindPropertyRelative("AttenuationCurve").animationCurveValue = EditorGUILayout.CurveField("Attenuation", selectedEventProperty.FindPropertyRelative("attenuationCurve").animationCurveValue, Color.red, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //Spatial green
                selectedEventProperty.FindPropertyRelative("SpatialCurve").animationCurveValue = EditorGUILayout.CurveField("Spatial", selectedEventProperty.FindPropertyRelative("spatialCurve").animationCurveValue, Color.green, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //spread lightblue
                selectedEventProperty.FindPropertyRelative("SpreadCurve").animationCurveValue = EditorGUILayout.CurveField("Spread", selectedEventProperty.FindPropertyRelative("spreadCurve").animationCurveValue, Color.blue, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //lowpass purple
                selectedEventProperty.FindPropertyRelative("LowPassCurve").animationCurveValue = EditorGUILayout.CurveField("LowPass", selectedEventProperty.FindPropertyRelative("lowPassCurve").animationCurveValue, Color.magenta, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));
                //Yellow reverb
                selectedEventProperty.FindPropertyRelative("ReverbCurve").animationCurveValue = EditorGUILayout.CurveField("Reverb", selectedEventProperty.FindPropertyRelative("reverbCurve").animationCurveValue, Color.yellow, editorCurveSize, GUILayout.Height(curveHeight), GUILayout.Width(curveWidth), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(true));

                EditorGUILayout.Space();
            } 

            // AudioBus
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("AudioBus"));

            // Fades
            if (!selectedEvent.IsContinuous())
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("FadeInTime"));
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("FadeOutTime"));
            }

            // Pitch Settings
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("PitchCenter"));

            // Volume settings
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("VolumeCenter"));

            // Pan Settings
            if (selectedEvent.Spatialization == SpatialPositioningType.TwoD)
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("PanCenter"));
            }
            // Instancing
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("InstanceLimit"));
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("InstanceTimeBuffer"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("AudioEventInstanceBehavior"));

            // Container
            EditorGUILayout.Space();
        }

        private bool DrawContainerInspector(SerializedProperty selectedEventProperty, TEvent selectedEvent)
        {
            bool addedSound = false;
            EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.containerType"));

            if (!selectedEvent.IsContinuous())
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.looping"));

                if (selectedEvent.Container.looping)
                {
                    EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.loopTime"));
                }
            }

            // Sounds
            EditorGUILayout.Space();

            if (selectedEvent.IsContinuous())
            {
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.crossfadeTime"));
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sounds");

            if (EditorGUILayoutExtensions.Button("Add"))
            {
                AddSound(selectedEvent);

                // Skip drawing sound inspector after adding a new sound.
                addedSound = true;
            }
            EditorGUILayout.EndHorizontal();
            return addedSound;
        }

        private void DrawSoundClipInspector(SerializedProperty selectedEventProperty, TEvent selectedEvent)
        {
            bool allowLoopingClip = !selectedEvent.Container.looping;

            if (allowLoopingClip)
            {
                if (selectedEvent.IsContinuous())
                {
                    allowLoopingClip = false;
                }
            }

            for (int i = 0; i < selectedEvent.Container.sounds.Length; i++)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.sounds.Array.data[" + i + "].sound"));

                if (EditorGUILayoutExtensions.Button("Remove"))
                {
                    selectedEventProperty.FindPropertyRelative("Container.sounds.Array.data[" + i + "]").DeleteCommand();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (!selectedEvent.IsContinuous())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.sounds.Array.data[" + i + "].delayCenter"));
                    EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.sounds.Array.data[" + i + "].delayRandomization"));
                    EditorGUILayout.EndHorizontal();

                    //Disable looping next clips in a simultaneous container only.
                    if (allowLoopingClip)
                    {
                        EditorGUILayout.PropertyField(selectedEventProperty.FindPropertyRelative("Container.sounds.Array.data[" + i + "].looping"));

                        if (selectedEvent.Container.sounds[i].looping && selectedEvent.Container.containerType == AudioContainerType.Simultaneous)
                        {
                            allowLoopingClip = false;
                        }
                    }
                    else
                    {
                        selectedEvent.Container.sounds[i].looping = false;
                    }
                }
            }
        }

        private void UpdateEventNames(TEvent[] EditorEvents)
        {
            HashSet<string> previousEventNames = new HashSet<string>();

            for (int i = 0; i < EditorEvents.Length; i++)
            {
                if (string.IsNullOrEmpty(EditorEvents[i].Name))
                {
                    EditorEvents[i].Name = "_NewEvent" + i.ToString();
                }

                while (previousEventNames.Contains(EditorEvents[i].Name))
                {
                    EditorEvents[i].Name = "_" + EditorEvents[i].Name;
                }

                this.eventNames[i] = EditorEvents[i].Name;
                previousEventNames.Add(this.eventNames[i]);
            }
        }

        private void AddSound(TEvent selectedEvent)
        {

            UAudioClip[] tempClips = new UAudioClip[selectedEvent.Container.sounds.Length + 1];
            selectedEvent.Container.sounds.CopyTo(tempClips, 0);
            tempClips[tempClips.Length - 1] = new UAudioClip();
            selectedEvent.Container.sounds = tempClips;
        }

        private TEvent[] AddAudioEvent(TEvent[] EditorEvents)
        {
            TEvent tempEvent = new TEvent();
            TEvent[] tempEventArray = new TEvent[EditorEvents.Length + 1];
            tempEvent.Container = new AudioContainer();
            tempEvent.Container.sounds = new UAudioClip[0];
            EditorEvents.CopyTo(tempEventArray, 0);
            tempEventArray[EditorEvents.Length] = tempEvent;
            this.eventNames = new string[tempEventArray.Length];
            UpdateEventNames(tempEventArray);
            this.selectedEventIndex = this.eventNames.Length - 1;
            return tempEventArray;
        }

        private TEvent[] RemoveAudioEvent(TEvent[] editorEvents, int eventToRemove)
        {
            editorEvents = RemoveElement(editorEvents, eventToRemove);
            this.eventNames = new string[editorEvents.Length];
            UpdateEventNames(editorEvents);

            if (this.selectedEventIndex >= editorEvents.Length)
            {
                this.selectedEventIndex--;
            }

            return editorEvents;
        }

        /// <summary>
        /// Returns a new array that has the item at the given index removed.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">Index to remove.</param>
        /// <returns>The new array.</returns>
        public static T[] RemoveElement<T>(T[] array, int index)
        {
            T[] newArray = new T[array.Length - 1];

            for (int i = 0; i < array.Length; i++)
            {
                // Send the clip to the previous item in the new array if we have passed the removed clip.
                if (i > index)
                {
                    newArray[i - 1] = array[i];
                }
                else if (i < index)
                {
                    newArray[i] = array[i];
                }
            }

            return newArray;
        }
    }
}