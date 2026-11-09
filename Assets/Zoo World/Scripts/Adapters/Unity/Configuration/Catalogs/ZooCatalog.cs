using System;
using System.Collections.Generic;
using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Animal catalog")]
    public sealed class ZooCatalog : ScriptableObject
    {
        public AnimalDefinitionSO[] Animals;

        public AnimalDefinition[] CreateDefinitions()
        {
            if (Animals == null || Animals.Length == 0)
            {
                throw new InvalidOperationException("Animal catalog is empty.");
            }

            var ids = new HashSet<string>();
            var definitions = new AnimalDefinition[Animals.Length];
            for (var i = 0; i < Animals.Length; i++)
            {
                definitions[i] = Animals[i].Create();
                if (!ids.Add(definitions[i].TypeId))
                {
                    throw new InvalidOperationException("Duplicate TypeId: " + definitions[i].TypeId);
                }
            }

            return definitions;
        }
    }
}
