namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Interface for providing contextual help content for different views
    /// </summary>
    public interface IHelpContentProvider
    {
        /// <summary>
        /// Gets help content for a specific view
        /// </summary>
        /// <param name="viewName">Name of the view (e.g., "UserManagement", "Dashboard")</param>
        /// <returns>HelpContent object containing title, content, tips, and shortcuts</returns>
        HelpContent GetHelpContent(string viewName);
    }

    /// <summary>
    /// Represents help content for a view
    /// </summary>
    public class HelpContent
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string QuickTips { get; set; }
        public string KeyboardShortcuts { get; set; }

        public HelpContent(string title, string content, string quickTips = null, string keyboardShortcuts = null)
        {
            Title = title;
            Content = content;
            QuickTips = quickTips;
            KeyboardShortcuts = keyboardShortcuts;
        }
    }
}

