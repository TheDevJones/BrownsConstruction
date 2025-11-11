using BCSApp.Models;

namespace BCSApp.Models
{
    public class ConversationViewModel
    {
        public ApplicationUser Partner { get; set; } = null!;
        public Message? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}