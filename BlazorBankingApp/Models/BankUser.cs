using System.ComponentModel.DataAnnotations.Schema;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[System.ComponentModel.DataAnnotations.Schema.Table("BankUser")]
public class BankUser : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("balance")]
    public decimal balance { get; set; }

    [Supabase.Postgrest.Attributes.Column("authoritylevel")]
    public string AuthorityLevel { get; set; }
}