using System.Collections;
using System.Linq; // Added for OfType<T>
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors; // Requires Microsoft.Xaml.Behaviors.Wpf NuGet package

namespace WPFGrowerApp.Infrastructure
{
    /// <summary>
    /// Attached behavior to enable two-way binding for ListBox.SelectedItems.
    /// Usage: <ListBox ... infrastructure:ListBoxSelectedItemsBehavior.SelectedItems="{Binding YourSelectedItemsCollection}">
    /// Requires Microsoft.Xaml.Behaviors.Wpf NuGet package.
    /// </summary>
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, OnSelectedItemsChanged)); // Changed BindsTwoWayByDefault to None

        public static IList GetSelectedItems(DependencyObject obj)
        {
            return (IList)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, IList value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                // Attach or detach the behavior
                var behavior = Interaction.GetBehaviors(listBox).OfType<ListBoxSelectionBehavior>().FirstOrDefault();

                if (e.NewValue != null && behavior == null)
                {
                    behavior = new ListBoxSelectionBehavior(listBox, (IList)e.NewValue);
                    Interaction.GetBehaviors(listBox).Add(behavior);
                }
                else if (e.NewValue == null && behavior != null)
                {
                    Interaction.GetBehaviors(listBox).Remove(behavior);
                }
            }
        }

        // Internal behavior class to handle synchronization
        private class ListBoxSelectionBehavior : Behavior<ListBox>
        {
            private readonly IList _targetList;
            private bool _isUpdating; // Prevents reentrancy

            public ListBoxSelectionBehavior(ListBox listBox, IList targetList)
            {
                _targetList = targetList;
            }

            protected override void OnAttached()
            {
                base.OnAttached();
                AssociatedObject.SelectionChanged += OnListBoxSelectionChanged;
                SyncListBoxFromSource(); // Initial sync
            }

            protected override void OnDetaching()
            {
                base.OnDetaching();
                if (AssociatedObject != null)
                {
                    AssociatedObject.SelectionChanged -= OnListBoxSelectionChanged;
                }
            }

            private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (_isUpdating) return;

                try
                {
                    _isUpdating = true;
                    _targetList.Clear();
                    if (AssociatedObject.SelectedItems != null)
                    {
                        foreach (var item in AssociatedObject.SelectedItems)
                        {
                            _targetList.Add(item);
                        }
                    }
                }
                finally
                {
                    _isUpdating = false;
                }
            }

            // Optional: Sync ListBox selection if the source collection changes externally
            // This requires the source collection to be ObservableCollection or similar.
            // If using ObservableCollection, hook into its CollectionChanged event here.
            // For simplicity, this example primarily syncs from ListBox to Source.

            private void SyncListBoxFromSource()
            {
                 if (_isUpdating) return;
                 // This part is tricky without knowing the exact source type.
                 // If _targetList changes, we need to update ListBox.SelectedItems.
                 // This often requires clearing and re-selecting.
                 // For now, we assume the primary flow is UI selection updating the ViewModel.
                 // A more robust solution might involve comparing collections.
                 try
                 {
                     _isUpdating = true;
                     AssociatedObject.SelectedItems.Clear();
                     foreach (var item in _targetList)
                     {
                         AssociatedObject.SelectedItems.Add(item);
                     }
                 }
                 finally
                 {
                     _isUpdating = false;
                 }
            }
        }
    }
}
