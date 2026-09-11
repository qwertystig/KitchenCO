using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace YourKitchenCo.Models;

public class NotificationLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = "All Users";
    public DateTime SentAt { get; set; } = DateTime.Now;
    public int RecipientCount { get; set; }
}