﻿using Harmony;
using KSP.UI.Screens;
using LmpClient.Base;
using LmpClient.Events;
using System.Reflection;
using UnityEngine;

namespace LmpClient.Systems.KscScene
{
    /// <summary>
    /// This class controls what happens when we are in KSC screen
    /// </summary>
    public class KscSceneSystem : System<KscSceneSystem>
    {
        private static MethodInfo BuildSpaceTrackingVesselList { get; } = typeof(SpaceTracking).GetMethod("buildVesselsList", AccessTools.all);

        private static KscSceneEvents KscSceneEvents { get; } = new KscSceneEvents();

        #region Base overrides

        public override string SystemName { get; } = nameof(KscSceneSystem);
        
        protected override void OnEnabled()
        {
            LockEvent.onLockAcquireUnityThread.Add(KscSceneEvents.OnLockAcquire);
            LockEvent.onLockReleaseUnityThread.Add(KscSceneEvents.OnLockRelease);
            GameEvents.onGameSceneLoadRequested.Add(KscSceneEvents.OnSceneRequested);
            GameEvents.onLevelWasLoadedGUIReady.Add(KscSceneEvents.LevelLoaded);

            SetupRoutine(new RoutineDefinition(0, RoutineExecution.FixedUpdate, IncreaseTimeWhileInEditor));
        }

        protected override void OnDisabled()
        {
            LockEvent.onLockAcquireUnityThread.Remove(KscSceneEvents.OnLockAcquire);
            LockEvent.onLockReleaseUnityThread.Remove(KscSceneEvents.OnLockRelease);
            GameEvents.onGameSceneLoadRequested.Remove(KscSceneEvents.OnSceneRequested);
            GameEvents.onLevelWasLoadedGUIReady.Remove(KscSceneEvents.LevelLoaded);
        }

        #endregion

        #region Routines
        
        /// <summary>
        /// While in editor the time doesn't advance so here we make it advance
        /// </summary>
        private static void IncreaseTimeWhileInEditor()
        {
            if (!HighLogic.LoadedSceneHasPlanetarium)
            {
                Planetarium.fetch.time += Time.fixedDeltaTime;
                HighLogic.CurrentGame.flightState.universalTime = Planetarium.fetch.time;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Refreshes the vessels displayed in the tracking station panel
        /// </summary>
        public void RefreshTrackingStationVessels()
        {
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                var spaceTracking = Object.FindObjectOfType<SpaceTracking>();
                if (spaceTracking != null)
                    BuildSpaceTrackingVesselList?.Invoke(spaceTracking, null);
            }
        }

        #endregion
    }
}
