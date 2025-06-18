using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     Provides a standardized set of interactions for a "tabbed" UI layout, 
    ///     using the ViewDataKey fields of the VisualElements to map tabs to containers.
    /// </summary>
    public class TabbedMenuController : MenuController
    {

        /// <summary> Enumerates all tab buttons in the Menu </summary>
        protected readonly List<VisualElement> TabButtons = new();

        /// <summary> Enumerates all tab container elements in the Menu </summary>
        protected readonly List<VisualElement> TabContainers = new();


        /// <summary>
        ///     Invoked in the Start() callback, driven by the parent class
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Links button interactions for tabs, containers, and individual item options
            ConfigureTabViewClasses();
        }


        // UI Navigation

        protected virtual void ConfigureTabViewClasses()
        {
            // Configure tab buttons
            Root.Query<Button>(className: UIHelper.ClassTabButton).ForEach((button) =>
            {
                TabButtons.Add(button);

                button.clicked += () =>
                {
                    SetActiveTab(button);
                };
            });

            // Configure tab containers
            // Previously TemplateContainer, instead the tab containers will be VisualElements and
            // their contents will be TemplateContainers. This is more predictable
            Root.Query<VisualElement>(className: UIHelper.ClassTabContainer).ForEach(container =>
            {
                TabContainers.Add(container);
            });
        }


        // Tab Controls

        /// <summary>
        ///     Set the active tab based on the provided <paramref name="tab"/> VisualElement, matching 
        ///     the <see cref="VisualElement.viewDataKey"/> metadata fields between tab and container
        /// </summary>
        /// <param name="tab">The button for the selected tab</param>
        protected virtual void SetActiveTab(VisualElement tab)
        {
            // Ensures the selected tab is mapped to a Task
            if (tab.viewDataKey.Equals(string.Empty))
            {
                Debug.LogWarning($"{nameof(SetActiveTab)} invoked with no mapping data ({tab.name})");
                return;
            }

            // Ensure all tab buttons are enabled and free of styling
            TabButtons.ForEach(element =>
            {
                element.SetEnabled(true);
                element.RemoveFromClassList(UIHelper.ClassTabButtonSelected);
            });

            // Apply selected style to only the current tab
            tab.AddToClassList(UIHelper.ClassTabButtonSelected);

            // Hide all tab containers
            TabContainers.ForEach(element => element.style.display = DisplayStyle.None);

            // All containers set hidden on start
            var container = GetContainer(tab);
            if (container == null)
            {
                Debug.LogWarning($"No tab container matches the tab button '{tab.name}'");
                return;
            }

            container.style.display = DisplayStyle.Flex;
        }


        // Container Mapping

        public VisualElement GetContainer(VisualElement tab)
        {
            return TabContainers.FindElementByViewData(tab.viewDataKey);
        }

        // Tab Mapping

        public VisualElement GetTab(VisualElement container)
        {
            return TabButtons.FindElementByViewData(container.viewDataKey);
        }
    }
}
