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
        private static ushort _lastVehicleID;

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

        private static Dictionary<Type, Redirection> _CalculateTargetSpeed;
        private static Redirection _RestrictSpeed;
        //private static Redirection _SimulationStep;

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

        public static void Intitialize()
        {
            if (_enabled && !_detoured)
            {
                if (_CalculateTargetSpeed == null)
                {
                    _CalculateTargetSpeed = new Dictionary<Type, Redirection>();
                    MethodInfo CalculateTargetSpeed_detour = typeof(VehicleAIDetour).GetMethod("CalculateTargetSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

                    for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++ )
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

                    System.Type[] types = new System.Type[]
                    {
                        typeof(ushort),
                        typeof(Vehicle).MakeByRefType(),
                        typeof(Vehicle.Frame).MakeByRefType(),
                        typeof(ushort),
                        typeof(Vehicle).MakeByRefType(),
                        typeof(int)
                    };

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
                    /*_SimulationStep = new Redirection();
                    _SimulationStep.original = typeof(CarAI).GetMethod("SimulationStep", types);
                    _SimulationStep.detour = typeof(CarAIDetour).GetMethod("SimulationStep", types);*/
                }

                foreach (Redirection redirct in _CalculateTargetSpeed.Values)
                    redirct.Redirect();

                if (_RestrictSpeed != null) _RestrictSpeed.Redirect();
                //_SimulationStep.Redirect();

                _detoured = true;
            }
            else if (!_enabled && _detoured)
            {
                foreach (Redirection redirct in _CalculateTargetSpeed.Values)
                    redirct.Revert();

                if (_RestrictSpeed != null) _RestrictSpeed.Revert();
                //_SimulationStep.Revert();

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

        public static int count = 0;

        public static float Randomize(float value, int seed)
        {
            float step = value / 10f;

            UnityEngine.Random.seed = seed;
            return UnityEngine.Random.Range(value - step, value + step);
        }


        internal class VehicleAIDetour : VehicleAI
        {
            protected new virtual float CalculateTargetSpeed(ushort vehicleID, ref Vehicle data, float speedLimit, float curve)
            {
                float a = 1000f / (1f + curve * 1000f / this.m_info.m_turning) + 2f;
                float b = 8f * speedLimit;
                //return Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed);
                _lastVehicleID = vehicleID;
                return Randomize(Mathf.Min(Mathf.Min(a, b), this.m_info.m_maxSpeed), vehicleID);
            }
        }

        private static float RestrictSpeed(float calculatedSpeed, uint laneId, VehicleInfo info)
        {
            _RestrictSpeed.Revert();
            float speed = (float) _RestrictSpeed.original.Invoke(null, new object[]{calculatedSpeed, laneId, info});
            _RestrictSpeed.Redirect();

            return Randomize(speed, _lastVehicleID);
        }

        /*internal class CarAIDetour : CarAI
        {
            public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics)
            {
                float speed = this.m_info.m_maxSpeed;
                float acceleration = this.m_info.m_acceleration;
                float braking = this.m_info.m_braking;

                this.m_info.m_maxSpeed = Randomize(speed, vehicleID);
                this.m_info.m_acceleration = Randomize(acceleration, vehicleID);
                this.m_info.m_braking = Randomize(braking, vehicleID);

                _SimulationStep.Revert();
                base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
                _SimulationStep.Redirect();

                this.m_info.m_acceleration = acceleration;
                this.m_info.m_braking = braking;
                this.m_info.m_maxSpeed = speed;
            }
        }*/
    }
}
