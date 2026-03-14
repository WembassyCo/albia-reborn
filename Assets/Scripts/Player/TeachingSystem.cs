using UnityEngine;
using AlbiaReborn.Creatures;
using AlbiaReborn.Creatures.Biochemistry;

namespace AlbiaReborn.Player
{
    /// <summary>
    /// Player teaches Norns words by pointing and vocalizing.
    /// Week 18: Steward Tools.
    /// </summary>
    public class TeachingSystem : MonoBehaviour
    {
        [Header("Settings")]
        public float TeachingRange = 10f;
        public float AttentionThreshold = 0.3f;
        public float LearningWindow = 3f; // seconds to associate
        
        [Header("State")]
        public GameObject CurrentTarget;
        public string CurrentWord;
        public bool IsTeaching = false;

        // Vocab tracking per creature
        private System.Collections.Generic.Dictionary<Norn, WordAssociation> _wordMemory 
            = new System.Collections.Generic.Dictionary<Norn, WordAssociation>();

        void Update()
        {
            // Point at target
            if (Input.GetMouseButtonDown(1)) // Right click
            {
                TrySelectTarget();
            }

            // Type word to teach
            if (IsTeaching && Input.anyKeyDown)
            {
                // Simplified: store typed char
                // Full implementation: capture string input
            }

            if (IsTeaching && CurrentTarget != null && !string.IsNullOrEmpty(CurrentWord))
            {
                BroadcastTeaching();
            }
        }

        void TrySelectTarget()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                CurrentTarget = hit.collider.gameObject;
                IsTeaching = true;
                Debug.Log($"Teaching target: {CurrentTarget.name}");
            }
        }

        /// <summary>
        /// Teach word to nearby Norns.
        /// </summary>
        public void TeachWord(string word, GameObject context)
        {
            CurrentWord = word;
            
            // Find attentive Norns
            var nearby = PopulationRegistry.Instance?.GetOrganismsInRange(transform.position, TeachingRange);
            if (nearby == null) return;
            
            foreach (var organism in nearby)
            {
                if (organism is Norn norn)
                {
                    if (IsNornAttentive(norn))
                    {
                        LearnWord(norn, word, context);
                    }
                }
            }
        }

        void BroadcastTeaching()
        {
            // Raycast to find what player is pointing at
            // Teach word to attentive Norns
        }

        bool IsNornAttentive(Norn norn)
        {
            // Norn is attentive if:
            // - Curiosity is high, or
            // - Fear is low, and
            // - Looking at player
            
            float curiosity = norn.Chemicals.GetChemical(ChemicalType.Curiousity);
            float fear = norn.Chemicals.GetChemical(ChemicalType.Fear);
            
            return curiosity > AttentionThreshold && fear < 0.5f;
        }

        void LearnWord(Norn norn, string word, GameObject context)
        {
            // Create association with current chemical state
            ChemicalType contextChemical = GetContextChemical(context);
            
            if (!_wordMemory.ContainsKey(norn))
            {
                _wordMemory[norn] = new WordAssociation();
            }
            
            _wordMemory[norn].Learn(word, contextChemical, 0.1f); // learning delta
            
            Debug.Log($"{norn.OrganismName} learned: {word} = {contextChemical}");
            
            // Trigger reward
            norn.Chemicals.Apply(ChemicalType.Reward, 0.3f);
        }

        ChemicalType GetContextChemical(GameObject context)
        {
            // Infer meaning from object type
            if (context.GetComponent<Ecology.PlantOrganism>() != null)
                return ChemicalType.Hunger;
            
            if (context.GetComponent<Norn>() != null)
                return ChemicalType.Affection;
            
            // etc.
            return ChemicalType.Curiousity;
        }
    }

    public class WordAssociation
    {
        public System.Collections.Generic.Dictionary<string, ChemicalType> WordToChemical;
        public System.Collections.Generic.Dictionary<string, float> WordStrength;
        public int MaxWords = 50;

        public WordAssociation()
        {
            WordToChemical = new();
            WordStrength = new();
        }

        public void Learn(string word, ChemicalType chemical, float delta)
        {
            if (!WordStrength.ContainsKey(word))
            {
                // Max words check
                if (WordStrength.Count >= MaxWords)
                {
                    // Drop weakest
                    string weakest = null;
                    float minStrength = float.MaxValue;
                    foreach (var kvp in WordStrength)
                    {
                        if (kvp.Value < minStrength)
                        {
                            minStrength = kvp.Value;
                            weakest = kvp.Key;
                        }
                    }
                    if (weakest != null)
                    {
                        WordStrength.Remove(weakest);
                        WordToChemical.Remove(weakest);
                    }
                }
                
                WordToChemical[word] = chemical;
                WordStrength[word] = 0f;
            }

            WordStrength[word] = Mathf.Clamp01(WordStrength[word] + delta);
            WordToChemical[word] = chemical;
        }
    }
}
