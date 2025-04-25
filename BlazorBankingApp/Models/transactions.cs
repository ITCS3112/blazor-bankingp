//using System.ComponentModel.DataAnnotations.Schema;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


[Table("transactions")]
public class Transaction : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }


    [Supabase.Postgrest.Attributes.Column("sender_id")]
    public Guid SenderId { get; set; }


    public string SenderName { get; set; }


    [Supabase.Postgrest.Attributes.Column("receiver_id")]
    public Guid ReceiverId { get; set; }


    public string ReceiverName { get; set; }
   


    [Supabase.Postgrest.Attributes.Column("amount")]
    public decimal Amount { get; set; }


    [Supabase.Postgrest.Attributes.Column("timestamp")]
    public DateTime Timestamp { get; set; }
}
