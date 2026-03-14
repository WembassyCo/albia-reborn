using System;
using UnityEngine;

namespace Albia.Creatures.Neural
{
    /// <summary>
    /// Represents the current action state of a creature
    /// Mapped from neural network outputs
    /// </summary>
    [Serializable]
    public struct OrganismState
    {
        // Movement (mapped from neurons 0-2)
        public float MoveForward;   // Positive = forward, Negative = backward
        public float MoveLeft;      // Positive = left, Negative = right
        public float MoveRight;     // Positive = right, Negative = left
        
        // Rotation (mapped from neuron 3)
        public float TurnAmount;    // -1 = left 90°, 1 = right 90°
        
        // Actions (mapped from neurons 4-7)
        public bool IsEating;       // Neuron 4 > threshold AND food nearby
        public bool IsInteracting;  // Neuron 5 > threshold
        public bool IsResting;      // Neuron 6 > threshold
        public bool IsReproducing;  // Neuron 7 > threshold AND conditions met
        
        /// <summary>
        /// Threshold for binary action activation
        /// </summary>
        public const float ActionThreshold = 0.3f;
        
        /// <summary>
        /// Gets the dominant movement direction
        /// </summary>
        public Vector2 GetMovementVector()
        {
            // Combine left/right into single strafe value
            float strafe = MoveRight - MoveLeft;
            return new Vector2(strafe, MoveForward).normalized;
        }
        
        /// <summary>
        /// Gets the current primary action
        /// </summary>
        public CreatureAction GetPrimaryAction()
        {
            if (IsReproducing) return CreatureAction.Reproduce;
            if (IsEating) return CreatureAction.Eat;
            if (IsInteracting) return CreatureAction.Interact;
            if (IsResting) return CreatureAction.Rest;
            return CreatureAction.Move;
        }
    }

    /// <summary>
    /// Possible creature actions
    /// </summary>
    public enum CreatureAction
    {
        Move,
        Eat,
        Interact,
        Rest,
        Reproduce
    }

    /// <summary>
    /// Maps neural network outputs to creature actions
    /// Output neurons:
    /// - Neuron 0: Move forward/backward
    /// - Neuron 1: Move left
    /// - Neuron 2: Move right
    /// - Neuron 3: Turn
    /// - Neuron 4: Eat (if food nearby)
    /// - Neuron 5: Interact
    /// - Neuron 6: Rest
    /// - Neuron 7: Reproduce (if conditions met)
    /// </summary>
    [Serializable]
    public class ActionSystem
    {
        /// <summary>
        /// Number of output neurons
        /// </summary>
        public const int OutputCount = 8;
        
        /// <summary>
        /// Threshold for binary action activation
        /// </summary>
        public float ActionThreshold { get; set; } = 0.3f;
        
        /// <summary>
        /// Current action state
        /// </summary>
        public OrganismState CurrentState { get; private set; }
        
        /// <summary>
        /// Last raw neural outputs for debugging
        /// </summary>
        public float[] LastOutputs { get; private set; }
        
        /// <summary>
        /// Conditions checker for context-dependent actions
        /// </summary>
        public IActionConditions Conditions { get; set; }

        public ActionSystem()
        {
            LastOutputs = new float[OutputCount];
            Conditions = new DefaultActionConditions();
        }

        /// <summary>
        /// Maps neural network outputs to action state
        /// </summary>
        /// <param name="outputs">Neural network outputs (length must equal OutputCount)</param>
        /// <returns>Mapped organism state</returns>
        public OrganismState MapOutputs(float[] outputs)
        {
            if (outputs == null || outputs.Length != OutputCount)
            {
                throw new ArgumentException($"Outputs must have length {OutputCount}", nameof(outputs));
            }
            
            // Store raw outputs
            Array.Copy(outputs, LastOutputs, OutputCount);
            
            // Map movement outputs (neurons 0-2)
            float moveForward = outputs[0];           // Neuron 0
            float moveLeft = outputs[1];              // Neuron 1
            float moveRight = outputs[2];             // Neuron 2
            
            // Map turn output (neuron 3)
            float turnAmount = outputs[3];          // Neuron 3
            
            // Map action outputs with conditions (neurons 4-7)
            bool canEat = Conditions?.CanEat() ?? true;
            bool canReproduce = Conditions?.CanReproduce() ?? false;
            
            bool isEating = outputs[4] > ActionThreshold && canEat;       // Neuron 4
            bool isInteracting = outputs[5] > ActionThreshold;              // Neuron 5
            bool isResting = outputs[6] > ActionThreshold;                   // Neuron 6
            bool isReproducing = outputs[7] > ActionThreshold && canReproduce; // Neuron 7
            
            // Build state
            CurrentState = new OrganismState
            {
                MoveForward = moveForward,
                MoveLeft = moveLeft,
                MoveRight = moveRight,
                TurnAmount = turnAmount,
                IsEating = isEating,
                IsInteracting = isInteracting,
                IsResting = isResting,
                IsReproducing = isReproducing
            };
            
            return CurrentState;
        }

        /// <summary>
        /// Gets the index of the winning action neuron
        /// Returns -1 if no action neuron exceeds threshold
        /// </summary>
        public int GetWinningActionIndex()
        {
            float maxValue = ActionThreshold;
            int winningIndex = -1;
            
            // Check action neurons (4-7)
            for (int i = 4; i < OutputCount; i++)
            {
                if (LastOutputs[i] > maxValue)
                {
                    maxValue = LastOutputs[i];
                    winningIndex = i;
                }
            }
            
            return winningIndex;
        }

        /// <summary>
        /// Gets the action type from neuron index
        /// </summary>
        public static CreatureAction GetActionFromNeuron(int neuronIndex)
        {
            return neuronIndex switch
            {
                4 => CreatureAction.Eat,
                5 => CreatureAction.Interact,
                6 => CreatureAction.Rest,
                7 => CreatureAction.Reproduce,
                _ => CreatureAction.Move
            };
        }

        /// <summary>
        /// Gets the neuron index from action type
        /// </summary>
        public static int GetNeuronFromAction(CreatureAction action)
        {
            return action switch
            {
                CreatureAction.Eat => 4,
                CreatureAction.Interact => 5,
                CreatureAction.Rest => 6,
                CreatureAction.Reproduce => 7,
                _ => -1 // Movement uses neurons 0-3
            };
        }

        /// <summary>
        /// Gets a descriptive string of current actions
        /// </summary>
        public string GetActionDescription()
        {
            var actions = new System.Collections.Generic.List<string>();
            
            if (CurrentState.IsEating) actions.Add("Eating");
            if (CurrentState.IsInteracting) actions.Add("Interacting");
            if (CurrentState.IsResting) actions.Add("Resting");
            if (CurrentState.IsReproducing) actions.Add("Reproducing");
            
            if (actions.Count == 0)
            {
                var move = CurrentState.GetMovementVector();
                actions.Add($"Moving ({move.x:F2}, {move.y:F2})");
            }
            
            return string.Join(", ", actions);
        }

        /// <summary>
        /// Resets all action states
        /// </summary>
        public void Reset()
        {
            CurrentState = new OrganismState();
            Array.Clear(LastOutputs, 0, LastOutputs.Length);
        }
    }

    /// <summary>
    /// Interface for checking action conditions
    /// </summary>
    public interface IActionConditions
    {
        bool CanEat();
        bool CanReproduce();
        bool CanRest();
    }

    /// <summary>
    /// Default implementation of action conditions
    /// </summary>
    public class DefaultActionConditions : IActionConditions
    {
        public bool CanEat() => true;
        public bool CanReproduce() => false;
        public bool CanRest() => true;
    }
}
