namespace Albia.Creatures
{
    /// <summary>
    /// All possible creature actions.
    /// Map to neural net output indices.
    /// </summary>
    public enum ActionType
    {
        MoveToward,     // Approach nearest detected target
        MoveAway,       // Flee from nearest threat
        Eat,            // Consume food in range
        Rest,           // Reduce activity
        Sleep,          // Full rest mode
        Vocalize,       // Produce sound
        Interact,       // Use object in range
        PickUp,         // Add object to inventory
        Drop,           // Release held object
        Dig,            // Alter terrain
        Build,          // Place held material
        Craft,          // Combine held items
        Plant,          // Plant seed
        Groom,          // Social bonding
        Attack          // Damage nearby creature
    }
}
