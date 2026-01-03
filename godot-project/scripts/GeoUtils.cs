using Godot;

public static class GeoUtils
{
    public const float EarthRadius = 50f;
    
    public static Vector3 LatLonToVector3(float lat, float lon, float altFt, float rotationOffset, float altitudeExaggeration = 1.0f)
    {
        float phi = Mathf.DegToRad(lat);
        float theta = Mathf.DegToRad(-lon + rotationOffset);

        float altMeters = altFt * 0.3048f;
        float altScaled = altMeters * 0.0001f * altitudeExaggeration;

        float radius = EarthRadius + altScaled;

        return new Vector3(
            radius * Mathf.Cos(phi) * Mathf.Cos(theta),
            radius * Mathf.Sin(phi),
            radius * Mathf.Cos(phi) * Mathf.Sin(theta)
        );
    }
}