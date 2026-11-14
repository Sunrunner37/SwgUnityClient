using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
namespace Swg.Client.Core.World
{
    /// <summary>Server-provided crafting requirements, reusable by the datapad and future crafting UI.</summary>
    public sealed class DraftSchematicDetails
    {
        public sealed class ExperimentGroup
        {
            public string Name { get; }
            public IReadOnlyList<Experiment> Properties { get; }
            internal ExperimentGroup(string name, IReadOnlyList<Experiment> properties) { Name = name; Properties = properties; }
        }
        public readonly struct Experiment
        {
            public readonly string Name;
            public readonly (string Attribute, int Percentage)[] Weights;
            internal Experiment(string name, (string Attribute, int Percentage)[] weights) { Name = name; Weights = weights; }
        }
        // Native attribute positions correspond to server weight groups, including non-experimental placeholders.
        public IReadOnlyList<ExperimentGroup> GroupExperiments(SharedDraftSchematicTemplate.Attribute[] attributes)
        {
            var groups = new List<ExperimentGroup>();
            var shown = new HashSet<string>();
            List<Experiment> properties = null;
            string heading = null;
            for (int i = 0; i < Experiments.Length && i < attributes.Length; i++)
            {
                var weights = Experiments[i];
                var attribute = attributes[i];
                if (weights.Length == 0 || string.IsNullOrEmpty(attribute.Experiment)) continue;
                string key = attribute.Experiment + string.Join(";", weights.Select(weight => weight.Attribute + weight.Weight));
                if (!shown.Add(key)) continue;
                if (heading != attribute.Experiment)
                {
                    heading = attribute.Experiment;
                    properties = new List<Experiment>();
                    groups.Add(new ExperimentGroup(heading, properties));
                }
                int total = weights.Sum(weight => weight.Weight);
                properties.Add(new Experiment(attribute.Name, weights.Select(weight => (weight.Attribute, weight.Weight * 100 / total)).ToArray()));
            }
            return groups;
        }
        public int Complexity;
        public int Size;
        public ExperimentWeight[][] Experiments = Array.Empty<ExperimentWeight[]>();
        public readonly struct ExperimentWeight
        {
            public readonly string Attribute;
            public readonly int Weight;
            public ExperimentWeight(string attribute, int weight) { Attribute = attribute; Weight = weight; }
        }
        public Ingredient[] Ingredients = Array.Empty<Ingredient>();
        public readonly struct Ingredient
        {
            public readonly string Name;
            public readonly string Resource;
            public readonly uint Quantity;
            public readonly bool Optional;
            public Ingredient(string name, string resource, uint quantity, bool optional)
            {
                Name = name;
                Resource = resource;
                Quantity = quantity;
                Optional = optional;
            }
        }
    }
}
