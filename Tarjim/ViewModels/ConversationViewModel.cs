using System;

namespace Tarjim.ViewModels
{
    public class ConversationViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}