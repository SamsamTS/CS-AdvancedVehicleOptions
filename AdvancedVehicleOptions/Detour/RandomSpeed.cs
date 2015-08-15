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
        private static float[] _factors;
        private static float[] _lastFactors;
        private static uint[] _lastLaneIDs;
        private static NetInfo[] _highways;

        private static VehicleManager _vehicleManager;
        private static PathManager _pathManager;
        private static NetManager _netManager;

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
            if (_factors == null)
            {
                // Precalculate factors for better performances
                _factors = new float[ushort.MaxValue + 1];
                for (int i = 0; i < _factors.Length; i++)
                {
                    _factors[i] = Randomize(1f, 10f, i);
                }

                _lastFactors = new float[ushort.MaxValue + 1];
                _lastLaneIDs = new uint[ushort.MaxValue + 1];
            }

            _vehicleManager = VehicleManager.instance;
            _pathManager = PathManager.instance;
            _netManager = NetManager.instance;

            List<NetInfo> highways = new List<NetInfo>();

            for (uint i = 0; i < PrefabCollection<NetInfo>.PrefabCount(); i++)
            {
                NetInfo info = PrefabCollection<NetInfo>.GetPrefab(i);
                if (info != null && info.name.ToLower().Contains("highway"))
                {
                    int count = 0;
                    for (int j = 0; j < info.m_lanes.Length; j++)
                    {
                        if (info.m_lanes[j] != null && info.m_lanes[j].m_laneType == NetInfo.LaneType.Vehicle) count++;
                    }

                    if (count == 3) highways.Add(info);
                }
            }

            _highways = highways.ToArray();

            if (_enabled && !_detoured)
            {
                if (_CalculateTargetSpeed == null)
                {
                    _CalculateTargetSpeed = new Dictionary<Type, Redirection>();
                    MethodInfo CalculateTargetSpeed_detour = typeof(VehicleAIDetour).GetMethod("CalculateTargetSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

                    for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
                    {
                        VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                        if (prefab == null) continue;
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
                        // Traffic++ support
                        Type csl_traffic = Type.GetType("CSL_Traffic.CustomVehicleAI, CSL-Traffic");
                        if (csl_traffic != null)
                        {
                            DebugUtils.Log("Traffic++ found. Adding support");
                            _RestrictSpeed = new Redirection();
                            _RestrictSpeed.original = csl_traffic.GetMethod("RestrictSpeed", BindingFlags.Public | BindingFlags.Static);
                            _RestrictSpeed.detour = typeof(RandomSpeed).GetMethod("RestrictSpeed", BindingFlags.NonPublic | BindingFlags.Static);
                        }
                    }
                    catch { }
                }

                foreach (Redirection redirect in _CalculateTargetSpeed.Values)
                    redirect.Redirect();

                if (_RestrictSpeed != null) _RestrictSpeed.Redirect();

                _detoured = true;
            }
            else if (!_enabled && _detoured)
            {
                foreach (Redirection redirect in _CalculateTargetSpeed.Values)
                    redirect.Revert();

                if (_RestrictSpeed != null) _RestrictSpeed.Revert();

                _detoured = false;
            }
        }

        public static void Restore()
        {
            if (_CalculateTargetSpeed != null && _detoured)
            {
                foreach (Redirection redirect in _CalculateTargetSpeed.Values)
                    redirect.Revert();

                if (_RestrictSpeed != null) _RestrictSpeed.Revert();

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
                if (!highwaySpeed)
                {
                    _lastFactor = _factors[vehicleID];
                    return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed) * _factors[vehicleID];
                }

                // Highway Speed :
                uint laneID = PathManager.GetLaneID(_pathManager.m_pathUnits.m_buffer[_vehicleManager.m_vehicles.m_buffer[(int)vehicleID].m_path].GetPosition(_vehicleManager.m_vehicles.m_buffer[(int)vehicleID].m_pathPositionIndex >> 1));

                if (laneID != 0)
                {
                    if (_lastLaneIDs[vehicleID] == laneID)
                    {
                        // Still on same lane
                        _lastFactor = _lastFactors[vehicleID];
                        return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed) * _lastFactor;
                    }
                    _lastLaneIDs[vehicleID] = laneID;

                    NetInfo info = PrefabCollection<NetInfo>.GetPrefab(_netManager.m_segments.m_buffer[_netManager.m_lanes.m_buffer[laneID].m_segment].m_infoIndex);

                    for (int i = 0; i < _highways.Length; i++)
                    {
                        if (_highways[i] == info)
                        {
                            // On highway
                            uint currentLane = _netManager.m_segments.m_buffer[_netManager.m_lanes.m_buffer[laneID].m_segment].m_lanes;
                            int lanePos = 0;

                            while (currentLane != 0u && currentLane != laneID)
                            {
                                lanePos++;
                                currentLane = _netManager.m_lanes.m_buffer[(int)currentLane].m_nextLane;
                            }

                            lanePos = 2 - lanePos;

                            _lastFactor = _lastFactors[vehicleID] = _factors[vehicleID] + lanePos / 8f - 0.10f;
                            return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed) * _lastFactor;
                        }
                    }
                }

                // Not on highway
                _lastFactor = _lastFactors[vehicleID] = _factors[vehicleID];
                return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed) * _lastFactor;
            }
        }

        // Traffic++ detour method
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
    }
}
