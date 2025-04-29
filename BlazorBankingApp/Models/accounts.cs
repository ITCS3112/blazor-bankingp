using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("accounts")]
public class Account : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("balance")]
    public decimal Balance { get; set; }

    [Column("type")]
    public string Type { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}