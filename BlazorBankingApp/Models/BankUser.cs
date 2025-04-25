using System.ComponentModel.DataAnnotations.Schema;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[System.ComponentModel.DataAnnotations.Schema.Table("BankUser")]
public class BankUser : BaseModel
{
    [PrimaryKey("id")]
    [Supabase.Postgrest.Attributes.Column("id")]
    public string Id { get; set; }

<<<<<<< HEAD
    [Supabase.Postgrest.Attributes.Column("balance")]
    public decimal Balance { get; set; }
=======
    [System.ComponentModel.DataAnnotations.Schema.Column("balance")]
    public decimal balance { get; set; }
>>>>>>> eb70c34 (Logout page created, Loan page works)

    [Supabase.Postgrest.Attributes.Column("authoritylevel")]
    public string AuthorityLevel { get; set; }

<<<<<<< HEAD
=======
    [Supabase.Postgrest.Attributes.Column("name")]
    public string Name { get; set; }

    [Supabase.Postgrest.Attributes.Column("phone")]
    public string Phone { get; set; }
>>>>>>> 26fbc54 (Signup logic and recreating Bankusers mode)
}