using UnityEngine;
using System.Collections.Generic;

namespace Albia.Core.Interfaces
{
    /// <summary>
    /// Interface for UI inspection panels that display entity data.
    /// Defines the contract between Agents pod and UI pod.
    /// </summary>
    public interface IInspectionPanel
    {
        string PanelId { get; }
        string PanelTitle { get; }
        bool IsOpen { get; }
        IInspectable CurrentTarget { get; }
        
        void Open(IInspectable target);
        void Close();
        void Refresh();
        void SetTab(string tabName);
        
        void RegisterDataProvider(IInspectionDataProvider provider);
        void UnregisterDataProvider(IInspectionDataProvider provider);
    }

    /// <summary>
    /// Marks an entity as inspectable by the player.
    /// </summary>
    public interface IInspectable
    {
        string DisplayName { get; }
        string Description { get; }
        Sprite InspectionIcon { get; }
        IEnumerable<IInspectionSection> GetInspectionSections();
        void OnInspectionRequested();
    }

    /// <summary>
    /// Defines a section within an inspection panel.
    /// </summary>
    public interface IInspectionSection
    {
        string SectionName { get; }
        int Priority { get; }
        bool IsFoldable { get; }
        IInspectionField[] GetFields();
    }

    public interface IInspectionField
    {
        string FieldLabel { get; }
        string FieldValue { get; }
        FieldType ValueType { get; }
        bool IsEditable { get; }
    }

    public interface IInspectionDataProvider
    {
        string ProviderName { get; }
        bool CanProvideFor(IInspectable target);
        IEnumerable<IInspectionSection> GetSections(IInspectable target);
    }

    public enum FieldType
    {
        Text,
        Number,
        Percentage,
        Boolean,
        Gene,
        Chemical,
        Temperature,
        Vector,
        Time,
        HealthBar
    }
}