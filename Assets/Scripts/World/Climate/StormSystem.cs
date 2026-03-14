using UnityEngine;
using AlbiaReborn.Creatures;
using AlbiaReborn.Creatures.Biochemistry;

namespace AlbiaReborn.World.Climate
{
    /// <summary>
    /// Storm system: weather grid, precipitation, creature response.
    /// Week 12: Storm Season milestone.
    /// </summary>
    public class StormSystem : MonoBehaviour
    {
        [Header("Grid")]
        public int GridResolution = 64;
        public float UpdateInterval = 10f; // seconds
        
        [Header("Weather Cells")]
        private WeatherCell[,] _grid;
        
        [Header("Time")]
        public float TimeScale = 1f;

        void Start()
        {
            InitializeGrid();
            InvokeRepeating(nameof(UpdateGrid), 0f, UpdateInterval);
        }

        void InitializeGrid()
        {
            _grid = new WeatherCell[GridResolution, GridResolution];
            
            for (int x = 0; x < GridResolution; x++)
            {
                for (int z = 0; z < GridResolution; z++)
                {
                    _grid[x, z] = new WeatherCell
                    {
                        Temperature = 15f,
                        Moisture = 0.5f,
                        Wind = Vector2.zero,
                        Precipitation = 0f
                    };
                }
            }
        }

        void UpdateGrid()
        {
            // Run cellular automaton
            var newGrid = new WeatherCell[GridResolution, GridResolution];
            
            for (int x = 0; x < GridResolution; x++)
            {
                for (int z = 0; z < GridResolution; z++)
                {
                    newGrid[x, z] = SimulateCell(x, z);
                }
            }
            
            _grid = newGrid;
        }

        WeatherCell SimulateCell(int x, int z)
        {
            var cell = _grid[x, z];
            var newCell = new WeatherCell
            {
                Temperature = cell.Temperature,
                Moisture = cell.Moisture,
                Wind = cell.Wind,
                Precipitation = cell.Precipitation
            };

            // Temperature diffusion (biased by wind)
            float tempSum = cell.Temperature;
            int count = 1;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    
                    if (nx >= 0 && nx < GridResolution && nz >= 0 && nz < GridResolution)
                    {
                        // Bias by wind direction
                        float bias = 1f;
                        if (dx != 0) bias *= 1f + Mathf.Sign(dx) * cell.Wind.x * 0.5f;
                        if (dz != 0) bias *= 1f + Mathf.Sign(dz) * cell.Wind.y * 0.5f;
                        
                        tempSum += _grid[nx, nz].Temperature * bias;
                        count++;
                    }
                }
            }
            
            newCell.Temperature = tempSum / count;

            // Moisture evaporation and precipitation
            if (cell.Moisture > 0.8f && cell.Temperature > 0)
            {
                // Precipitation
                newCell.Precipitation = cell.Moisture * 0.5f;
                newCell.Moisture *= 0.7f;
            }

            // Storm formation
            if (cell.Temperature > 25f && cell.Moisture > 0.6f && Random.value < 0.01f)
            {
                newCell.IsStorm = true;
                newCell.Wind += new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
            }

            return newCell;
        }

        /// <summary>
        /// Apply weather to creature at position.
        /// </summary>
        public void ApplyWeatherToCreature(Organism creature, Vector3 position)
        {
            if (_grid == null) return;
            
            int gx = Mathf.FloorToInt(position.x / GridResolution);
            int gz = Mathf.FloorToInt(position.z / GridResolution);
            
            if (gx < 0 || gx >= GridResolution || gz < 0 || gz >= GridResolution)
                return;
            
            var cell = _grid[gx, gz];
            var chems = creature.Chemicals;

            // Temperature discomfort
            float temp = cell.Temperature;
            float tempComfort = GetTemperatureComfort(creature);
            float tempDiff = Mathf.Abs(temp - tempComfort);
            
            if (tempDiff > 10f)
            {
                chems.Apply(ChemicalType.Discomfort, tempDiff * 0.001f);
            }

            // Storm fear
            if (cell.IsStorm)
            {
                chems.Apply(ChemicalType.Fear, 0.01f);
                chems.Apply(ChemicalType.Discomfort, 0.005f);
            }

            // Precipitation discomfort
            if (cell.Precipitation > 0.5f)
            {
                // Check if sheltered (simplified - just check if in enclosed)
                bool isSheltered = CheckIsSheltered(creature);
                
                if (!isSheltered)
                {
                    chems.Apply(ChemicalType.Discomfort, cell.Precipitation * 0.01f);
                }
            }

            // Cold (blizzard)
            if (cell.Temperature < 0 && cell.Precipitation > 0.3f)
            {
                chems.Apply(ChemicalType.Pain, 0.02f);
                creature.Energy -= 0.05f; // Cold damage
            }
        }

        bool CheckIsSheltered(Organism creature)
        {
            // Raycast up to check for roof
            // Simplified: return false for now
            return false;
        }

        float GetTemperatureComfort(Organism creature)
        {
            // Based on genome
            return 20f; // Default
        }

        public WeatherCell GetCellAt(Vector3 position)
        {
            int gx = Mathf.FloorToInt(position.x / GridResolution);
            int gz = Mathf.FloorToInt(position.z / GridResolution);
            
            if (gx >= 0 && gx < GridResolution && gz >= 0 && gz < GridResolution)
                return _grid[gx, gz];
            
            return null;
        }
    }

    public class WeatherCell
    {
        public float Temperature; // Celsius
        public float Moisture; // 0-1
        public Vector2 Wind; // Direction + magnitude
        public float Precipitation; // 0-1
        public bool IsStorm;
    }
}
