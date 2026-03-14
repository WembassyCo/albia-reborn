using UnityEngine;
using System.Collections.Generic;

namespace AlbiaReborn.Core.Interfaces
{
    /// <summary>
    /// Interface for climate system queries.
    /// Defines the contract between Climate pod and dependent pods.
    /// </summary>
    public interface IClimateQuery
    {
        float GetTemperatureAt(Vector3 worldPosition);
        float GetHumidityAt(Vector3 worldPosition);
        float GetWindSpeedAt(Vector3 worldPosition);
        Vector3 GetWindDirectionAt(Vector3 worldPosition);
        float GetSunIntensityAt(Vector3 worldPosition);
        bool IsRainingAt(Vector3 worldPosition);
        float GetPrecipitationLevel(Vector3 worldPosition);
        float GetSeasonalFactor();
        void GetClimateZones(List<IClimateZone> zones);
    }

    /// <summary>
    /// Represents a defined climate zone within the world.
    /// </summary>
    public interface IClimateZone
    {
        string ZoneName { get; }
        float BaseTemperature { get; }
        float BaseHumidity { get; }
        Bounds ZoneBounds { get; }
    }
}