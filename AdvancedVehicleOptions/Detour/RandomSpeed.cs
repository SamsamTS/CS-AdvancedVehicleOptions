using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

namespace AdvancedVehicleOptions.Detour
{
    public static class RandomSpeed
    {
        private static bool _enabled = false;
        private static bool _detoured = false;

        private static float _lastFactor;
        private static Dictionary<ushort, ushort> _previousVehicleSegment = new Dictionary<ushort,ushort>();

        private static Dictionary<Type, Redirection> _CalculateTargetSpeed;
        private static Redirection _RestrictSpeed;

        private class Redirection
        {
            public RedirectCallsState state;
            public MethodInfo original;
            public MethodInfo detour;

            public void Redirect()
            {
                state = RedirectionHelper.RedirectCalls(original, detour);
            }

            public void Revert()
            {
                RedirectionHelper.RevertRedirect(original, state);
            }
        }

        public static bool activated = false;
        public static List<NetInfo> highways = new List<NetInfo>();

        public static bool enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if(activated) Intitialize();
                }
            }
        }

        public static bool highwaySpeed = true;

        public static void Intitialize()
        {
            highways.Clear();

            for (uint i = 0; i < PrefabCollection<NetInfo>.PrefabCount(); i++)
            {
                NetInfo info = PrefabCollection<NetInfo>.GetPrefab(i);
                if (info != null && info.name.ToLower().Contains("highway"))
                {
                    int count = 0;
                    for (int j = 0; j < info.m_lanes.Length; j++ )
                    {
                        if(info.m_lanes[j] != null && info.m_lanes[j].m_laneType == NetInfo.LaneType.Vehicle) count++;
                    }

                    if (count == 3) highways.Add(info);
                }
            }

            if (_enabled && !_detoured)
            {
                if (_CalculateTargetSpeed == null)
                {
                    _CalculateTargetSpeed = new Dictionary<Type, Redirection>();
                    MethodInfo CalculateTargetSpeed_detour = typeof(VehicleAIDetour).GetMethod("CalculateTargetSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

                    for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
                    {
                        VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                        Type aiType = prefab.m_vehicleAI.GetType();

                        if (prefab.m_vehicleAI is VehicleAI && !_CalculateTargetSpeed.ContainsKey(aiType))
                        {
                            Redirection redirect = new Redirection();
                            redirect.original = aiType.GetMethod("CalculateTargetSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
                            redirect.detour = CalculateTargetSpeed_detour;
                            _CalculateTargetSpeed.Add(aiType, redirect);
                        }
                    }

                    try
                    {
                        Type csl_traffic = Type.GetType("CSL_Traffic.CustomVehicleAI, CSL-Traffic");
                        if (csl_traffic != null)
                        {
                            Debug.Log("CSL_Traffic.CustomVehicleAI found");
                            _RestrictSpeed = new Redirection();
                            _RestrictSpeed.original = csl_traffic.GetMethod("RestrictSpeed", BindingFlags.Public | BindingFlags.Static);
                            _RestrictSpeed.detour = typeof(RandomSpeed).GetMethod("RestrictSpeed", BindingFlags.NonPublic | BindingFlags.Static);
                        }
                    }
                    catch { }
                }

                foreach (Redirection redirct in _CalculateTargetSpeed.Values)
                    redirct.Redirect();

                if (_RestrictSpeed != null) _RestrictSpeed.Redirect();

                _detoured = true;
            }
            else if (!_enabled && _detoured)
            {
                foreach (Redirection redirct in _CalculateTargetSpeed.Values)
                    redirct.Revert();

                if (_RestrictSpeed != null) _RestrictSpeed.Revert();

                _detoured = false;
            }
        }

        public static void Restore()
        {
            if (_CalculateTargetSpeed != null && _detoured)
            {
                foreach (Redirection redirct in _CalculateTargetSpeed.Values)
                    redirct.Revert();

                if (_RestrictSpeed != null) _RestrictSpeed.Revert();
                //_SimulationStep.Revert();

                _detoured = false;

            }

            _CalculateTargetSpeed = null;
            _RestrictSpeed = null;
            activated = false;
        }

        internal class VehicleAIDetour : VehicleAI
        {
            protected new virtual float CalculateTargetSpeed(ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
            {
                float a = 1000f / (1f + curve * 1000f / this.m_info.m_turning) + 2f;
                float b = 8f * speedLimit;

                // Original code :
                // return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed);

                // New code :
                _lastFactor = GetFactor(vehicleID);
                return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed) * _lastFactor;
            }
        }

        private static float RestrictSpeed(float calculatedSpeed, uint laneId, VehicleInfo info)
        {
            _RestrictSpeed.Revert();
            float speed = (float) _RestrictSpeed.original.Invoke(null, new object[]{calculatedSpeed, laneId, info});
            _RestrictSpeed.Redirect();

            return speed * _lastFactor;
        }

        private static float Randomize(float value, float percent, int seed)
        {
            float step = value * percent / 100;

            UnityEngine.Random.seed = seed;
            return UnityEngine.Random.Range(value - step, value + step);
        }

        private static float GetFactor(ushort vehicleID)
        {
            if (!highwaySpeed) return Randomize(1f, 10f, vehicleID);

            Vehicle vehicle = VehicleManager.instance.m_vehicles.m_buffer[(int)vehicleID];
            PathUnit path = PathManager.instance.m_pathUnits.m_buffer[vehicle.m_path];

            uint laneID = PathManager.GetLaneID(path.GetPosition(vehicle.m_pathPositionIndex >> 1));

            ushort segmentID = NetManager.instance.m_lanes.m_buffer[laneID].m_segment;
            NetSegment segment = NetManager.instance.m_segments.m_buffer[segmentID];

            if (!highways.Contains(segment.Info)) return Randomize(1f, 10f, vehicleID);

            uint currentLane = segment.m_lanes;

            int lanePos = 0;

            while (currentLane != 0u && currentLane != laneID)
            {
                lanePos++;
                currentLane = NetManager.instance.m_lanes.m_buffer[(int)currentLane].m_nextLane;
            }

            lanePos = 2 - lanePos;

            return Randomize(1f, 10f, vehicleID) + lanePos / 8f - 0.10f;
        }
    }
}
