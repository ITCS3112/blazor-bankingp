using Supabase.Postgrest.Models;

public class BankUser : BaseModel
{
    public string Id { get; set; }
    public decimal Balance { get; set; }
    public string AuthorityLevel { get; set; }
}