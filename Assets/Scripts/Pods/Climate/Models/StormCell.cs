using Albia.Climate.Interfaces;

namespace Albia.Climate.Models;

/// <summary>
/// Represents a weather storm system in the world
/// Storms form from temperature and moisture differentials
/// </summary>
public class StormCell
{
    /// <summary>
    /// Unique identifier for this storm
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
    
    /// <summary>
    /// Current center position X
    /// </summary>
    public float CenterX { get; set; }
    
    /// <summary>
    /// Current center position Y
    /// </summary>
    public float CenterY { get; set; }
    
    /// <summary>
    /// Storm radius in grid cells
    /// </summary>
    public float Radius { get; set; }
    
    /// <summary>
    /// Movement velocity X (cells per second)
    /// </summary>
    public float VelocityX { get; set; }
    
    /// <summary>
    /// Movement velocity Y (cells per second)
    /// </summary>
    public float VelocityY { get; set; }
    
    /// <summary>
    /// Storm intensity (0.0 - 1.0)
    /// </summary>
    public float Intensity { get; set; }
    
    /// <summary>
    /// Storm lifetime in seconds
    /// </summary>
    public float Lifetime { get; private set; }
    
    /// <summary>
    /// Maximum lifetime before storm dissipates
    /// </summary>
    public float MaxLifetime { get; set; } = 300f; // 5 minutes default
    
    /// <summary>
    /// Storm type based on severity
    /// </summary>
    public StormType Type { get; set; }
    
    /// <summary>
    /// Temperature differential at formation (Celsius)
    /// </summary>
    public float TemperatureDifferential { get; set; }
    
    /// <summary>
    /// Moisture content driving the storm
    /// </summary>
    public float MoistureContent { get; set; }
    
    /// <summary>
    /// Current energy of the storm
    /// </summary>
    public float Energy { get; set; }
    
    /// <summary>
    /// Rotation in degrees (for visual effects)
    /// </summary>
    public float Rotation { get; set; }
    
    /// <summary>
    /// Whether storm is active/dissipated
    /// </summary>
    public bool IsActive => Lifetime < MaxLifetime && Intensity > 0.1f;
    
    /// <summary>
    /// Creates a new storm cell
    /// </summary>
    public StormCell(float x, float y, float tempDiff, float moisture)
    {
        CenterX = x;
        CenterY = y;
        TemperatureDifferential = tempDiff;
        MoistureContent = moisture;
        
        // Calculate initial intensity from differentials
        Intensity = Math.Min(1.0f, CalculateStormIntensity(tempDiff, moisture));
        Energy = Intensity * 100f;
        
        // Initialize radius based on intensity
        Radius = 2f + (Intensity * 8f); // 2-10 cells
        
        // Random initial rotation
        Rotation = 0f;
        
        // Determine storm type based on intensity
        Type = GetStormType(Intensity);
        
        // Lifetime increases with intensity
        MaxLifetime = 120f + (Intensity * 600f); // 2-12 minutes
        
        // Initialize velocity (storms move with prevailing winds)
        VelocityX = 0f;
        VelocityY = 0f;
    }
    
    /// <summary>
    /// Updates storm position and state
    /// </summary>
    public void Update(float deltaTime, WeatherGrid grid)
    {
        if (!IsActive) return;
        
        Lifetime += deltaTime;
        
        // Move storm based on velocity
        CenterX += VelocityX * deltaTime;
        CenterY += VelocityY * deltaTime;
        
        // Wrap around grid boundaries
        CenterX = ((CenterX % grid.Width) + grid.Width) % grid.Width;
        CenterY = ((CenterY % grid.Height) + grid.Height) % grid.Height;
        
        // Rotate slowly
        Rotation += (Intensity * 10f) * deltaTime;
        
        // Natural dissipation over time
        float dissipation = 0.05f * deltaTime;
        Intensity = Math.Max(0f, Intensity - dissipation);
        Energy = Intensity * 100f;
        
        // Update affected cells
        AffectCells(grid, deltaTime);
    }
    
    /// <summary>
    /// Affects weather cells within storm radius
    /// </summary>
    private void AffectCells(WeatherGrid grid, float deltaTime)
    {
        int minX = (int)Math.Floor(CenterX - Radius);
        int maxX = (int)Math.Ceiling(CenterX + Radius);
        int minY = (int)Math.Floor(CenterY - Radius);
        int maxY = (int)Math.Ceiling(CenterY + Radius);
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Wrap coordinates
                int wrappedX = ((x % grid.Width) + grid.Width) % grid.Width;
                int wrappedY = ((y % grid.Height) + grid.Height) % grid.Height;
                
                // Calculate distance from storm center
                float dx = x - CenterX;
                float dy = y - CenterY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (distance > Radius) continue;
                
                // Influence factor decreases with distance from center
                float influence = 1.0f - (distance / Radius);
                influence *= Intensity;
                
                var cell = grid.GetCell(wrappedX, wrappedY);
                ApplyStormEffect(cell, influence, deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Applies storm effects to a specific cell
    /// </summary>
    private void ApplyStormEffect(WeatherCell cell, float influence, float deltaTime)
    {
        // Storm draws moisture
        cell.Moisture = Math.Max(0f, cell.Moisture - influence * 0.1f * deltaTime);
        
        // Storms cool the area initially, then warm it
        float tempChange = influence * TemperatureDifferential * 0.1f * deltaTime;
        cell.Temperature -= tempChange;
        
        // Increase cloud cover
        cell.CloudCover = Math.Min(1f, cell.CloudCover + influence * 0.3f * deltaTime);
        
        // Generate precipitation based on storm intensity
        float precipChance = influence * Intensity;
        if (precipChance > 0.3f)
        {
            // Snow if below freezing, rain otherwise
            if (cell.Temperature < 0f)
            {
                cell.PrecipitationType = Interfaces.PrecipitationType.Snow;
            }
            else if (cell.Temperature < 2f)
            {
                cell.PrecipitationType = Interfaces.PrecipitationType.Sleet;
            }
            else
            {
                cell.PrecipitationType = Interfaces.PrecipitationType.Rain;
            }
            cell.PrecipitationIntensity = Math.Max(cell.PrecipitationIntensity, precipChance);
        }
        
        // Storm influences wind (circular around center)
        float angle = (float)Math.Atan2(cell.Y - CenterY, cell.X - CenterX);
        float windStrength = influence * 5f;
        cell.WindDirection = (cell.WindDirection * 0.8f) + ((angle * (180f / MathF.PI) + 90f) * 0.2f);
        cell.WindSpeed = Math.Min(30f, cell.WindSpeed + windStrength * deltaTime);
        
        // Storms lower pressure
        cell.Pressure = Math.Max(980f, cell.Pressure - influence * 2f * deltaTime);
    }
    
    /// <summary>
    /// Calculates storm intensity from differentials
    /// </summary>
    private static float CalculateStormIntensity(float tempDiff, float moisture)
    {
        // Storms form from temperature and moisture differentials
        // Higher differentials + more moisture = stronger storms
        float tempFactor = Math.Abs(tempDiff) / 20f; // Normalize (20C diff = max)
        float moistureFactor = moisture;
        
        return Math.Min(1.0f, tempFactor * moistureFactor * 1.5f);
    }
    
    /// <summary>
    /// Determines storm type from intensity
    /// </summary>
    private static StormType GetStormType(float intensity)
    {
        return intensity switch
        {
            > 0.8f => StormType.Tempest,
            > 0.6f => StormType.Thunderstorm,
            > 0.4f => StormType.Rainstorm,
            > 0.2f => StormType.Shower,
            _ => StormType.Drizzle
        };
    }
    
    /// <summary>
    /// Feeds the storm with new energy
    /// </summary>
    public void Feed(float energy)
    {
        Energy += energy;
        Intensity = Math.Min(1.0f, Energy / 100f);
        MaxLifetime += 30f; // Extend when fed
    }
}

/// <summary>
/// Types of storms
/// </summary>
public enum StormType
{
    Drizzle,      // Very light precipitation
    Shower,      // Light precipitation, brief
    Rainstorm,   // Moderate rain
    Thunderstorm,// Heavy with thunder
    Tempest,     // Severe storm
    Blizzard     // Snow storm
}
